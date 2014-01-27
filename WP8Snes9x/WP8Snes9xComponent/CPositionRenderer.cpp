#include "pch.h"
#include "CPositionRenderer.h"
#include "DDSTextureLoader.h"
#include "CPositionComponent.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;
using namespace Windows::Graphics::Display;

CPositionRenderer::CPositionRenderer()
{ 

	frontbuffer = 0;
	controller = nullptr; 
	elapsedTime = 0.0f;
	settings = EmulatorSettings::Current;
	frames = 0;

	useButtonColor = false;

}


CPositionRenderer::~CPositionRenderer(void)
{


	delete this->spriteBatch;
	this->spriteBatch = nullptr;

	delete this->commonStates;
	this->commonStates = nullptr;
}

void CPositionRenderer::CreateDeviceResources()
{
	Renderer::CreateDeviceResources();
	
}


void CPositionRenderer::UpdateForWindowSizeChange(float width, float height)
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


void CPositionRenderer::UpdateController(void)
{
	if(this->controller)
	{
		this->controller->UpdateFormat(this->format);
	}
}


void CPositionRenderer::SetVirtualController(CPositionVirtualController *controller)
{
	this->controller = controller;
	this->controller->SetOrientation(this->orientation);
	this->controller->UpdateFormat(this->format);
	
}


void CPositionRenderer::ChangeOrientation(int orientation)
{
	this->orientation = orientation;
	if(this->controller)
	{
		this->controller->SetOrientation(this->orientation);
	}
	this->CreateTransformMatrix();
}

void CPositionRenderer::Update(float timeTotal, float timeDelta)
{
	cstate = controller->GetControllerState();
	
	float opacity = 0.5; //this->settings->ControllerOpacity / 100.0f;
	XMFLOAT4A color = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity);
	XMFLOAT4A color2 = XMFLOAT4A(1.0f, 0.0f, 0.0f, opacity);

	
	joystick_color = color;
	joystick_center_color = color;
	l_color = color;
	r_color = color;
	select_color = color;
	start_color = color;
	a_color = color;
	b_color = color;
	x_color = color;
	y_color = color;

	//change color if button is pressed
	if (cstate->JoystickPressed)
	{
		joystick_color = color2;
		joystick_center_color = color2;
	}

	if (cstate->LPressed)
		l_color = color2;

	if (cstate->RPressed)
		r_color = color2;

	if (cstate->APressed)
		a_color = color2;

	if (cstate->BPressed)
		b_color = color2;

	if (cstate->XPressed)
		x_color = color2;

	if (cstate->YPressed)
		y_color = color2;

	if (cstate->StartPressed)
		start_color = color2;

	if (cstate->SelectPressed)
		select_color = color2;

	this->controller->GetStickRectangle(&stickRect);
	this->controller->GetStickCenterRectangle(&centerRect);

	if(settings->DPadStyle == 0 || settings->DPadStyle == 1)
		pad_to_draw = 0;
	else if(settings->DPadStyle == 2 || settings->DPadStyle == 3)
		pad_to_draw = 1;


}


void CPositionRenderer::Render()
{

	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);

	const float white[] = { 1.0f, 1.0f, 1.0f, 1.000f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		white
		);

	m_d3dContext->ClearDepthStencilView(
		m_depthStencilView.Get(),
		D3D11_CLEAR_DEPTH,
		1.0f,
		0
		);

	

			
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


	
	//begin drawing
	this->spriteBatch->Begin(
		DirectX::SpriteSortMode::SpriteSortMode_Deferred, 
		this->alphablend.Get(), nullptr, nullptr, nullptr, nullptr, 
		this->outputTransform);



	//===draw virtual controller 
	this->DrawController();


	this->spriteBatch->End();

	frames++;
}