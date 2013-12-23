#pragma once

#define ORIENTATION_LANDSCAPE			0
#define ORIENTATION_LANDSCAPE_RIGHT		1
#define ORIENTATION_PORTRAIT			2

namespace PhoneDirect3DXamlAppComponent
{
	public delegate void SettingsChangedDelegate(void);

	public ref class EmulatorSettings sealed
	{
	public:
		static property int OrientationBoth
		{
			int get() { return 0; } 
		}

		static property int OrientationLandscape
		{
			int get() { return 1; }
		}

		static property int OrientationPortrait
		{
			int get() { return 2; }
		}

		static property EmulatorSettings ^Current
		{
			EmulatorSettings ^get() 
			{
				if(!instance)
				{
					instance = ref new EmulatorSettings();
				}
				return instance;
			}
		}


		property SettingsChangedDelegate ^SettingsChanged;

		property bool Initialized;
		property bool SoundEnabled
		{
			bool get() { return this->soundEnabled; }
			void set(bool value) 
			{ 
				this->soundEnabled = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}


		property bool UseMogaController
		{
			bool get() { return this->useMogaController; }
			void set(bool value) 
			{ 
				this->useMogaController = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}


		property bool VirtualControllerOnTop
		{
			bool get() { return this->vcontrollerOnTop; }
			void set(bool value) 
			{ 
				this->vcontrollerOnTop = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool LargeVController
		{
			bool get() { return this->vcontrollerLarge; }
			void set(bool value) 
			{ 
				this->vcontrollerLarge = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool LowFrequencyMode
		{
			bool get() { return this->lowFreqMode; }
			void set(bool value) 
			{ 
				this->lowFreqMode = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		/*property bool LowFrequencyModeMeasured
		{
			bool get() { return this->lowFreqModeMeasured; }
			void set(bool value) 
			{ 
				this->lowFreqModeMeasured = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}*/

		property bool GrayVControllerButtons
		{
			bool get() { return this->grayButtons; }
			void set(bool value)
			{
				this->grayButtons = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool FullscreenStretch
		{
			bool get() { return this->fullscreenStretch; }
			void set(bool value)
			{
				this->fullscreenStretch = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int ControllerScale
		{
			int get() { return this->controllerScale; }
			void set(int value)
			{
				this->controllerScale = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int ButtonScale
		{
			int get() { return this->buttonScale; }
			void set(int value)
			{
				this->buttonScale = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int Orientation
		{
			int get() { return this->orientation; }
			void set(int value) 
			{
				this->orientation = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int ControllerOpacity
		{
			int get() { return this->controllerOpacity; }
			void set(int value) 
			{
				this->controllerOpacity = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int FrameSkip
		{
			int get() { return this->frameSkip; }
			void set(int value) 
			{
				this->frameSkip = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int TurboFrameSkip
		{
			int get() { return this->turboFrameSkip; }
			void set(int value) 
			{
				this->turboFrameSkip = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int PowerFrameSkip
		{
			int get() { return this->powerFrameSkip; }
			void set(int value) 
			{
				this->powerFrameSkip = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int DPadStyle
		{
			int get() { return this->dpadStyle; }
			void set(int value) 
			{
				this->dpadStyle = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool SynchronizeAudio
		{
			bool get() { return this->syncAudio; }
			void set(bool value)
			{
				this->syncAudio = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property float Deadzone
		{
			float get() { return this->deadzone; }
			void set(float value) 
			{
				this->deadzone = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int ImageScaling
		{
			int get() { return this->imageScale; }
			void set(int value) 
			{
				this->imageScale = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int CameraButtonAssignment
		{
			int get() { return this->cameraAssignment; }
			void set(int value) 
			{
				this->cameraAssignment = value;
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool HideConfirmationDialogs
		{
			bool get(void) { return this->hideConfirmations; }
			void set(bool value) 
			{ 
				this->hideConfirmations = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool HideLoadConfirmationDialogs
		{
			bool get(void) { return this->hideLoadConfirmations; }
			void set(bool value) 
			{ 
				this->hideLoadConfirmations = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool AutoIncrementSavestates
		{
			bool get(void) { return this->autoIncSavestates; }
			void set(bool value) 
			{ 
				this->autoIncSavestates = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool SelectLastState
		{
			bool get(void) { return this->selectLastState; }
			void set(bool value) 
			{ 
				this->selectLastState = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool ManualSnapshots
		{
			bool get(void) { return this->manualSnapshots; }
			void set(bool value) 
			{ 
				this->manualSnapshots = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool ShouldShowAds
		{
			bool get(void) { return this->shouldShowAds; }
			void set(bool value) 
			{ 
				this->shouldShowAds = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		EmulatorSettings(void);
	private:
		bool soundEnabled;
		bool useMogaController;
		bool vcontrollerOnTop;
		bool lowFreqMode;
		//bool lowFreqModeMeasured;
		bool vcontrollerLarge;
		bool grayButtons;
		bool fullscreenStretch;
		int controllerScale;
		int buttonScale;
		int controllerOpacity;
		int orientation;
		int frameSkip;
		int turboFrameSkip;
		int powerFrameSkip;
		int imageScale;
		bool syncAudio;
		int dpadStyle;
		float deadzone;
		int cameraAssignment;
		bool hideConfirmations;
		bool hideLoadConfirmations;
		bool autoIncSavestates;
		bool selectLastState;
		bool manualSnapshots;
		bool shouldShowAds;

		static EmulatorSettings ^instance;
	};
}

