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

		property int BgcolorR
		{
			int get(void) { return this->bgcolorR; }
			void set(int value) 
			{ 
				this->bgcolorR = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int BgcolorG
		{
			int get(void) { return this->bgcolorG; }
			void set(int value) 
			{ 
				this->bgcolorG = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}


		property int BgcolorB
		{
			int get(void) { return this->bgcolorB; }
			void set(int value) 
			{ 
				this->bgcolorB = value; 
				if(this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool AutoSaveLoad
		{
			bool get(void) { return this->autoSaveLoad; }
			void set(bool value)
			{
				this->autoSaveLoad = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}


		property int VirtualControllerStyle
		{
			int get(void) { return this->virtualControllerStyle; }
			void set(int value)
			{
				this->virtualControllerStyle = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool VibrationEnabled
		{
			bool get(void) { return this->vibrationEnabled; }
			void set(bool value)
			{
				this->vibrationEnabled = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property double VibrationDuration
		{
			double get(void) { return this->vibrationDuration; }
			void set(double value)
			{
				this->vibrationDuration = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool EnableAutoFire
		{
			bool get(void) { return this->enableAutoFire; }
			void set(bool value)
			{
				this->enableAutoFire = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool MapABLRTurbo
		{
			bool get(void) { return this->mapABLRTurbo; }
			void set(bool value)
			{
				this->mapABLRTurbo = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool FullPressStickABLR
		{
			bool get(void) { return this->fullPressStickABLR; }
			void set(bool value)
			{
				this->fullPressStickABLR = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property int UseMotionControl
		{
			int get(void) { return this->useMotionControl; }
			void set(int value)
			{
				this->useMotionControl = value;
				if (this->SettingsChanged)
				{
					this->SettingsChanged();
				}
			}
		}

		property bool UseTurbo
		{
			bool get(void) { return this->useTurbo; }
			void set(bool value)
			{
				this->useTurbo = value;

				//don't need this, we do it in the C# project already
				//if (this->SettingsChanged)
				//{
				//	this->SettingsChanged();
				//}
			}
		}

		EmulatorSettings(void);

		property int PadCenterXP;
		property int PadCenterYP;
		property int ACenterXP;
		property int ACenterYP;
		property int BCenterXP;
		property int BCenterYP;
		property int XCenterXP;
		property int XCenterYP;
		property int YCenterXP;
		property int YCenterYP;
		property int StartLeftP;
		property int StartTopP;
		property int SelectRightP;
		property int SelectTopP;
		property int LLeftP;
		property int LTopP;
		property int RRightP;
		property int RTopP;

		property int PadCenterXL;
		property int PadCenterYL;
		property int ACenterXL;
		property int ACenterYL;
		property int BCenterXL;
		property int BCenterYL;
		property int XCenterXL;
		property int XCenterYL;
		property int YCenterXL;
		property int YCenterYL;
		property int StartLeftL;
		property int StartTopL;
		property int SelectRightL;
		property int SelectTopL;
		property int LLeftL;
		property int LTopL;
		property int RRightL;
		property int RTopL;


		property int MogaA;
		property int MogaB;
		property int MogaX;
		property int MogaY;
		property int MogaL1;
		property int MogaR1;
		property int MogaL2;
		property int MogaR2;
		property int MogaLeftJoystick;
		property int MogaRightJoystick;

		property int MotionLeft;
		property int MotionRight;
		property int MotionUp;
		property int MotionDown;
		property double RestAngleX;
		property double RestAngleY;
		property double RestAngleZ;

		property double MotionDeadzoneH;
		property double MotionDeadzoneV;
		property bool MotionAdaptOrientation;

		
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
		int bgcolorR;
		int bgcolorG;
		int bgcolorB;

		bool autoSaveLoad;
		int virtualControllerStyle;
		bool vibrationEnabled;
		double vibrationDuration; //in second
		bool enableAutoFire;
		bool mapABLRTurbo;
		bool fullPressStickABLR;
		int useMotionControl;
		bool useTurbo;

		static EmulatorSettings ^instance;


	};
}

