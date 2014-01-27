#include "pch.h"
#include "CPositionComponent.h"
#include "CPositionContentProvider.h"


#if _DEBUG
#include <string>
#include <sstream>

using namespace std;
#endif

using namespace Windows::Storage;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;
using namespace Microsoft::WRL;
using namespace Windows::Phone::Graphics::Interop;
using namespace Windows::Phone::Input::Interop;




namespace PhoneDirect3DXamlAppComponent
{

	CPositionDirect3DBackground::CPositionDirect3DBackground() :
		m_timer(ref new BasicTimer())
	{
	}

	IDrawingSurfaceBackgroundContentProvider^ CPositionDirect3DBackground::CreateContentProvider()
	{
		ComPtr<CPositionDirect3DContentProvider> provider = Make<CPositionDirect3DContentProvider>(this);
		return reinterpret_cast<IDrawingSurfaceBackgroundContentProvider^>(provider.Detach());
	}

	// IDrawingSurfaceManipulationHandler
	void CPositionDirect3DBackground::SetManipulationHost(DrawingSurfaceManipulationHost^ manipulationHost)
	{
		manipulationHost->PointerPressed +=
			ref new TypedEventHandler<DrawingSurfaceManipulationHost^, PointerEventArgs^>(this, &CPositionDirect3DBackground::OnPointerPressed);

		manipulationHost->PointerMoved +=
			ref new TypedEventHandler<DrawingSurfaceManipulationHost^, PointerEventArgs^>(this, &CPositionDirect3DBackground::OnPointerMoved);

		manipulationHost->PointerReleased +=
			ref new TypedEventHandler<DrawingSurfaceManipulationHost^, PointerEventArgs^>(this, &CPositionDirect3DBackground::OnPointerReleased);
	}

	// Event Handlers
	void CPositionDirect3DBackground::OnPointerPressed(DrawingSurfaceManipulationHost^ sender, PointerEventArgs^ args)
	{
		this->vController->PointerPressed(args->CurrentPoint);
	}

	void CPositionDirect3DBackground::OnPointerMoved(DrawingSurfaceManipulationHost^ sender, PointerEventArgs^ args)
	{
		this->vController->PointerMoved(args->CurrentPoint);
	}

	void CPositionDirect3DBackground::OnPointerReleased(DrawingSurfaceManipulationHost^ sender, PointerEventArgs^ args)
	{
		this->vController->PointerReleased(args->CurrentPoint);
	}


	void CPositionDirect3DBackground::ChangeOrientation(int orientation)
	{
#if _DEBUG
		wstringstream wss;
		wss << L"Orientation: " << orientation;
		wss << L"\n";
		OutputDebugStringW(wss.str().c_str());
#endif
		this->orientation = orientation;
		if(this->m_renderer != nullptr)
		{
			this->m_renderer->ChangeOrientation(orientation);//FlipOutput(flip);
		}
	}


	void CPositionDirect3DBackground::SetControllerPosition(Windows::Foundation::Collections::IVector<int>^ cpos)
	{
		this->vController->SetControllerPosition(cpos);

	}

	void CPositionDirect3DBackground::GetControllerPosition(Windows::Foundation::Collections::IVector<int>^ ret)
	{
		this->vController->GetControllerPosition(ret);
	}


	// Interface With Direct3DContentProvider
	HRESULT CPositionDirect3DBackground::Connect(_In_ IDrawingSurfaceRuntimeHostNative* host, _In_ ID3D11Device1* device)
	{

		m_renderer = ref new CPositionRenderer();
		vController = new CPositionVirtualController();
		m_renderer->Initialize(device);
		m_renderer->SetVirtualController(this->vController);
		m_renderer->UpdateForWindowSizeChange(WindowBounds.Width, WindowBounds.Height);

	

		//this->m_renderer->ChangeOrientation(orientation);

		// Restart timer after renderer has finished initializing.
		m_timer->Reset();

		return S_OK;
	}	



	void CPositionDirect3DBackground::Disconnect()
	{

		m_renderer = nullptr;
		delete this->vController;

	}

	HRESULT CPositionDirect3DBackground::PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Inout_ DrawingSurfaceSizeF* desiredRenderTargetSize)
	{
		m_timer->Update();
		m_renderer->Update(m_timer->Total, m_timer->Delta);

		desiredRenderTargetSize->width = RenderResolution.Width;
		desiredRenderTargetSize->height = RenderResolution.Height;

		return S_OK;
	}

	HRESULT CPositionDirect3DBackground::Draw(_In_ ID3D11Device1* device, _In_ ID3D11DeviceContext1* context, _In_ ID3D11RenderTargetView* renderTargetView)
	{
		m_renderer->UpdateDevice(device, context, renderTargetView);
		m_renderer->Render();

		RequestAdditionalFrame();

		return S_OK;
	}



}