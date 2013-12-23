#include <io.h>
#include "apu.h"
#include "display.h"
#include "memmap.h"
#include "Logger.h"
#include "controls.h"
#include "conffile.h"
//#include "common.h"

#define _tToChar
#define _tFromChar

const char *S9xGetFilenameInc (const char *e, enum s9x_getdirtype dirtype)
{
    static char filename [PATH_MAX + 1];
    char dir [_MAX_DIR + 1];
    char drive [_MAX_DRIVE + 1];
    char fname [_MAX_FNAME + 1];
    char ext [_MAX_EXT + 1];
    unsigned int i=0;
    const char *d;

    _splitpath_s (Memory.ROMFilename, drive, dir, fname, ext);
    d=S9xGetDirectory(dirtype);
    do {
        _snprintf_s(filename, sizeof(filename), "%s\\%s%03d%s", d, fname, i, e);
        i++;
    } while(_access (filename, 0) == 0 && i!=0);

    return (filename);
}

const char *S9xGetFilename (const char *ex, enum s9x_getdirtype dirtype)
{
    static char filename [PATH_MAX + 1];
    char dir [_MAX_DIR + 1];
    char drive [_MAX_DRIVE + 1];
    char fname [_MAX_FNAME + 1];
    char ext [_MAX_EXT + 1];
   _splitpath_s (Memory.ROMFilename, drive, dir, fname, ext);
   _snprintf_s(filename, sizeof(filename), "%s" SLASH_STR "%s%s",
             S9xGetDirectory(dirtype), fname, ex);
    return (filename);
}

const TCHAR *S9xGetDirectoryT (enum s9x_getdirtype dirtype)
{
	return nullptr;
}

const char *S9xGetDirectory (enum s9x_getdirtype dirtype)
{
	return nullptr;
}

void S9xExit( void)
{
}

void S9xMessage (int type, int, const char *str)
{
#ifdef DEBUGGER
    static FILE *out = NULL;

    if (out == NULL)
        out = fopen ("out.txt", "w");

    fprintf (out, "%s\n", str);
#endif

    S9xSetInfoString (str);

	// if we can't draw on the screen, messagebox it
	// also send to stderr/stdout depending on message type
	/*string errstr(str);
	wstring werrstr(errstr.begin(), errstr.end());*/
	switch(type)
	{
		case S9X_INFO:
			if(Settings.StopEmulation)
				fprintf(stdout, "%s\n", str);
			break;
		case S9X_WARNING:
			fprintf(stdout, "%s\n", str);
			/*if(Settings.StopEmulation)
				EngineLog(LOG_LEVEL::Warning, werrstr);*/
			break;
		case S9X_ERROR:
			fprintf(stderr, "%s\n", str);
			/*if(Settings.StopEmulation)
				EngineLog(LOG_LEVEL::Error, werrstr);*/
			break;
		case S9X_FATAL_ERROR:
			fprintf(stderr, "%s\n", str);
			/*if(Settings.StopEmulation)
				EngineLog(LOG_LEVEL::Error, werrstr);*/
			break;
		default:
				fprintf(stdout, "%s\n", str);
			break;
	}
}

bool S9xPollButton(uint32 id, bool *pressed)
{
	return false;
}

bool S9xPollAxis(uint32 id, int16 *value)
{
    return false;
}

bool S9xPollPointer(uint32 id, int16 *x, int16 *y)
{
	return false;
}

void S9xHandlePortCommand(s9xcommand_t cmd, int16 data1, int16 data2)
{
	return;
}

//  NYI
const char *S9xChooseFilename (bool8 read_only)
{
	return NULL;
}

// NYI
const char *S9xChooseMovieFilename (bool8 read_only)
{
	return NULL;
}


const char * S9xStringInput(const char *msg)
{
	return NULL;
}

void S9xToggleSoundChannel (int c)
{
	return;
}

void S9xSyncSpeed( void)
{
    if (!Settings.TurboMode && Settings.SkipFrames == AUTO_FRAMERATE)
    {
		if (IPPU.SkippedFrames < Settings.AutoMaxSkipFrames)
		{
			IPPU.SkippedFrames++;
			IPPU.RenderThisFrame = FALSE;
		}
		else
		{
			IPPU.RenderThisFrame = TRUE;
			IPPU.SkippedFrames = 0;
		}
	}
    else
    {
		uint32 SkipFrames;
		if(Settings.TurboMode)
			SkipFrames = Settings.TurboSkipFrames;
		else
			SkipFrames = (Settings.SkipFrames == AUTO_FRAMERATE) ? 0 : Settings.SkipFrames;
		if (IPPU.FrameSkip++ >= SkipFrames)
		{
			IPPU.FrameSkip = 0;
			IPPU.SkippedFrames = 0;
			IPPU.RenderThisFrame = TRUE;
		}
		else
		{
			IPPU.SkippedFrames++;
			IPPU.RenderThisFrame = true;
		}
    }
}

void WinDisplayStringFromBottom (const char *string, int linesFromBottom, int pixelsFromLeft, bool allowWrap)
{
	
}

bool8 S9xInitUpdate (void)
{
	return (TRUE);
}

bool8 S9xContinueUpdate(int Width, int Height)
{
	return true;
}

// we no longer support 8bit modes - no palette necessary
void S9xSetPalette( void)
{
	return;	
}

void S9xAutoSaveSRAM ()
{
    Memory.SaveSRAM (S9xGetFilename (".srm", SRAM_DIR));
}

void SetInfoDlgColor(unsigned char r, unsigned char g, unsigned char b)
{

}

void S9xOnSNESPadRead()
{

}

bool8 S9xOpenSnapshotFile( const char *fname, bool8 read_only, STREAM *file)
{
    char filename [_MAX_PATH + 1];
    char drive [_MAX_DRIVE + 1];
    char dir [_MAX_DIR + 1];
    char fn [_MAX_FNAME + 1];
    char ext [_MAX_EXT + 1];

    _splitpath_s( fname, drive, dir, fn, ext);
    _makepath_s( filename, drive, dir, fn, ext[0] == '\0' ? ".000" : ext);

    if (read_only)
    {
	OPEN_STREAM (file, filename, "rb");
	if (*file)
	    return (TRUE);
    }
    else
    {
	OPEN_STREAM (file, filename, "wb");
	if (*file)
	    return (TRUE);
        FILE *fs;
		fopen_s (&fs, filename, "rb");
        if (fs)
        {
            sprintf_s (String, "Freeze file \"%s\" exists but is read only",
                     filename);
            fclose (fs);
            S9xMessage (S9X_ERROR, S9X_FREEZE_FILE_NOT_FOUND, String);
        }
        else
        {
            sprintf_s (String, "Cannot create freeze file \"%s\". Directory is read-only or does not exist.", filename);

            S9xMessage (S9X_ERROR, S9X_FREEZE_FILE_NOT_FOUND, String);
        }
    }
    return (FALSE);
}

void S9xCloseSnapshotFile( STREAM file)
{
    CLOSE_STREAM (file);
}

const char *S9xBasename (const char *f)
{
    const char *p;
    if ((p = strrchr (f, '/')) != NULL || (p = strrchr (f, '\\')) != NULL)
	return (p + 1);

#ifdef __DJGPP
    if (p = _tcsrchr (f, SLASH_CHAR))
	return (p + 1);
#endif

    return (f);
}

void S9xExtraUsage ()
{
}

void S9xParseArg (char **argv, int &i, int argc)
{
}

void S9xParsePortConfig(ConfigFile &conf, int pass)
{
	return;
}