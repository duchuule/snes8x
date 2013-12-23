#pragma once

#include <ppltasks.h>
#include "SpriteBatch.h"
#include "CXAudio2.h"
#include "IS9xSoundOutput.h"

using namespace DirectX;
using namespace concurrency;
using namespace Windows::Storage;

bool8 S9xOpenSoundDevice (void);
void S9xSoundCallback(void *data);
bool8 S9xDeinitUpdate (int Width, int Height);

#define EXT_WIDTH (MAX_SNES_WIDTH + 4)
#define EXT_PITCH (EXT_WIDTH * 2)
#define EXT_HEIGHT (MAX_SNES_HEIGHT + 4)
#define EXT_OFFSET (EXT_PITCH * 2 + 2 * 2)

#define JOYPAD_A		1
#define JOYPAD_B		2
#define JOYPAD_X		3
#define JOYPAD_Y		4
#define JOYPAD_L		5
#define JOYPAD_R		6
#define JOYPAD_START	7
#define JOYPAD_SELECT	8
#define JOYPAD_LEFT		9
#define JOYPAD_RIGHT	10
#define JOYPAD_UP		11
#define JOYPAD_DOWN		12

namespace Emulator
{
	struct ROMData
	{
		unsigned char *ROM;
		size_t Length;
	};

	class EmulatorGame
	{
	public:
		static EmulatorGame *GetInstance(void);

		static IS9xSoundOutput *Audio;
		static int SnesImageWidth, SnesImageHeight;

		EmulatorGame(void);
		~EmulatorGame(void);

		void Update(void *buffer, size_t rowPitch, float lastElapsed);

		void SetButtonState(int button, bool pressed);

		bool LastFrameSkipped(void);
		bool IsROMLoaded(void);
		bool IsPaused(void);
		void Pause(void);
		void Unpause(void);
		task<void> StopEmulationAsync(void);

		void Resume(void);
		void Suspend(void);
	private:
		static EmulatorGame instance;
		static bool initialized;
		int updateCount;
		bool frameSkipped;

		Windows::Foundation::IAsyncAction ^threadAction;
		CRITICAL_SECTION cs;
		CRITICAL_SECTION pauseSync;
		HANDLE swapEvent;
		HANDLE updateEvent;
		HANDLE sleepEvent;
		bool stopThread;
		
		CXAudio2 audio;
		BYTE *gfxbuffer;

		void Initialize(void);
		void UpdateAsync(void);
		void InitEmulator(void);

		void InitSound(void);
		void DeInitSound(void);
		void InitThread(void);
		void DeInitThread(void);
	};
}