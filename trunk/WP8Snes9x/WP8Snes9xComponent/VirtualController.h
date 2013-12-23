#pragma once

#include <D3D11.h>
#include "defines.h"
#include <collection.h>

using namespace Platform;
using namespace Windows::UI::Input;

namespace Emulator
{

	struct ControllerState
	{
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
	};

	class VirtualController
	{
	public:
		VirtualController(void);
		~VirtualController(void);
		void UpdateFormat(int format);
		void VirtualControllerOnTop(bool onTop);	
		void SetOrientation(int orientation);

		void PointerPressed(PointerPoint ^point);
		void PointerMoved(PointerPoint ^point);
		void PointerReleased(PointerPoint ^point);

		const ControllerState *GetControllerState(void);

		void GetStickRectangle(RECT *rect);
		void GetStickCenterRectangle(RECT *rect);
		bool StickFingerDown(void);

		void GetCrossRectangle(RECT *rect);
		void GetButtonsRectangle(RECT *rect);
		void GetStartSelectRectangle(RECT *rect);
		void GetLRectangle(RECT *rect);
		void GetRRectangle(RECT *rect);

		int pressCount;
		int errCount;

	private:
		Platform::Collections::Map<unsigned int, Windows::UI::Input::PointerPoint ^> ^pointers;
		CRITICAL_SECTION cs;
		ControllerState state;
		bool virtualControllerOnTop;
		int orientation;
		int format;
		int width, height;
		int touchWidth, touchHeight;

		bool stickFingerDown;
		int stickFingerID;
		Windows::Foundation::Point stickPos;
		Windows::Foundation::Point stickOffset;
		POINT visibleStickPos;
		POINT visibleStickOffset;
		Windows::Foundation::Rect stickBoundaries;

		RECT padCrossRectangle;
		RECT startSelectRectangle;
		RECT buttonsRectangle;
		RECT lRectangle;
		RECT rRectangle;
		Windows::Foundation::Rect leftRect;
		Windows::Foundation::Rect upRect;
		Windows::Foundation::Rect rightRect;
		Windows::Foundation::Rect downRect;
		Windows::Foundation::Rect startRect;
		Windows::Foundation::Rect selectRect;
		Windows::Foundation::Rect lRect;
		Windows::Foundation::Rect rRect;
		Windows::Foundation::Rect aRect;
		Windows::Foundation::Rect bRect;
		Windows::Foundation::Rect xRect;
		Windows::Foundation::Rect yRect;
		
		void CreateWXGARectangles(void);
		void CreateWVGARectangles(void);
		void Create720PRectangles(void);
		void CreateWXGAPortraitRectangles(void);
		void CreateWVGAPortraitRectangles(void);
		void Create720PPortraitRectangles(void);
		void CreateTouchLandscapeRectangles(void);
		void CreateTouchPortraitRectangles(void);
		double CalculateDistanceDiff(Windows::Foundation::Point point1, Windows::Foundation::Point point2, Windows::Foundation::Point target);
		
	};
}