#include "pch.h"
#include "WP8Snes9xComponent.h"
#include "Direct3DContentProvider.h"
#include "EmulatorFileHandler.h"

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
using namespace Windows::ApplicationModel::Core;

Moga::Windows::Phone::ControllerManager^ Direct3DBackground::mogacontroller;
Windows::Devices::Sensors::Accelerometer^ Direct3DBackground::m_accelerometer;
Windows::Devices::Sensors::Inclinometer^ Direct3DBackground::m_inclinometer;



extern bool cameraPressed;

namespace PhoneDirect3DXamlAppComponent
{

	Direct3DBackground::Direct3DBackground() :
		m_timer(ref new BasicTimer())
	{
	}

	IDrawingSurfaceBackgroundContentProvider^ Direct3DBackground::CreateContentProvider()
	{
		ComPtr<Direct3DContentProvider> provider = Make<Direct3DContentProvider>(this);
		return reinterpret_cast<IDrawingSurfaceBackgroundContentProvider^>(provider.Get());

	}



	// IDrawingSurfaceManipulationHandler
	void Direct3DBackground::SetManipulationHost(DrawingSurfaceManipulationHost^ manipulationHost)
	{
		manipulationHost->PointerPressed +=
			ref new TypedEventHandler<DrawingSurfaceManipulationHost^, PointerEventArgs^>(this, &Direct3DBackground::OnPointerPressed);

		manipulationHost->PointerMoved +=
			ref new TypedEventHandler<DrawingSurfaceManipulationHost^, PointerEventArgs^>(this, &Direct3DBackground::OnPointerMoved);

		manipulationHost->PointerReleased +=
			ref new TypedEventHandler<DrawingSurfaceManipulationHost^, PointerEventArgs^>(this, &Direct3DBackground::OnPointerReleased);
	}

	// Event Handlers
	void Direct3DBackground::OnPointerPressed(DrawingSurfaceManipulationHost^ sender, PointerEventArgs^ args)
	{
		this->vController->PointerPressed(args->CurrentPoint);
	}

	void Direct3DBackground::OnPointerMoved(DrawingSurfaceManipulationHost^ sender, PointerEventArgs^ args)
	{
		this->vController->PointerMoved(args->CurrentPoint);
	}

	void Direct3DBackground::OnPointerReleased(DrawingSurfaceManipulationHost^ sender, PointerEventArgs^ args)
	{
		this->vController->PointerReleased(args->CurrentPoint);
	}

	void Direct3DBackground::SetContinueNotifier(PhoneDirect3DXamlAppComponent::ContinueEmulationNotifier ^notifier)
	{
		this->ContinueEmulationNotifier = notifier;
	}




	void Direct3DBackground::ChangeOrientation(int orientation)
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

	void Direct3DBackground::ToggleCameraPress(void)
	{
		cameraPressed = !cameraPressed;
	}

	void Direct3DBackground::StartCameraPress(void)
	{
		cameraPressed = true;
	}

	void Direct3DBackground::StopCameraPress(void)
	{
		cameraPressed = false;
	}

	bool Direct3DBackground::IsROMLoaded(void)
	{
		return this->emulator->IsROMLoaded();
	}

	void Direct3DBackground::PauseEmulation(void)
	{
		this->emulator->Pause();
		this->m_renderer->should_show_resume_text = true;
	}

	void Direct3DBackground::UnpauseEmulation(void)
	{
		this->m_renderer->should_show_resume_text = false;
		this->emulator->Unpause();
	}

	void Direct3DBackground::SelectSaveState(int slot)
	{
		int oldSlot = SavestateSlot;
		SavestateSlot = slot % MAX_SAVESTATE_SLOTS;
		LoadstateSlot = SavestateSlot;
		if(this->SavestateSelected)
		{
			this->SavestateSelected(SavestateSlot, oldSlot);  //call the c# code to change the UI about selected save state
		}
	}

	int Direct3DBackground::GetCurrentSaveSlot(void)
	{
		return SavestateSlot;

	}

	void Direct3DBackground::SaveState(void)
	{
		this->m_renderer->should_show_resume_text = false;

		SaveStateAsync().then([this]()
		{
			this->emulator->Unpause();
			this->SavestateCreated(SavestateSlot, ROMFile->Name);
			if(EmulatorSettings::Current->AutoIncrementSavestates)
			{
				int oldSlot = SavestateSlot;
				int tmp = (oldSlot == AUTOSAVE_SLOT) ? -1 : 0;
				SavestateSlot = (oldSlot + 1 + tmp) % (MAX_SAVESTATE_SLOTS - 1); // -1 because last one is auto save
				LoadstateSlot = oldSlot;
				if(this->SavestateSelected)
				{
					this->SavestateSelected(SavestateSlot, oldSlot);
				}
			}
		});
		
		this->ContinueEmulationNotifier();
	}

	void Direct3DBackground::LoadState(int slot)
	{
		this->m_renderer->should_show_resume_text = false;

		LoadStateAsync(slot).then([this]()
		{
			this->emulator->Unpause();
		});
		this->ContinueEmulationNotifier();
	}

	void Direct3DBackground::Reset(void)
	{
		this->m_renderer->should_show_resume_text = false;

		if(emulator->IsROMLoaded())
		{
			//Emulator::ResetAsync();
			Emulator::ResetSync();
			this->ContinueEmulationNotifier();
		}
	}

	void Direct3DBackground::LoadROMAsync(StorageFile ^file, StorageFolder ^folder)
	{
		Emulator::LoadROMAsync(file, folder);
	}

	// Interface With Direct3DContentProvider
	HRESULT Direct3DBackground::Connect(_In_ IDrawingSurfaceRuntimeHostNative* host, _In_ ID3D11Device1* device)
	{
		waitHandle = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);

		m_renderer = ref new EmulatorRenderer();
		vController = new VirtualController();
		m_renderer->Initialize(device);
		m_renderer->SetVirtualController(this->vController);
		m_renderer->UpdateForWindowSizeChange(WindowBounds.Width, WindowBounds.Height);

		//moga controler
		//NOTE: NEED TO ENABLE CAPABILITY PROXIMITY!!!!!!
		if (EmulatorSettings::Current->UseMogaController)
		{
			if (mogacontroller == nullptr)
			{
				try 
				{
					mogacontroller = ref new Moga::Windows::Phone::ControllerManager();
					mogacontroller->Connect() ;
				}
				catch(Platform::Exception^ ex)
				{
				}
			}
		}
		//motion control
		if (EmulatorSettings::Current->UseMotionControl == 1)
		{
			if (m_accelerometer == nullptr)
			{
				try
				{
					m_accelerometer = Windows::Devices::Sensors::Accelerometer::GetDefault();
				}
				catch (Platform::Exception^ ex)
				{
				}
			}
		}
		else if (EmulatorSettings::Current->UseMotionControl == 2)
		{
			if (m_inclinometer == nullptr)
			{
				try
				{
					m_inclinometer = Windows::Devices::Sensors::Inclinometer::GetDefault();
				}
				catch (Platform::Exception^ ex)
				{
				}
			}
		}

		Settings.Mute = !EmulatorSettings::Current->SoundEnabled;
		this->emulator = EmulatorGame::GetInstance();
		this->emulator->Resume();
		if(this->emulator->IsROMLoaded())
		{

			this->emulator->Unpause();
		}else
		{
			SavestateSlot = 0;
			LoadstateSlot = 0;
		}


		this->m_renderer->ChangeOrientation(orientation);

		// Restart timer after renderer has finished initializing.
		m_timer->Reset();

		return S_OK;
	}	

	void Direct3DBackground::TriggerSnapshot(void)
	{
		uint16 *backbuffer;
		size_t pitch;
		int width, height;
		this->m_renderer->GetBackbufferData(&backbuffer, &pitch, &width, &height);
		Platform::Array<unsigned short> ^pixels = GetSnapshotBuffer(backbuffer, pitch, width, height);
		this->SnapshotAvailable(pixels, 2 * width, ROMFile->Name);
	}

	void Direct3DBackground::Disconnect()
	{
		create_task([this]()
		{
			this->emulator->Pause();
			SaveSRAMAsync().wait();


			int oldstate = SavestateSlot;
			SavestateSlot = AUTOSAVE_SLOT;
			SaveStateAsync().wait();
			this->SavestateCreated(SavestateSlot, ROMFile->Name);

			SavestateSlot = oldstate;
			this->emulator->Unpause();
		}).then([this]()
		{
			if(!EmulatorSettings::Current->ManualSnapshots)
			{
				this->TriggerSnapshot();
			}
		}).then([this]()
		{
			this->emulator->Suspend();
			m_renderer = nullptr;
			delete this->vController;



			if(waitHandle)
			{
				SetEvent(waitHandle);
			}
		});

		WaitForSingleObjectEx(waitHandle, 1500, false);

		CloseHandle(waitHandle);
		waitHandle = nullptr;

	}

	HRESULT Direct3DBackground::PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Inout_ DrawingSurfaceSizeF* desiredRenderTargetSize)
	{
		m_timer->Update();
		m_renderer->Update(m_timer->Total, m_timer->Delta);

		desiredRenderTargetSize->width = RenderResolution.Width;
		desiredRenderTargetSize->height = RenderResolution.Height;

		return S_OK;
	}

	HRESULT Direct3DBackground::Draw(_In_ ID3D11Device1* device, _In_ ID3D11DeviceContext1* context, _In_ ID3D11RenderTargetView* renderTargetView)
	{
		m_renderer->UpdateDevice(device, context, renderTargetView);
		m_renderer->Render();

		RequestAdditionalFrame();

		return S_OK;
	}

}