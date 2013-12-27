#include "pch.h"
#include "EmulatorRenderer.h"
#include "EmulatorFileHandler.h"
#include "DDSTextureLoader.h"
#include "WP8Snes9xComponent.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;
using namespace Windows::Graphics::Display;

#define CROSS_TEXTURE_FILE_NAME						L"Assets/pad_cross.dds"
#define BUTTONS_TEXTURE_FILE_NAME					L"Assets/pad_buttons.dds"
#define BUTTONS_GRAY_TEXTURE_FILE_NAME				L"Assets/pad_buttons_gray.dds"
#define SS_TEXTURE_FILE_NAME						L"Assets/pad_start_select.dds"
#define L_TEXTURE_FILE_NAME							L"Assets/pad_l_button.dds"
#define R_TEXTURE_FILE_NAME							L"Assets/pad_r_button.dds"
#define STICK_TEXTURE_FILE_NAME						L"Assets/ThumbStick.dds"
#define STICK_CENTER_TEXTURE_FILE_NAME				L"Assets/ThumbStickCenter.dds"

#define AUTOSAVE_INTERVAL			60.0f

float lastElapsed = 0.0f;
int framesNotRendered = 0;
extern bool turbo;
extern bool enableTurboMode;

EmulatorRenderer::EmulatorRenderer()
	: emulator(EmulatorGame::GetInstance()),
	frontbuffer(0), controller(nullptr), autosaving(false),
	elapsedTime(0.0f), settings(EmulatorSettings::Current), frames(0)
{ 
	this->waitEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
}

EmulatorRenderer::~EmulatorRenderer(void)
{
	if(this->m_d3dContext)
	{
		this->m_d3dContext->Unmap(this->buffers[(this->frontbuffer + 1) % 2].Get(), 0);
	}

	CloseHandle(this->waitEvent);

	delete this->spriteBatch;
	this->spriteBatch = nullptr;

	delete this->commonStates;
	this->commonStates = nullptr;
}

void EmulatorRenderer::CreateDeviceResources()
{
	Direct3DBase::CreateDeviceResources();

	if(this->spriteBatch)
	{
		delete this->spriteBatch;
		this->spriteBatch = nullptr;
	}
	this->m_d3dDevice->GetImmediateContext1(this->m_d3dContext.GetAddressOf());
	this->spriteBatch = new SpriteBatch(this->m_d3dContext.Get());

	if(this->commonStates)
	{
		delete this->commonStates;
		this->commonStates = nullptr;
	}
	this->commonStates = new CommonStates(this->m_d3dDevice.Get());
	
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), STICK_TEXTURE_FILE_NAME,
		this->stickResource.GetAddressOf(),
		this->stickSRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), STICK_CENTER_TEXTURE_FILE_NAME,
		this->stickCenterResource.GetAddressOf(),
		this->stickCenterSRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), CROSS_TEXTURE_FILE_NAME,
		this->crossResource.GetAddressOf(),
		this->crossSRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), BUTTONS_TEXTURE_FILE_NAME,
		this->buttonsResource.GetAddressOf(),
		this->buttonsSRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), BUTTONS_GRAY_TEXTURE_FILE_NAME,
		this->buttonsGrayResource.GetAddressOf(),
		this->buttonsGraySRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), SS_TEXTURE_FILE_NAME,
		this->startSelectResource.GetAddressOf(),
		this->startSelectSRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), L_TEXTURE_FILE_NAME,
		this->lButtonResource.GetAddressOf(),
		this->lButtonSRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), R_TEXTURE_FILE_NAME,
		this->rButtonResource.GetAddressOf(),
		this->rButtonSRV.GetAddressOf())
		);

	// Create Textures and SRVs for front and backbuffer
	D3D11_TEXTURE2D_DESC desc;
	ZeroMemory(&desc, sizeof(D3D11_TEXTURE2D_DESC));

	desc.ArraySize = 1;
	desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
	desc.Format = DXGI_FORMAT_B5G6R5_UNORM;
	desc.Width = EXT_WIDTH;
	desc.Height = EXT_HEIGHT;
	desc.MipLevels = 1;
	desc.SampleDesc.Count = 1;
	desc.SampleDesc.Quality = 0;
	desc.Usage = D3D11_USAGE_DYNAMIC;

	DX::ThrowIfFailed(
		this->m_d3dDevice->CreateTexture2D(&desc, nullptr, this->buffers[0].GetAddressOf())
		);
	DX::ThrowIfFailed(
		this->m_d3dDevice->CreateTexture2D(&desc, nullptr, this->buffers[1].GetAddressOf())
		);

	D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc;
	ZeroMemory(&srvDesc, sizeof(D3D11_SHADER_RESOURCE_VIEW_DESC));
	srvDesc.Format = DXGI_FORMAT_UNKNOWN;
	srvDesc.Texture2D.MipLevels = 1;
	srvDesc.Texture2D.MostDetailedMip = 0;
	srvDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;

	DX::ThrowIfFailed(
		this->m_d3dDevice->CreateShaderResourceView(this->buffers[0].Get(), &srvDesc, this->bufferSRVs[0].GetAddressOf())
		);
	DX::ThrowIfFailed(
		this->m_d3dDevice->CreateShaderResourceView(this->buffers[1].Get(), &srvDesc, this->bufferSRVs[1].GetAddressOf())
		);

	// Map backbuffer so it can be unmapped on first update
	int backbuffer = (this->frontbuffer + 1) % 2;
	this->backbufferPtr = (uint16 *) this->MapBuffer(backbuffer, &this->pitch);

	D3D11_BLEND_DESC blendDesc;
	ZeroMemory(&blendDesc, sizeof(D3D11_BLEND_DESC));

	blendDesc.RenderTarget[0].BlendEnable = true;
	blendDesc.RenderTarget[0].SrcBlend = blendDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_SRC_ALPHA;
	blendDesc.RenderTarget[0].DestBlend = blendDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_INV_SRC_ALPHA;
	blendDesc.RenderTarget[0].BlendOp = blendDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;

	blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;

	DX::ThrowIfFailed(
		this->m_d3dDevice->CreateBlendState(&blendDesc, this->alphablend.GetAddressOf())
		);
}

void EmulatorRenderer::CreateWindowSizeDependentResources()
{
	Direct3DBase::CreateWindowSizeDependentResources();
}

void EmulatorRenderer::UpdateForWindowSizeChange(float width, float height)
{
	Direct3DBase::UpdateForWindowSizeChange(width, height);

	float scale = ((int)DisplayProperties::ResolutionScale) / 100.0f;
	this->height = width * scale;
	this->width = height * scale;

	if(this->height > 700.0f && this->height < 740.0f)
	{
		this->format = HD720P;
	}else if(this->height > 760.0f)
	{
		this->format = WXGA;
	}else 
	{
		this->format = WVGA;
	}

	if(this->controller)
	{
		this->controller->UpdateFormat(this->format);
	}

	this->CreateTransformMatrix();
}

void EmulatorRenderer::SetVirtualController(VirtualController *controller)
{
	this->controller = controller;
	this->controller->UpdateFormat(this->format);
	this->controller->SetOrientation(this->orientation);
	this->controller->pressCount = 0;
	this->controller->errCount = 0;
}

void EmulatorRenderer::GetBackbufferData(uint16 **backbufferPtr, size_t *pitch, int *imageWidth, int *imageHeight)
{
	*backbufferPtr = this->backbufferPtr;
	*pitch = this->pitch;
	*imageWidth = EmulatorGame::SnesImageWidth;
	*imageHeight = EmulatorGame::SnesImageHeight;
}

void EmulatorRenderer::ChangeOrientation(int orientation)
{
	this->orientation = orientation;
	if(this->controller)
	{
		this->controller->SetOrientation(this->orientation);
	}
	this->CreateTransformMatrix();
}

void EmulatorRenderer::CreateTransformMatrix(void)
{
	this->outputTransform = XMMatrixIdentity();

	if(this->orientation != ORIENTATION_PORTRAIT)
	{
		if(this->orientation == ORIENTATION_LANDSCAPE_RIGHT)
		{
			this->outputTransform = XMMatrixMultiply(XMMatrixTranslation(-this->width/2, -this->height/2, 0.0f), XMMatrixRotationZ(XM_PI));
			this->outputTransform = XMMatrixMultiply(this->outputTransform, XMMatrixTranslation(this->width/2, this->height/2, 0.0f));
		}

		this->outputTransform = XMMatrixMultiply(this->outputTransform, XMMatrixRotationZ(XM_PIDIV2));
		this->outputTransform = XMMatrixMultiply(this->outputTransform, XMMatrixTranslation(this->height, 0.0f, 0.0f));
	}
}

void EmulatorRenderer::Update(float timeTotal, float timeDelta)
{
	lastElapsed = timeDelta;

	if(!emulator->IsPaused())
	{
		this->elapsedTime += timeDelta;
	}
	/*if(!settings->LowFrequencyModeMeasured)
	{
		if(this->elapsedTime >= 3.0f)
		{
			if(this->frames < 100)
			{
				settings->LowFrequencyMode = true;
			}else
			{
				settings->LowFrequencyMode = false;
			}
			settings->LowFrequencyModeMeasured = true;
		}
	}*/
	if(this->elapsedTime >= AUTOSAVE_INTERVAL)
	{
		if(!emulator->IsPaused())
		{
			this->elapsedTime -= AUTOSAVE_INTERVAL;
			autosaving = true;
			SaveSRAMAsync().then([this]()
			{
				int oldSlot = SavestateSlot;
				SavestateSlot = AUTOSAVE_SLOT;
				SaveStateAsync().wait();
				SavestateSlot = oldSlot;
				//Settings.Mute = !EmulatorSettings::Current->SoundEnabled;
				emulator->Unpause();
				SetEvent(this->waitEvent);
			});
		}else
		{
			this->elapsedTime = AUTOSAVE_INTERVAL;
		}
	}

	bool left = false;
	bool right = false;
	bool up = false;
	bool down = false;
	bool start = false;
	bool select = false;
	bool a = false;
	bool b = false;
	bool x = false;
	bool y = false;
	bool l = false;
	bool r = false;
	turbo = false;
		
	//Moga
	using namespace Moga::Windows::Phone;
	Moga::Windows::Phone::ControllerManager^ ctrl = Direct3DBackground::getController();
	if(EmulatorSettings::Current->UseMogaController && ctrl != nullptr && ctrl->GetState(Moga::Windows::Phone::ControllerState::Connection) == ControllerResult::Connected)
	{
		//pro only, the pocket version has the left joy stick output to both Axis and directional Keycode.
		if (ctrl->GetState(Moga::Windows::Phone::ControllerState::SelectedVersion) != ControllerResult::VersionMoga ) 
		{
			float axis_x = ctrl->GetAxisValue(Axis::X);
			float axis_y = ctrl->GetAxisValue(Axis::Y);

			float angle = 0;
			if (axis_x == 0)
			{
				if (axis_y > 0)
					angle = 1.571f;
				else if (axis_y < 0)
					angle = 4.712f;
				else
					angle = 1000.0f; //non existent value
			}
			else if (axis_x > 0)
			{
				angle = atan(axis_y / axis_x);

				if (angle < 0)
					angle += 6.283f;
			}
			else if (axis_x <0)
			{
				angle = atan(axis_y / axis_x) +  3.142f;
			}

			//convert to degree
			angle = angle / 3.142f * 180.0f;
		
			if ( angle <= 22.5f)
			{
				right = true;
			}
			else if ( angle <= 67.5f)
			{
				right = true;
				up = true;
			}
			else if (angle <= 112.5f)
			{
				up = true;
			}
			else if (  angle <= 157.5f)
			{
				up = true;
				left = true;
			}
			else if (angle <= 202.5f)
			{
				left = true;
			}
			else if (angle <= 247.5f)
			{
				left = true;
				down = true;
			}
			else if (angle <= 292.5f)
			{
				down = true;
			}
			else if (angle <= 337.5f)
			{
				down = true;
				right = true;
			}
			else if (angle <= 360.0f)
			{
				right = true;
			}
		}

		
		if(ctrl->GetKeyCode(KeyCode::A) == ControllerAction::Pressed )
		{
			b = true;
		}

		if(ctrl->GetKeyCode(KeyCode::B) == ControllerAction::Pressed)
		{
			a = true;
		}

		if(ctrl->GetKeyCode(KeyCode::X) == ControllerAction::Pressed)
		{
			y = true;
		}

		if( ctrl->GetKeyCode(KeyCode::Y) == ControllerAction::Pressed)
		{
			x = true;
		}

		if(ctrl->GetKeyCode(KeyCode::Start) == ControllerAction::Pressed)
		{
			start = true;
		}

		if(ctrl->GetKeyCode(KeyCode::Select) == ControllerAction::Pressed)
		{
			select = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionLeft) == ControllerAction::Pressed)
		{
			left = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionRight) == ControllerAction::Pressed)
		{
			right = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionUp) == ControllerAction::Pressed)
		{
			up = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionDown) == ControllerAction::Pressed)
		{
			down = true;
		}

		if(ctrl->GetKeyCode(KeyCode::L1) == ControllerAction::Pressed || ctrl->GetKeyCode(KeyCode::L2) == ControllerAction::Pressed)
		{
			l = true;
		}

		if(ctrl->GetKeyCode(KeyCode::R1) == ControllerAction::Pressed || ctrl->GetKeyCode(KeyCode::R2) == ControllerAction::Pressed)
		{
			r = true;
		}

	}


	if(this->controller)
	{
		const Emulator::ControllerState *state = this->controller->GetControllerState();

		left |= state->LeftPressed;
		right |= state->RightPressed;
		up |= state->UpPressed;
		down |= state->DownPressed;
		start |= state->StartPressed;
		select |= state->SelectPressed;
		a |= state->APressed;
		b |= state->BPressed;
		x |= state->XPressed;
		y |= state->YPressed;
/*
		this->emulator->SetButtonState(JOYPAD_LEFT, state->LeftPressed);
		this->emulator->SetButtonState(JOYPAD_UP, state->UpPressed);
		this->emulator->SetButtonState(JOYPAD_RIGHT, state->RightPressed);
		this->emulator->SetButtonState(JOYPAD_DOWN, state->DownPressed);
		this->emulator->SetButtonState(JOYPAD_START, state->StartPressed);
		this->emulator->SetButtonState(JOYPAD_SELECT, state->SelectPressed);
		this->emulator->SetButtonState(JOYPAD_A, state->APressed);
		this->emulator->SetButtonState(JOYPAD_B, state->BPressed);
		this->emulator->SetButtonState(JOYPAD_X, state->XPressed);
		this->emulator->SetButtonState(JOYPAD_Y, state->YPressed);*/
		if(settings->CameraButtonAssignment == 0)
		{
			/*this->emulator->SetButtonState(JOYPAD_L, state->LPressed);
			this->emulator->SetButtonState(JOYPAD_R, state->RPressed);*/

			l |= state->LPressed;
			r |= state->RPressed;
		}else if(settings->CameraButtonAssignment == 1)
		{
			// R Button
			/*this->emulator->SetButtonState(JOYPAD_L, state->LPressed);
			this->emulator->SetButtonState(JOYPAD_R, enableTurboMode);
			turbo = state->RPressed;*/

			l |= state->LPressed;
			r |= enableTurboMode;
			turbo |= state->RPressed;
		}else if(settings->CameraButtonAssignment == 2)
		{
			// L Button
			/*this->emulator->SetButtonState(JOYPAD_R, state->RPressed);
			this->emulator->SetButtonState(JOYPAD_L, enableTurboMode);
			turbo = state->LPressed;*/

			r |= state->RPressed;
			l |= enableTurboMode;
			turbo |= state->LPressed;
		}else if(settings->CameraButtonAssignment == 3)
		{
			/*this->emulator->SetButtonState(JOYPAD_L, enableTurboMode);
			this->emulator->SetButtonState(JOYPAD_R, enableTurboMode);
			turbo = state->RPressed || state->LPressed;*/

			l |= enableTurboMode;
			r |= enableTurboMode;
			turbo |= state->RPressed || state->LPressed;
		}
	}

	this->emulator->SetButtonState(JOYPAD_LEFT, left);
	this->emulator->SetButtonState(JOYPAD_UP, up);
	this->emulator->SetButtonState(JOYPAD_RIGHT, right);
	this->emulator->SetButtonState(JOYPAD_DOWN, down);
	this->emulator->SetButtonState(JOYPAD_START, start);
	this->emulator->SetButtonState(JOYPAD_SELECT, select);
	this->emulator->SetButtonState(JOYPAD_A, a);
	this->emulator->SetButtonState(JOYPAD_B, b);
	this->emulator->SetButtonState(JOYPAD_X, x);
	this->emulator->SetButtonState(JOYPAD_Y, y);
	this->emulator->SetButtonState(JOYPAD_L, l);
	this->emulator->SetButtonState(JOYPAD_R, r);
}

void EmulatorRenderer::Render()
{
	if(autosaving)
	{
		WaitForSingleObjectEx(this->waitEvent, INFINITE, false);
		autosaving = false;
	}

	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);

	const float black[] = { 0.0f, 0.0f, 0.0f, 1.000f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		black
		);

	m_d3dContext->ClearDepthStencilView(
		m_depthStencilView.Get(),
		D3D11_CLEAR_DEPTH,
		1.0f,
		0
		);

	if(!this->emulator->IsPaused())
	{
		if(framesNotRendered >= settings->PowerFrameSkip)
		{
			framesNotRendered = 0;

			if(!this->emulator->LastFrameSkipped())
			{
				// Swap buffers for background emulation
				int backbuffer = this->frontbuffer;
				this->frontbuffer = (this->frontbuffer + 1) % 2;
				uint16 *buffer = (uint16 *) this->MapBuffer(backbuffer, &this->pitch);
				this->backbufferPtr = buffer;
				//memset (buffer, 0, EXT_PITCH * EXT_HEIGHT);

				this->emulator->Update((void *) buffer, this->pitch, lastElapsed);
				this->m_d3dContext->Unmap(this->buffers[this->frontbuffer].Get(), 0);
			}else
			{
				this->emulator->Update((void *) this->backbufferPtr, this->pitch, lastElapsed);
			}
		}else
		{
			framesNotRendered++;
		}
	}

	int height, width;
	RECT rect;

	if(this->orientation != ORIENTATION_PORTRAIT)
	{
		height = this->height;
		if(settings->FullscreenStretch)
		{
			width = this->width;
			rect.left = 0;
			rect.right = width;
			rect.top = 0;
			rect.bottom = height;
		}else
		{
			height *= (this->settings->ImageScaling / 100.0f);
			width = (int)(height * ((float) SNES_WIDTH) / (float) SNES_HEIGHT);
			int leftOffset = (this->width - width) / 2;
			rect.left = leftOffset;
			rect.right = width + leftOffset;
			rect.top = 0;
			rect.bottom = height;
		}
	}else
	{
		width = this->height;
		height = (int)(width * ((float) SNES_HEIGHT / (float) SNES_WIDTH));
		rect.left = 0;
		rect.right = width;
		rect.top = 0;
		rect.bottom = height;
	}

	RECT source;
	source.left = 0;
	source.right = EmulatorGame::SnesImageWidth;
	source.top = 0;
	source.bottom = EmulatorGame::SnesImageHeight;
			
	this->controller->GetButtonsRectangle(&buttonsRectangle);
	this->controller->GetCrossRectangle(&crossRectangle);
	this->controller->GetStartSelectRectangle(&startSelectRectangle);
	this->controller->GetLRectangle(&lRectangle);
	this->controller->GetRRectangle(&rRectangle);

	float opacity = this->settings->ControllerOpacity / 100.0f;
	XMFLOAT4A colorf = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity);
	XMFLOAT4A colorf2 = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity + 0.2f);
	if(this->orientation == ORIENTATION_PORTRAIT)
	{
		colorf.w = 0.3f + 0.7f * opacity;
	}
	XMVECTOR colorv = XMLoadFloat4A(&colorf);
	XMVECTOR colorv2 = XMLoadFloat4A(&colorf2);
	
	// Render last frame to screen
	this->spriteBatch->Begin(
		DirectX::SpriteSortMode::SpriteSortMode_Deferred, 
		this->alphablend.Get(), nullptr, nullptr, nullptr, nullptr, 
		this->outputTransform);
	this->spriteBatch->Draw(this->bufferSRVs[this->frontbuffer].Get(), rect, &source);


	//===draw virtual controller if moga controller is not loaded
	using namespace Moga::Windows::Phone;
	Moga::Windows::Phone::ControllerManager^ ctrl = Direct3DBackground::getController();
	if (!(EmulatorSettings::Current->UseMogaController && ctrl != nullptr && ctrl->GetState(Moga::Windows::Phone::ControllerState::Connection) == ControllerResult::Connected))
	{
		if(settings->GrayVControllerButtons)
		{
			this->spriteBatch->Draw(this->buttonsGraySRV.Get(), this->buttonsRectangle, nullptr, colorv);
		}else
		{
			this->spriteBatch->Draw(this->buttonsSRV.Get(), this->buttonsRectangle, nullptr, colorv);
		}
		if(settings->DPadStyle == 0 || settings->DPadStyle == 1)
		{
			this->spriteBatch->Draw(this->crossSRV.Get(), this->crossRectangle, nullptr, colorv);
		}else if(settings->DPadStyle == 2 || (settings->DPadStyle == 3 && this->controller->StickFingerDown()))
		{
			RECT centerRect;
			RECT stickRect;
			this->controller->GetStickRectangle(&stickRect);
			this->controller->GetStickCenterRectangle(&centerRect);

			this->spriteBatch->Draw(this->stickCenterSRV.Get(), centerRect, nullptr, colorv2);
			this->spriteBatch->Draw(this->stickSRV.Get(), stickRect, nullptr, colorv);
		}
		this->spriteBatch->Draw(this->startSelectSRV.Get(), this->startSelectRectangle, nullptr, colorv);
		this->spriteBatch->Draw(this->lButtonSRV.Get(), this->lRectangle, nullptr, colorv);
		this->spriteBatch->Draw(this->rButtonSRV.Get(), this->rRectangle, nullptr, colorv);
	}

	this->spriteBatch->End();

	frames++;
}

void *EmulatorRenderer::MapBuffer(int index, size_t *rowPitch)
{
	D3D11_MAPPED_SUBRESOURCE map;
	ZeroMemory(&map, sizeof(D3D11_MAPPED_SUBRESOURCE));

	DX::ThrowIfFailed(
		this->m_d3dContext->Map(this->buffers[index].Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &map)
		);

	*rowPitch = map.RowPitch;
	return map.pData;
}