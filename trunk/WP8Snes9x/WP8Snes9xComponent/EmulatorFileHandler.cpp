#include "pch.h"
#include <string>
#include <sstream>
#include <memory>
#include "EmulatorFileHandler.h"
#include "snes9x.h"
#include "memmap.h"
#include "cpuexec.h"
#include "display.h"
#include "apu/apu.h"
#include "controls.h"
#include "snapshot.h"
#include <robuffer.h>

using namespace Platform;
using namespace Windows::Storage::Streams;
using namespace Windows::Storage::FileProperties;
using namespace std;

#define SAVE_FOLDER	"saves"

namespace Emulator
{
	StorageFile ^ROMFile = nullptr;
	StorageFolder ^ROMFolder = nullptr;
	int SavestateSlot = 0;
	int LoadstateSlot = 0;
	
	Platform::Array<unsigned short> ^GetSnapshotBuffer(uint16 *backbuffer, size_t pitch, int imageWidth, int imageHeight)
	{
		Platform::Array<unsigned short> ^buffer = ref new Platform::Array<unsigned short>(imageWidth * imageHeight);		
		
		/*Microsoft::WRL::ComPtr<IBufferByteAccess> byteAccess;
		reinterpret_cast<IUnknown*>(buffer)->QueryInterface(IID_PPV_ARGS(&byteAccess));
		byte *buf;
		byteAccess->Buffer(&buf);
		uint16 *targetBuffer = (uint16 *) buf;*/
		for (int i = 0; i < imageHeight; i++)
		{
			for (int j = 0; j < imageWidth; j++)
			{
				buffer[imageWidth * i + j] = *(backbuffer + (pitch / 2) * i + j) ;
				//*(targetBuffer + imageWidth * i + j) = *(backbuffer + (pitch / 2) * i + j) ;
			}
		}

		return buffer;
	}

	task<void> SaveStateAsync(void)
	{
		EmulatorGame *emulator = EmulatorGame::GetInstance();
		return create_task([]()
		{
			if(!ROMFile || !ROMFolder)
			{
				throw ref new Exception(E_FAIL, "No ROM loaded.");
			}
			return ROMFolder->GetFolderAsync(SAVE_FOLDER);
		}).then([emulator](StorageFolder ^folder)
		{
			emulator->Pause();

			// Generate random file name to store in temp folder			
			Platform::String ^folderpath = folder->Path;
			string folderPathStr(folderpath->Begin(), folderpath->End());

			StorageFile ^romFile = ROMFile;
			wstring wRomName(ROMFile->Name->Begin(), ROMFile->Name->Length() - 4);
			string romName(wRomName.begin(), wRomName.end());

			stringstream tmpFileNameStream;
			tmpFileNameStream << folderPathStr << "\\";
			tmpFileNameStream << romName << ".0";
			if(SavestateSlot >= 10)
			{
				tmpFileNameStream << SavestateSlot;
			}else
			{
				tmpFileNameStream << 0 << SavestateSlot;
			}
			string fileNameA = tmpFileNameStream.str();

			FILE *file;
			auto error = fopen_s(&file, fileNameA.c_str(), "wb");
			if(!file)
			{
#if _DEBUG
				stringstream ss;
				ss << "Unable to open file '";
				ss << fileNameA;
				ss << "' to store savestate (";
				ss << error;
				ss << ").";
				OutputDebugStringA(ss.str().c_str());
#endif
			}
			fclose(file);

			if(!S9xFreezeGame(fileNameA.c_str()))
			{
#if _DEBUG
				stringstream ss;
				ss << "Unable to write savestate to file '";
				ss << fileNameA;
				ss << ".";
				OutputDebugStringA(ss.str().c_str());
#endif
			}
		}).then([emulator](task<void> t)
		{
			try
			{
				//emulator->Unpause();	
				t.get();
			}catch(Exception ^ex)
			{
#if _DEBUG
				wstring str(ex->Message->Begin(), ex->Message->End());
				OutputDebugStringW((L"Save state: " + str).c_str());
#endif
			}
		});
	}

	task<void> LoadStateAsync(int slot)
	{
		int whichslot;
		if (slot <0)
			whichslot = LoadstateSlot;
		else
			whichslot = slot;

		EmulatorGame *emulator = EmulatorGame::GetInstance();
		return create_task([]()
		{
			if(!ROMFile || !ROMFolder)
			{
				throw ref new Exception(E_FAIL, "No ROM loaded.");
			}
			return ROMFolder->GetFolderAsync(SAVE_FOLDER);
		}).then([emulator, whichslot](StorageFolder ^folder)
		{
			emulator->Pause();
			wstringstream extension;
			extension << L".0";
			if(whichslot >= 10)
			{
				extension << whichslot;
			}else
			{
				extension << 0 << whichslot;
			}
			Platform::String ^nameWithoutExtension = ref new Platform::String(ROMFile->Name->Data(), ROMFile->Name->Length() - 4);			
			Platform::String ^statePath = folder->Path + "\\" + nameWithoutExtension + ref new Platform::String(extension.str().c_str());
			return StorageFile::GetFileFromPathAsync(statePath);
		}).then([](StorageFile ^file)
		{
			Platform::String ^path = file->Path;
			wstring str = path->Data();
			string strA(str.begin(), str.end());
			S9xUnfreezeGame(strA.c_str());
		}).then([](){}).then([emulator](task<void> t)
		{
			try
			{
				t.get();
			}catch(Platform::Exception ^ex)
			{
#if _DEBUG
				wstring err = ex->Message->Data();
				OutputDebugStringW((L"Load state: " + err).c_str());
#endif
			}
		});
	}

	task<void> SaveSRAMAsync(void)
	{
		if(ROMFile == nullptr || ROMFolder == nullptr)
			return task<void>();

		Platform::String ^name = ROMFile->Name;
		Platform::String ^nameWithoutExt = ref new Platform::String(name->Begin(), name->Length() - 4);
		Platform::String ^sramName = nameWithoutExt->Concat(nameWithoutExt, ".srm");


		return create_task([]()
		{
			return ROMFolder->CreateFolderAsync(SAVE_FOLDER, CreationCollisionOption::OpenIfExists);
		}).then([sramName](StorageFolder ^saveFolder)
		{
			// try to open the file			
			return saveFolder->CreateFileAsync(sramName, CreationCollisionOption::OpenIfExists);
		})
		.then([](StorageFile ^file)
		{
			if (Settings.SuperFX && Memory.ROMType < 0x15) // doesn't have SRAM
				return;

			if (Settings.SA1 && Memory.ROMType == 0x34)    // doesn't have SRAM
				return;

			int		size;
			if (Multi.cartType && Multi.sramSizeB)
			{
				size = (1 << (Multi.sramSizeB + 3)) * 128;
				SaveBytesToFileAsync(file,Multi.sramB, size).wait();
			}

			size = Memory.SRAMSize ? (1 << (Memory.SRAMSize + 3)) * 128 : 0;
			if (size > 0x20000)
				size = 0x20000;

			if (size)
			{
				SaveBytesToFileAsync(file, Memory.SRAM, size).wait();

				if (Settings.SRTC || Settings.SPC7110RTC)
					Memory.SaveSRTC();
			}
		}).then([sramName](task<void> t)
		{
			try
			{
				t.get();
			}catch(Platform::COMException ^ex)
			{
#if _DEBUG
				Platform::String ^error = ex->Message;
				wstring wname(sramName->Begin(), sramName->End());
				wstring werror(error->Begin(), error->End());
				OutputDebugStringW((wname + L": " + werror).c_str());
#endif
			}
		});
	}

	task<void> LoadSRAMAsync ()
	{
		if(!ROMFile || !ROMFolder)
			return task<void>([](){});

		Platform::String ^name = ROMFile->Name;
		Platform::String ^nameWithoutExt = ref new Platform::String(name->Begin(), name->Length() - 4);
		Platform::String ^sramName = nameWithoutExt->Concat(nameWithoutExt, ".srm");

		return create_task([]()
		{
			return ROMFolder->GetFolderAsync(SAVE_FOLDER);
		}).then([sramName](StorageFolder ^folder)
		{
			// try to open the file
			return folder->GetFileAsync(sramName);
		})
		.then([](StorageFile ^file)
		{
			// get bytes out of storage file
			return GetBytesFromFileAsync(file);
		}).then([](ROMData data)
		{
			// load the sram finally
			int		size, len;

			Memory.ClearSRAM();

			if (Multi.cartType && Multi.sramSizeB)
			{
				size = (1 << (Multi.sramSizeB + 3)) * 128;
				len = data.Length;
				for (int i = 0; i < len && i < 0x10000; i++)
				{
					Multi.sramB[i] = data.ROM[i];
					if (len - size == 512)
						memmove(Multi.sramB, Multi.sramB + 512, size);
				}
			}

			size = Memory.SRAMSize ? (1 << (Memory.SRAMSize + 3)) * 128 : 0;
			if (size > 0x20000)
				size = 0x20000;

			if (size)
			{
				len = data.Length;
				for (int i = 0; i < len && i < 0x20000; i++)
				{
					Memory.SRAM[i] = data.ROM[i];
				}
				if (len - size == 512)
					memmove(Memory.SRAM, Memory.SRAM + 512, size);

				if (Settings.SRTC || Settings.SPC7110RTC)
					Memory.LoadSRTC();
			}

			delete [] data.ROM;

		}).then([sramName](task<void> t)
		{
			// handle exceptions
			try
			{
				t.get();
			}
			catch(Platform::COMException ^ex)
			{
#if _DEBUG
				Platform::String ^error = ex->Message;
				wstring wname(sramName->Begin(), sramName->End());
				wstring werror(error->Begin(), error->End());
				OutputDebugStringW((wname + L": " + werror).c_str());
#endif
			}catch(Platform::AccessDeniedException ^ex)
			{
#if _DEBUG
				Platform::String ^error = ex->Message;
				wstring wname(sramName->Begin(), sramName->End());
				wstring werror(error->Begin(), error->End());
				OutputDebugStringW((wname + L": " + werror).c_str());
#endif
			}catch(Platform::Exception ^ex)
			{
#if _DEBUG
				Platform::String ^error = ex->Message;
				wstring wname(sramName->Begin(), sramName->End());
				wstring werror(error->Begin(), error->End());
				OutputDebugStringW((wname + L": " + werror).c_str());
#endif
			}
		});	
	}

	task<void> SaveBytesToFileAsync(StorageFile ^file, unsigned char *bytes, size_t length)
	{
		Platform::String ^name = file->Name;

		return create_task(file->OpenAsync(FileAccessMode::ReadWrite))
			.then([=](IRandomAccessStream ^stream)
		{
			IOutputStream ^outputStream = stream->GetOutputStreamAt(0L);;
			DataWriter ^writer = ref new DataWriter(outputStream);
			
			Platform::Array<unsigned char> ^array = ref new Array<unsigned char>(length);
			memcpy(array->Data, bytes, length);

			writer->WriteBytes(array);
			create_task(writer->StoreAsync()).wait();
			writer->DetachStream();
			return create_task(outputStream->FlushAsync());
		}).then([name](bool b)
		{ 
			if(!b)
			{
#if _DEBUG
				OutputDebugStringW(L"Error while writing files to file.");
#endif
			}
		}).then([name](task<void> t)
		{
			try
			{
				t.get();
			}catch(COMException ^ex)
			{
#if _DEBUG
				Platform::String ^error = ex->Message;
				wstring wname(name->Begin(), name->End());
				wstring werror(error->Begin(), error->End());
				OutputDebugStringW((wname + L": " + werror).c_str());
#endif
			}
		});
	}

	task<ROMData> GetBytesFromFileAsync(StorageFile ^file)
	{
		auto inputStream = make_shared<IInputStream ^>();
		auto openTask = create_task(file->OpenSequentialReadAsync());
		
		return openTask.then([=] (IInputStream ^stream)
		{ 
			*inputStream = stream;
			return file->GetBasicPropertiesAsync();
		}).then([=](BasicProperties ^properties)
		{
			Buffer ^buffer = ref new Buffer(properties->Size);
			return (*inputStream)->ReadAsync(buffer, properties->Size, InputStreamOptions::None);
		})
		.then([=](IBuffer ^buffer)
		{			
			DataReader ^reader = DataReader::FromBuffer(buffer);
			Array<BYTE> ^bytes = ref new Array<BYTE>(buffer->Length);
			reader->ReadBytes(bytes);
			BYTE *rawBytes = new BYTE[buffer->Length];
			for (int i = 0; i < buffer->Length; i++)
			{
				rawBytes[i] = bytes[i]; 
			}
			                                                                                                                                                                                                             
			ROMData data;
			data.Length = buffer->Length;
			data.ROM = rawBytes;

			return data;
		});
	}
	
	task<void> ResetAsync(void)
	{
		if(!ROMFile || !ROMFolder)
			return task<void>();

		return LoadROMAsync(ROMFile, ROMFolder);
	}
	
	task<void> LoadROMAsync(StorageFile ^file, StorageFolder ^folder)
	{
		EmulatorGame *emulator = EmulatorGame::GetInstance();
		return create_task([emulator]()
		{
			emulator->Pause();
			emulator->StopEmulationAsync().wait();
		}).then([file]()
		{
			return GetROMBytesFromFileAsync(file);
		}).then([emulator, file, folder](ROMData data)
		{
			if(data.ROM && data.Length >= 0)
			{
				ROMFile = file;
				ROMFolder = folder;
				if(!Memory.LoadROM(data.ROM, data.Length))
				{
					ROMFile = nullptr;
					ROMFolder = nullptr;
				}
			}else
			{
				// error
				throw ref new Platform::Exception(E_FAIL, "Failed to load ROM file.");
			}
			if(data.ROM)
			{
				delete [] data.ROM;
				data.ROM = nullptr;
			}
		}).then([]()
		{
			return LoadSRAMAsync();
		}).then([emulator]()
		{
			emulator->Unpause();
			//SetEvent(this->updateEvent);
		})
		.then([](task<void> t)
		{
			try
			{
				t.get();
			}catch(Platform::Exception ^ex)
			{
				if(ex->HResult != E_FAIL)
				{
					throw ex;
				}
			}
		});
	}

	task<ROMData> GetROMBytesFromFileAsync(StorageFile ^file)
	{
		auto inputStream = make_shared<IInputStream ^>();
		auto openTask = create_task(file->OpenSequentialReadAsync());
		
		return openTask.then([=] (IInputStream ^stream)
		{ 
			*inputStream = stream;
			return file->GetBasicPropertiesAsync();
		}).then([=](BasicProperties ^properties)
		{
			Buffer ^buffer = ref new Buffer(properties->Size);
			return (*inputStream)->ReadAsync(buffer, properties->Size, InputStreamOptions::None);
		})
		.then([=](IBuffer ^buffer)
		{			
			DataReader ^reader = DataReader::FromBuffer(buffer);
			Array<BYTE> ^bytes = ref new Array<BYTE>(buffer->Length);
			reader->ReadBytes(bytes);
			BYTE *rawBytes = new BYTE[buffer->Length];
			for (int i = 0; i < buffer->Length; i++)
			{
				rawBytes[i] = bytes[i]; 
			}
			                                                                                                                                                                                                             
			ROMData data;
			data.Length = buffer->Length;
			data.ROM = rawBytes;

			return data;
		});
	}
}