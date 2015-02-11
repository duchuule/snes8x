#pragma once

#include <ppltasks.h>
#include "Emulator.h"

using namespace concurrency;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

#define MAX_SAVESTATE_SLOTS 10
#define AUTOSAVE_SLOT		9

namespace Emulator
{
	extern StorageFile ^ROMFile;
	extern StorageFolder ^ROMFolder;
	extern int SavestateSlot;
	extern int LoadstateSlot;

	Platform::Array<unsigned short> ^GetSnapshotBuffer(uint16 *backbuffer, size_t pitch, int imageWidth, int imageHeight);
	task<void> SaveStateAsync(void);
	task<void> LoadStateAsync(int slot);
	task<void> LoadROMAsync(StorageFile ^file, StorageFolder ^folder);
	task<void> ResetAsync(void);
	task<ROMData> GetROMBytesFromFileAsync(StorageFile ^file);
	task<void> SaveSRAMAsync(void);
	task<void> LoadSRAMAsync(void);

	task<void> SaveBytesToFileAsync(StorageFile ^file, unsigned char *bytes, size_t length);
	task<ROMData> GetBytesFromFileAsync(StorageFile ^file);
}