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


#define AUTOSAVE_INTERVAL			60.0f

float lastElapsed = 0.0f;
int framesNotRendered = 0;
extern bool turbo;
extern bool enableTurboMode;

EmulatorRenderer::EmulatorRenderer()
{ 
	emulator = EmulatorGame::GetInstance();
	frontbuffer = 0;
	controller = nullptr; 
	autosaving = false;
	elapsedTime = 0.0f;
	settings = EmulatorSettings::Current;
	frames = 0;
	should_show_resume_text = false;

	useButtonColor = !settings->GrayVControllerButtons;

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
	Renderer::CreateDeviceResources();


	if(this->controller)
	{
		this->controller->UpdateFormat(this->format);
	}

	

	// Map backbuffer so it can be unmapped on first update
	int backbuffer = (this->frontbuffer + 1) % 2;
	this->backbufferPtr = (uint16 *) this->MapBuffer(backbuffer, &this->pitch);

	
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

		if(ctrl->GetKeyCode(KeyCode::A) == ControllerAction::Pressed )
		{
			GetMogaMapping(settings->MogaA, &a, &b, &x, &y, &l, &r );
		}

		
		if(ctrl->GetKeyCode(KeyCode::B) == ControllerAction::Pressed)
			GetMogaMapping(settings->MogaB, &a, &b, &x, &y, &l, &r );


		if (ctrl->GetKeyCode(KeyCode::X) == ControllerAction::Pressed)
			GetMogaMapping(settings->MogaX, &a, &b, &x, &y, &l, &r );


		if(ctrl->GetKeyCode(KeyCode::Y) == ControllerAction::Pressed)
			GetMogaMapping(settings->MogaY, &a, &b, &x, &y, &l, &r );

		if(ctrl->GetKeyCode(KeyCode::L1) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaL1, &a, &b, &x, &y, &l, &r );

		if (abs(ctrl->GetAxisValue(Axis::LeftTrigger)) > 0.5f)
			GetMogaMapping(settings->MogaL2, &a, &b, &x, &y, &l, &r );


		if(ctrl->GetKeyCode(KeyCode::R1) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaR1, &a, &b, &x, &y, &l, &r );


		if (abs(ctrl->GetAxisValue(Axis::RightTrigger)) > 0.5f)
			GetMogaMapping(settings->MogaR2, &a, &b, &x, &y, &l, &r );

		if(ctrl->GetKeyCode(KeyCode::ThumbLeft) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaLeftJoystick, &a, &b, &x, &y, &l, &r );

		if(ctrl->GetKeyCode(KeyCode::ThumbRight) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaRightJoystick, &a, &b, &x, &y, &l, &r );

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


	//set render parameter
	float opacity = 1.0f;
	if(this->orientation != ORIENTATION_PORTRAIT || !this->useButtonColor) //only opacity in landscape or when using simple button
		opacity = this->settings->ControllerOpacity / 100.0f;


	XMFLOAT4A color = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity);
	XMFLOAT4A color2 = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity + 0.2f);
	joystick_color = color;
	joystick_center_color = color2;
	l_color = color;
	r_color = color;
	select_color = color;
	start_color = color;
	a_color = color;
	b_color = color;
	x_color = color;
	y_color = color;

	float text_opacity = (sinf(timeTotal*2) + 1.0f) / 2.0f;
	resume_text_color = XMFLOAT4A(1.0f, 0.0f, 0.0f, text_opacity);

	if(settings->DPadStyle == 0 || settings->DPadStyle == 1)
		pad_to_draw = 0;
	else if(settings->DPadStyle == 2 || (settings->DPadStyle == 3 && this->controller->StickFingerDown()))
		pad_to_draw = 1;
	else
		pad_to_draw = -1;
}


void EmulatorRenderer::GetMogaMapping(int pressedButton, bool* a, bool* b, bool* x, bool* y, bool* l, bool* r )
{
	if (pressedButton & 1)
		*a = true;
	if (pressedButton & 2)
		*b = true;
	if (pressedButton & 4)
		*x = true;
	if (pressedButton & 8)
		*y = true;
	if (pressedButton & 16) 
		*l = true;
	if (pressedButton & 32)
		*r = true;
	
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


	float bgcolor[] = { 0.0f, 0.0f, 0.0f, 1.000f }; //black
	if (this->useButtonColor && this->orientation == ORIENTATION_PORTRAIT)
	{
		bgcolor[0] = (float)this->settings->BgcolorR / 255;
		bgcolor[1] = (float)this->settings->BgcolorG / 255;
		bgcolor[2] = (float)this->settings->BgcolorB / 255;
	}


	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		bgcolor
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
			
	this->controller->GetARectangle(&aRectangle);
	this->controller->GetBRectangle(&bRectangle);
	this->controller->GetXRectangle(&xRectangle);
	this->controller->GetYRectangle(&yRectangle);
	this->controller->GetCrossRectangle(&crossRectangle);
	this->controller->GetStartRectangle(&startRectangle);
	this->controller->GetSelectRectangle(&selectRectangle);
	this->controller->GetLRectangle(&lRectangle);
	this->controller->GetRRectangle(&rRectangle);
	this->controller->GetStickRectangle(&stickRect);
	this->controller->GetStickCenterRectangle(&centerRect);

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

	//display resume text if paused
	if(should_show_resume_text)
	{
		int textWidth = 0.5*width;
		int textHeight = textWidth / 480.0f * 80.0f;

		RECT resume_text_rect = {(width - textWidth) / 2, (height - textHeight) / 2, (width + textWidth) / 2, (height + textHeight) / 2};

		this->spriteBatch->Draw(this->resumeTextSRV.Get(), resume_text_rect, nullptr, XMLoadFloat4A(&resume_text_color));


	}

	//===draw virtual controller if moga controller is not loaded
	using namespace Moga::Windows::Phone;
	Moga::Windows::Phone::ControllerManager^ ctrl = Direct3DBackground::getController();
	if (!(EmulatorSettings::Current->UseMogaController && ctrl != nullptr && ctrl->GetState(Moga::Windows::Phone::ControllerState::Connection) == ControllerResult::Connected))
	{
		this->DrawController();
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