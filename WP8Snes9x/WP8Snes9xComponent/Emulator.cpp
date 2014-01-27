#include "pch.h"
#include "Emulator.h"
#include "snes9x.h"
#include "memmap.h"
#include "cpuexec.h"
#include "display.h"
#include "apu/apu.h"
#include "controls.h"
#include "snapshot.h"
#include "EmulatorFileHandler.h"
#include "EmulatorSettings.h"
#include <ppltasks.h>
#include <windows.h>

using namespace concurrency;
using namespace Windows::Foundation;
using namespace Windows::System::Threading;
using namespace PhoneDirect3DXamlAppComponent;

bool enableTurboMode = false;
bool turbo = false;


namespace Emulator
{
	// Singleton
	EmulatorGame EmulatorGame::instance;
	bool EmulatorGame::initialized = false;

	int EmulatorGame::SnesImageWidth = 0;
	int EmulatorGame::SnesImageHeight = 0;
	IS9xSoundOutput *EmulatorGame::Audio = nullptr;
	
	float lastElapsedTime = 0.0f;
	bool lastSkipped = false;

	EmulatorGame *EmulatorGame::GetInstance(void)
	{
		if(!initialized)
		{
			EmulatorGame::instance.Initialize();
			initialized = true;
		}
		return &EmulatorGame::instance;
	}


	EmulatorGame::EmulatorGame(void)
		: stopThread(false), updateCount(0), frameSkipped(false), threadAction(nullptr)
	{ 
	}

	void EmulatorGame::Initialize(void)
	{
		this->InitEmulator();

		this->swapEvent = CreateEventEx(NULL, NULL, CREATE_EVENT_INITIAL_SET, EVENT_ALL_ACCESS);
		this->updateEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
		this->sleepEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
		this->endEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);

		InitializeCriticalSectionEx(&this->cs, 0, 0);
		InitializeCriticalSectionEx(&this->pauseSync, NULL, NULL);

		// Create and start Thread
		//this->InitThread();
	}

	void EmulatorGame::InitEmulator(void)
	{
		memset(&Settings, 0, sizeof(Settings));
		Settings.MouseMaster = true;
		Settings.SuperScopeMaster = true;
		Settings.MultiPlayer5Master = true;
		Settings.JustifierMaster = true;
		Settings.BlockInvalidVRAMAccess = true;
		//Settings.HDMATimingHack = 84;
		Settings.SoundSync = true;
		Settings.SoundPlaybackRate = 32000;
		Settings.Stereo = true;
		Settings.SixteenBitSound = true;
		Settings.Transparency = true;
		Settings.SupportHiRes = true;
		//Settings.SkipFrames = 5;
		//Settings.StopEmulation = true;

		Settings.FrameTimeNTSC = 16667;
		Settings.FrameTimePAL = 20000;

		/*Settings.SkipFrames = 10;
		Settings.TurboSkipFrames = 19;
		Settings.TurboMode = true;*/

		gfxbuffer = new BYTE[EXT_PITCH * EXT_HEIGHT];
		
		GFX.Screen = (uint16 *) gfxbuffer;
		GFX.Pitch = EXT_PITCH;
		GFX.RealPPL = EXT_PITCH;

		Memory.Init();		
		S9xInitAPU();

		S9xSetRenderPixelFormat(RGB565);
		S9xGraphicsInit();
		IPPU.RenderThisFrame = false;

		S9xMapButton(JOYPAD_START, S9xGetCommandT("Joypad1 Start"), false);
		S9xMapButton(JOYPAD_SELECT, S9xGetCommandT("Joypad1 Select"), false);
		S9xMapButton(JOYPAD_A, S9xGetCommandT("Joypad1 A"), false);
		S9xMapButton(JOYPAD_B, S9xGetCommandT("Joypad1 B"), false);
		S9xMapButton(JOYPAD_X, S9xGetCommandT("Joypad1 X"), false);
		S9xMapButton(JOYPAD_Y, S9xGetCommandT("Joypad1 Y"), false);
		S9xMapButton(JOYPAD_L, S9xGetCommandT("Joypad1 L"), false);
		S9xMapButton(JOYPAD_R, S9xGetCommandT("Joypad1 R"), false);
		S9xMapButton(JOYPAD_UP, S9xGetCommandT("Joypad1 Up"), false);
		S9xMapButton(JOYPAD_DOWN, S9xGetCommandT("Joypad1 Down"), false);
		S9xMapButton(JOYPAD_LEFT, S9xGetCommandT("Joypad1 Left"), false);
		S9xMapButton(JOYPAD_RIGHT, S9xGetCommandT("Joypad1 Right"), false);
		
		//this->InitSound();
	}

	void EmulatorGame::InitSound(void)
	{
		EmulatorGame::Audio = &this->audio;
		Settings.SoundInputRate = 31950;
		Settings.SoundPlaybackRate = 32000;
		this->audio.DeInitSoundOutput();
		S9xInitSound(128, 0);
	}

	void EmulatorGame::DeInitSound(void)
	{
		audio.DeInitSoundOutput();
		S9xSetSamplesAvailableCallback(nullptr, nullptr);
	}

	void EmulatorGame::InitThread(void)
	{
		EnterCriticalSection(&this->cs);

		if(this->threadAction)
		{
			LeaveCriticalSection(&this->cs);
			return;
		}

		this->stopThread = false;
		this->threadAction = ThreadPool::RunAsync(ref new WorkItemHandler([this](IAsyncAction ^action)
		{
			this->UpdateAsync();
		}), WorkItemPriority::High, WorkItemOptions::None);

		LeaveCriticalSection(&this->cs);
	}

	void EmulatorGame::DeInitThread(void)
	{
		EnterCriticalSection(&this->cs);

		if(this->stopThread)
		{
			LeaveCriticalSection(&this->cs);
			return;
		}

		this->stopThread = true;
		SetEvent(this->updateEvent);

		// Wait for thread termination
		WaitForSingleObjectEx(this->endEvent, INFINITE, false);

		this->threadAction = nullptr;

		LeaveCriticalSection(&this->cs);
	}

	void EmulatorGame::Resume(void)
	{		
		SetEvent(this->swapEvent);
		ResetEvent(this->updateEvent);

		this->InitSound();		
		this->InitThread();		
	}

	void EmulatorGame::Suspend(void)
	{
		this->DeInitThread();
		this->DeInitSound();
	}

	EmulatorGame::~EmulatorGame(void)
	{
		// Terminate thread
		this->DeInitThread();

		// Close handles
		CloseHandle(this->sleepEvent);
		CloseHandle(this->updateEvent);
		CloseHandle(this->swapEvent);
		DeleteCriticalSection(&this->cs);
		DeleteCriticalSection(&this->pauseSync);

		delete [] this->gfxbuffer;

		// Uninitialize Sound
		//audio.DeInitSoundOutput();
		//S9xSetSamplesAvailableCallback(nullptr, nullptr);
	}

	bool EmulatorGame::IsPaused(void)
	{
		return Settings.StopEmulation;
	}

	void EmulatorGame::Pause(void)
	{
		EnterCriticalSection(&this->pauseSync);
		Settings.StopEmulation = true;
		LeaveCriticalSection(&this->pauseSync);
	}

	void EmulatorGame::Unpause(void)
	{
		EnterCriticalSection(&this->pauseSync);
		Settings.StopEmulation = false;
		LeaveCriticalSection(&this->pauseSync);
	}
	
	bool EmulatorGame::IsROMLoaded(void)
	{
		return ROMFile && ROMFolder;
	}

	task<void> EmulatorGame::StopEmulationAsync(void)
	{
		return create_task([this]()
		{
			if(this->IsROMLoaded())
			{
				this->Pause();
				SaveSRAMAsync().wait();
				int oldstate = SavestateSlot;
				SavestateSlot = AUTOSAVE_SLOT;
				SaveStateAsync().wait();
				SavestateSlot = oldstate;
				this->InitSound();
				Memory.ClearSRAM();
				ROMFile = nullptr;
				ROMFolder = nullptr;
				updateCount = 0;
				frameSkipped = false;
			}
		});
	}

	void EmulatorGame::SetButtonState(int button, bool pressed)
	{
		S9xReportButton(button, pressed);
	}

	bool EmulatorGame::LastFrameSkipped(void)
	{
		return this->frameSkipped;
	}

	void EmulatorGame::UpdateAsync(void)
	{
		WaitForSingleObjectEx(this->updateEvent, INFINITE, false);
		while(!stopThread)
		{
			EnterCriticalSection(&this->pauseSync);
			if(!Settings.StopEmulation)
			{
				if(Settings.PAL && updateCount >= 5)
				{
					updateCount = 0;
					this->frameSkipped = true;
				}else
				{
					EmulatorSettings ^settings = EmulatorSettings::Current;

					int count = 1;
					float targetFps = 55.0f;
					int skip = settings->PowerFrameSkip;
					if(settings->CameraButtonAssignment == 0)
					{
						turbo = enableTurboMode;
					}
					if(!turbo)
					{
						if(settings->LowFrequencyMode)
						{
							skip = skip * 2 + 1;
							targetFps = 28.0f;
						}
						if(settings->FrameSkip == -1 && settings->PowerFrameSkip == 0)
						{
							if(!lastSkipped && (lastElapsedTime > (1.0f / targetFps)))
							{
								skip = (int)((lastElapsedTime / (1.0f / targetFps)));
								lastSkipped = true;
							}else
							{
								lastSkipped = false;
							}
						}else if(settings->FrameSkip >= 0)
						{
							skip += settings->FrameSkip;
						}
					}else
					{
						skip = settings->TurboFrameSkip;
					}
					count += skip;

					Settings.SkipFrames = count - 1;
					for (int i = 0; i < count; i++)
					{
						if(EmulatorSettings::Current->SynchronizeAudio)
						{
							// Update Emulator
							__int64 time1 = 0, time2 = 0;
							QueryPerformanceCounter((LARGE_INTEGER *) &time1);
							while(!S9xSyncSound())
							{
								//wait(2);
								WaitForSingleObjectEx(this->sleepEvent, 2, false);
								QueryPerformanceCounter((LARGE_INTEGER *) &time2);
								if(time2 - time1 > 1000)
								{
									S9xClearSamples();
									break;
								}
							}	
						}
			
						S9xMainLoop();
					}
					if(Settings.PAL)
					{
						updateCount++;
					}
					this->frameSkipped = false;
				}
			}
			LeaveCriticalSection(&this->pauseSync);

			SetEvent(this->swapEvent);
			WaitForSingleObjectEx(this->updateEvent, INFINITE, false);
		}
		
		// Signal for terminated thread
		SetEvent(this->endEvent);
	}

	void EmulatorGame::Update(void *buffer, size_t rowPitch, float lastElapsed)
	{
		if(!Settings.StopEmulation && this->IsROMLoaded())
		{
			//Settings.SkipFrames = EmulatorSettings::Current->LowFrequencyMode ? 1 : 0;
			Settings.Mute = !EmulatorSettings::Current->SoundEnabled;

			WaitForSingleObjectEx(this->swapEvent, INFINITE, false);

			lastElapsedTime = lastElapsed;

			// Swap buffers
			GFX.Screen = (uint16 *) buffer;
			GFX.Pitch = rowPitch;

			SetEvent(this->updateEvent);
		}
	}
}

void S9xSoundCallback(void *data)
{	
	Emulator::EmulatorGame::Audio->ProcessSound();
}

bool8 S9xOpenSoundDevice (void)
{
	if(!Emulator::EmulatorGame::Audio->InitSoundOutput())
		return false;
	
	if(!Emulator::EmulatorGame::Audio->SetupSound())
		return false;

	S9xSetSamplesAvailableCallback(S9xSoundCallback, NULL);
	return true;
}

bool8 S9xDeinitUpdate (int Width, int Height)
{
	Emulator::EmulatorGame::SnesImageWidth = Width;
	Emulator::EmulatorGame::SnesImageHeight = Height;
	return true;
}