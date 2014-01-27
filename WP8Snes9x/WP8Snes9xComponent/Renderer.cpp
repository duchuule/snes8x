#include "pch.h"
#include "Renderer.h"
#include "EmulatorFileHandler.h"
#include "DDSTextureLoader.h"
#include "WP8Snes9xComponent.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;
using namespace Windows::Graphics::Display;




Renderer::Renderer()
{ 

}

Renderer::~Renderer(void)
{

}

void Renderer::CreateDeviceResources()
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
		this->m_d3dDevice.Get(), RESUME_TEXTURE_FILE_NAME,
		this->resumeTextResource.GetAddressOf(),
		this->resumeTextSRV.GetAddressOf())
		);


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

	if (useButtonColor)
	{
		DX::ThrowIfFailed(
			CreateDDSTextureFromFile(
			this->m_d3dDevice.Get(), A_TEXTURE_FILE_NAME,
			this->aResource.GetAddressOf(),
			this->aSRV.GetAddressOf())
			);
		DX::ThrowIfFailed(
			CreateDDSTextureFromFile(
			this->m_d3dDevice.Get(), B_TEXTURE_FILE_NAME,
			this->bResource.GetAddressOf(),
			this->bSRV.GetAddressOf())
			);
		DX::ThrowIfFailed(
			CreateDDSTextureFromFile(
			this->m_d3dDevice.Get(), X_TEXTURE_FILE_NAME,
			this->xResource.GetAddressOf(),
			this->xSRV.GetAddressOf())
			);
		DX::ThrowIfFailed(
			CreateDDSTextureFromFile(
			this->m_d3dDevice.Get(), Y_TEXTURE_FILE_NAME,
			this->yResource.GetAddressOf(),
			this->ySRV.GetAddressOf())
			);
	}
	else
	{

		DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), A_GRAY_TEXTURE_FILE_NAME,
		this->aResource.GetAddressOf(),
		this->aSRV.GetAddressOf())
		);

		DX::ThrowIfFailed(
			CreateDDSTextureFromFile(
			this->m_d3dDevice.Get(), B_GRAY_TEXTURE_FILE_NAME,
			this->bResource.GetAddressOf(),
			this->bSRV.GetAddressOf())
			);
		DX::ThrowIfFailed(
			CreateDDSTextureFromFile(
			this->m_d3dDevice.Get(), X_GRAY_TEXTURE_FILE_NAME,
			this->xResource.GetAddressOf(),
			this->xSRV.GetAddressOf())
			);
		DX::ThrowIfFailed(
			CreateDDSTextureFromFile(
			this->m_d3dDevice.Get(), Y_GRAY_TEXTURE_FILE_NAME,
			this->yResource.GetAddressOf(),
			this->ySRV.GetAddressOf())
			);
	}
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), START_TEXTURE_FILE_NAME,
		this->startResource.GetAddressOf(),
		this->startSRV.GetAddressOf())
		);
	DX::ThrowIfFailed(
		CreateDDSTextureFromFile(
		this->m_d3dDevice.Get(), SELECT_TEXTURE_FILE_NAME,
		this->selectResource.GetAddressOf(),
		this->selectSRV.GetAddressOf())
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


	//blend state
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

void Renderer::CreateWindowSizeDependentResources()
{
	Direct3DBase::CreateWindowSizeDependentResources();
}

void Renderer::UpdateForWindowSizeChange(float width, float height)
{
	Direct3DBase::UpdateForWindowSizeChange(width, height);

}




void Renderer::CreateTransformMatrix(void)
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

void Renderer::DrawController(void)
{

	//ABXY button
	this->spriteBatch->Draw(this->aSRV.Get(), this->aRectangle, nullptr, XMLoadFloat4A(&a_color));
	this->spriteBatch->Draw(this->bSRV.Get(), this->bRectangle, nullptr, XMLoadFloat4A(&b_color));
	this->spriteBatch->Draw(this->xSRV.Get(), this->xRectangle, nullptr, XMLoadFloat4A(&x_color));
	this->spriteBatch->Draw(this->ySRV.Get(), this->yRectangle, nullptr, XMLoadFloat4A(&y_color));

	//pad
	if(pad_to_draw == 0)
	{
		this->spriteBatch->Draw(this->crossSRV.Get(), this->crossRectangle, nullptr, XMLoadFloat4A(&joystick_color));

	}else if(pad_to_draw == 1)
	{
	
		this->spriteBatch->Draw(this->stickCenterSRV.Get(), centerRect, nullptr, XMLoadFloat4A(&joystick_center_color));
		this->spriteBatch->Draw(this->stickSRV.Get(), stickRect, nullptr, XMLoadFloat4A(&joystick_color));
	}

	//start-select buttons
	this->spriteBatch->Draw(this->startSRV.Get(), this->startRectangle, nullptr, XMLoadFloat4A(&start_color));
	this->spriteBatch->Draw(this->selectSRV.Get(), this->selectRectangle, nullptr, XMLoadFloat4A(&select_color));

	//L-R buttons
	this->spriteBatch->Draw(this->lButtonSRV.Get(), this->lRectangle, nullptr, XMLoadFloat4A(&l_color));
	this->spriteBatch->Draw(this->rButtonSRV.Get(), this->rRectangle, nullptr, XMLoadFloat4A(&r_color));

}

void Renderer::Update(float timeTotal, float timeDelta)
{
}


void Renderer::Render()
{
	

}
