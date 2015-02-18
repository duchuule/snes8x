#pragma once

#include <D3D11.h>
#include "defines.h"
#include <collection.h>
#include <math.h>

using namespace Platform;
using namespace Windows::UI::Input;
using namespace Windows::Phone::Devices::Notification;


namespace Emulator
{

	struct ControllerState
	{
		bool JoystickPressed;
		bool LeftPressed;
		bool UpPressed;
		bool RightPressed;
		bool DownPressed;
		bool StartPressed;
		bool SelectPressed;
		bool APressed;
		bool BPressed;
		bool YPressed;
		bool XPressed;
		bool LPressed;
		bool RPressed;
		bool TurboPressed;
		bool ComboPressed;
	};


	class VirtualController
	{
	public:
		static VirtualController *GetInstance(void);

		VirtualController(void);
		~VirtualController(void);
		void UpdateFormat(int format);
		void VirtualControllerOnTop(bool onTop);	
		void SetOrientation(int orientation);
		int GetOrientation();
		int GetFormat(void);

		void PointerPressed(PointerPoint ^point);
		void PointerMoved(PointerPoint ^point);
		void PointerReleased(PointerPoint ^point);

		virtual const ControllerState *GetControllerState(void);

		void GetStickRectangle(RECT *rect);
		void GetStickCenterRectangle(RECT *rect);
		bool StickFingerDown(void);

		void GetCrossRectangle(RECT *rect);
		void GetARectangle(RECT *rect);
		void GetBRectangle(RECT *rect);
		void GetXRectangle(RECT *rect);
		void GetYRectangle(RECT *rect);
		void GetStartRectangle(RECT *rect);
		void GetSelectRectangle(RECT *rect);
		void GetTurboRectangle(RECT *rect);
		void GetComboRectangle(RECT *rect);
		void GetLRectangle(RECT *rect);
		void GetRRectangle(RECT *rect);

		int pressCount;
		int errCount;
		Platform::Collections::Map<unsigned int, Platform::String^> ^pointerDescriptions;

	protected:
		void SetControllerPositionFromSettings(void);
		void CreateRenderRectangles(void);

		void CreateTouchLandscapeRectangles(void);
		void CreateTouchPortraitRectangles(void);
		double CalculateDistanceDiff(Windows::Foundation::Point point1, Windows::Foundation::Point point2, Windows::Foundation::Point target);

		

		Platform::Collections::Map<unsigned int, Windows::UI::Input::PointerPoint ^> ^pointers;
		CRITICAL_SECTION cs;
		ControllerState state;
		bool virtualControllerOnTop;
		int orientation;
		int format;
		int width, height;
		int touchWidth, touchHeight; //scaled, orientation-aware
		float hscale;

		bool stickFingerDown;
		int stickFingerID;
		Windows::Foundation::Point stickPos;
		Windows::Foundation::Point stickOffset;
		POINT visibleStickPos;
		POINT visibleStickOffset;
		Windows::Foundation::Rect stickBoundaries;

		RECT padCrossRectangle;
		RECT startRectangle;
		RECT selectRectangle;
		RECT turboRectangle;
		RECT comboRectangle;
		RECT aRectangle;
		RECT bRectangle;
		RECT xRectangle;
		RECT yRectangle;
		RECT lRectangle;
		RECT rRectangle;
		Windows::Foundation::Rect leftRect;
		Windows::Foundation::Rect upRect;
		Windows::Foundation::Rect rightRect;
		Windows::Foundation::Rect downRect;
		Windows::Foundation::Rect startRect;
		Windows::Foundation::Rect selectRect;
		Windows::Foundation::Rect turboRect;
		Windows::Foundation::Rect comboRect;
		Windows::Foundation::Rect lRect;
		Windows::Foundation::Rect rRect;
		Windows::Foundation::Rect aRect;
		Windows::Foundation::Rect bRect;
		Windows::Foundation::Rect xRect;
		Windows::Foundation::Rect yRect;

		int padCenterX;
		int padCenterY;
		int aCenterX;
		int aCenterY;
		int bCenterX;
		int bCenterY;
		int xCenterX;
		int xCenterY;
		int yCenterX;
		int yCenterY;
		int startLeft;
		int startTop;
		int selectRight;
		int selectTop;
		int turboLeft;
		int turboTop;
		int comboLeft;
		int comboTop;
		int lLeft;
		int lTop;
		int rRight;
		int rTop;
	private:
		static VirtualController *singleton;
		VibrationDevice ^vibrationDevice;
		bool CheckTouchableArea(Windows::Foundation::Point p);
		
	};
}