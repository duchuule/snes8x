#pragma once

#include "pch.h"
#include "BasicTimer.h"
#include "CPositionRenderer.h"
#include "CPositionVirtualController.h"
#include "EmulatorSettings.h"
#include <DrawingSurfaceNative.h>

using namespace Emulator;

namespace PhoneDirect3DXamlAppComponent
{
	
	public delegate void RequestAdditionalFrameHandler();
	public delegate void ContinueEmulationNotifier(void);

	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class CPositionDirect3DBackground sealed : public Windows::Phone::Input::Interop::IDrawingSurfaceManipulationHandler
	{	
	public:
		CPositionDirect3DBackground();

		Windows::Phone::Graphics::Interop::IDrawingSurfaceBackgroundContentProvider^ CreateContentProvider();

		// IDrawingSurfaceManipulationHandler
		virtual void SetManipulationHost(Windows::Phone::Input::Interop::DrawingSurfaceManipulationHost^ manipulationHost);

		event RequestAdditionalFrameHandler^ RequestAdditionalFrame;


		property Windows::Foundation::Size WindowBounds;
		property Windows::Foundation::Size NativeResolution;
		property Windows::Foundation::Size RenderResolution;



		void ChangeOrientation(int orientation);
		void CPositionDirect3DBackground::SetControllerPosition(Windows::Foundation::Collections::IVector<int>^ cpos);
		void CPositionDirect3DBackground::GetControllerPosition(Windows::Foundation::Collections::IVector<int>^ ret);


	protected:
		// Event Handlers
		void OnPointerPressed(Windows::Phone::Input::Interop::DrawingSurfaceManipulationHost^ sender, Windows::UI::Core::PointerEventArgs^ args);
		void OnPointerReleased(Windows::Phone::Input::Interop::DrawingSurfaceManipulationHost^ sender, Windows::UI::Core::PointerEventArgs^ args);
		void OnPointerMoved(Windows::Phone::Input::Interop::DrawingSurfaceManipulationHost^ sender, Windows::UI::Core::PointerEventArgs^ args);

	internal:
		HRESULT Connect(_In_ IDrawingSurfaceRuntimeHostNative* host, _In_ ID3D11Device1* device);
		void Disconnect();

		HRESULT PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Inout_ DrawingSurfaceSizeF* desiredRenderTargetSize);
		HRESULT Draw(_In_ ID3D11Device1* device, _In_ ID3D11DeviceContext1* context, _In_ ID3D11RenderTargetView* renderTargetView);

	private:
		ContinueEmulationNotifier ^ContinueEmulationNotifier;
		CPositionRenderer^ m_renderer;
		BasicTimer^ m_timer;
		CPositionVirtualController *vController;
		int orientation;
};

}