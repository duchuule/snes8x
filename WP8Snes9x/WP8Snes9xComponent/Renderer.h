#pragma once

#include <ppltasks.h>
#include "Direct3DBase.h"
#include "Emulator.h"
#include "CommonStates.h"
#include "defines.h"
#include "VirtualController.h"
#include "EmulatorSettings.h"
#include <collection.h>

using namespace Emulator;
using namespace DirectX;
using namespace concurrency;
using namespace Windows::Storage;
using namespace Windows::UI::Input;
using namespace Microsoft::WRL;
using namespace PhoneDirect3DXamlAppComponent;

#define CROSS_GRAY_TEXTURE_FILE_NAME				L"Assets/pad_cross_gray.dds"
#define A_GRAY_TEXTURE_FILE_NAME					L"Assets/pad_a_buttons_gray.dds"
#define B_GRAY_TEXTURE_FILE_NAME					L"Assets/pad_b_buttons_gray.dds"
#define X_GRAY_TEXTURE_FILE_NAME					L"Assets/pad_x_buttons_gray.dds"
#define Y_GRAY_TEXTURE_FILE_NAME					L"Assets/pad_y_buttons_gray.dds"
#define START_GRAY_TEXTURE_FILE_NAME				L"Assets/pad_start_gray.dds"
#define SELECT_GRAY_TEXTURE_FILE_NAME				L"Assets/pad_select_gray.dds"
#define L_GRAY_TEXTURE_FILE_NAME					L"Assets/pad_l_button_gray.dds"
#define R_GRAY_TEXTURE_FILE_NAME					L"Assets/pad_r_button_gray.dds"
#define STICK_GRAY_TEXTURE_FILE_NAME				L"Assets/ThumbStick_gray.dds"


#define CROSS_TEXTURE_FILE_NAME						L"Assets/pad_cross.dds"
#define A_TEXTURE_FILE_NAME							L"Assets/pad_a_buttons.dds"
#define B_TEXTURE_FILE_NAME							L"Assets/pad_b_buttons.dds"
#define X_TEXTURE_FILE_NAME							L"Assets/pad_x_buttons.dds"
#define Y_TEXTURE_FILE_NAME							L"Assets/pad_y_buttons.dds"
#define START_TEXTURE_FILE_NAME						L"Assets/pad_start.dds"
#define SELECT_TEXTURE_FILE_NAME					L"Assets/pad_select.dds"
#define L_TEXTURE_FILE_NAME							L"Assets/pad_l_button.dds"
#define R_TEXTURE_FILE_NAME							L"Assets/pad_r_button.dds"
#define STICK_TEXTURE_FILE_NAME						L"Assets/ThumbStick.dds"

#define STICK_CENTER_TEXTURE_FILE_NAME				L"Assets/ThumbStickCenter.dds"
#define RESUME_TEXTURE_FILE_NAME					L"Assets/resumetext.dds"

#define AUTOSAVE_INTERVAL			60.0f

// This class renders a simple spinning cube.
ref class Renderer abstract : public Direct3DBase
{
public:
	
	virtual ~Renderer(void);

protected:
	void DrawController(void);	

internal:
	Renderer();
	// Direct3DBase methods.
	virtual void CreateDeviceResources() override;
	virtual void CreateWindowSizeDependentResources() override;
	virtual void Render() override;
	virtual void UpdateForWindowSizeChange(float width, float height) override;
	
	// Method for updating time-dependent objects.
	virtual void Update(float timeTotal, float timeDelta);


	size_t pitch;
	uint16 *backbufferPtr;
	float elapsedTime;
	bool autosaving;
	HANDLE waitEvent;
	int frames;

	bool useButtonColor; 
	RECT centerRect;
	RECT stickRect;
	RECT aRectangle;
	RECT bRectangle;
	RECT xRectangle;
	RECT yRectangle;
	RECT crossRectangle;
	RECT startRectangle;
	RECT selectRectangle;
	RECT lRectangle;
	RECT rRectangle;


	XMFLOAT4A joystick_color;
	XMFLOAT4A joystick_center_color;
	XMFLOAT4A l_color;
	XMFLOAT4A r_color;
	XMFLOAT4A select_color;
	XMFLOAT4A start_color;
	XMFLOAT4A a_color;
	XMFLOAT4A b_color;
	XMFLOAT4A x_color;
	XMFLOAT4A y_color;
	XMFLOAT4A resume_text_color;
	int pad_to_draw;


	int									orientation;
	int									format;
	int									frontbuffer;
	int									width, height;
	bool								should_show_resume_text;


	SpriteBatch							*spriteBatch;
	CommonStates						*commonStates;
	EmulatorGame						*emulator;
	ComPtr<ID3D11Texture2D>				buffers[2];
	ComPtr<ID3D11ShaderResourceView>	bufferSRVs[2];
	ComPtr<ID3D11BlendState>			alphablend;

	ComPtr<ID3D11Resource>				resumeTextResource;
	ComPtr<ID3D11ShaderResourceView>	resumeTextSRV;
	ComPtr<ID3D11Resource>				stickCenterResource;
	ComPtr<ID3D11ShaderResourceView>	stickCenterSRV;
	ComPtr<ID3D11Resource>				stickResource;
	ComPtr<ID3D11ShaderResourceView>	stickSRV;
	ComPtr<ID3D11Resource>				crossResource;
	ComPtr<ID3D11ShaderResourceView>	crossSRV;
	ComPtr<ID3D11Resource>				aResource;
	ComPtr<ID3D11ShaderResourceView>	bSRV;
	ComPtr<ID3D11Resource>				bResource;
	ComPtr<ID3D11ShaderResourceView>	xSRV;
	ComPtr<ID3D11Resource>				xResource;
	ComPtr<ID3D11ShaderResourceView>	ySRV;
	ComPtr<ID3D11Resource>				yResource;
	ComPtr<ID3D11ShaderResourceView>	aSRV;
	ComPtr<ID3D11Resource>				startResource;
	ComPtr<ID3D11ShaderResourceView>	startSRV;
	ComPtr<ID3D11Resource>				selectResource;
	ComPtr<ID3D11ShaderResourceView>	selectSRV;
	ComPtr<ID3D11Resource>				lButtonResource;
	ComPtr<ID3D11ShaderResourceView>	lButtonSRV;
	ComPtr<ID3D11Resource>				rButtonResource;
	ComPtr<ID3D11ShaderResourceView>	rButtonSRV;
	EmulatorSettings					^settings;
	XMMATRIX							outputTransform;
	
	void CreateTransformMatrix(void);

};
