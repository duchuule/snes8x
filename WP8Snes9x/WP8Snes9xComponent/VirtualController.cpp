#include "pch.h"
#include "VirtualController.h"
#include "EmulatorSettings.h"
#include "WP8Snes9xComponent.h"
#include <string>
#include <xstring>
#include <sstream>

using namespace std;

using namespace PhoneDirect3DXamlAppComponent;
	
#define VCONTROLLER_Y_OFFSET_WVGA			218
#define VCONTROLLER_Y_OFFSET_WXGA			348
#define VCONTROLLER_Y_OFFSET_720P			308
#define VCONTROLLER_BUTTON_Y_OFFSET_WVGA			303
#define VCONTROLLER_BUTTON_Y_OFFSET_WXGA			498
#define VCONTROLLER_BUTTON_Y_OFFSET_720P			428

namespace Emulator
{
	VirtualController *VirtualController::singleton = nullptr;

	VirtualController *VirtualController::GetInstance(void)
	{
		return singleton;
	}

	VirtualController::VirtualController(void)
		: virtualControllerOnTop(false), stickFingerDown(false)
	{
		InitializeCriticalSectionEx(&this->cs, 0, 0);
		this->pointers = ref new Platform::Collections::Map<unsigned int, PointerPoint^>();
		this->pointerDescriptions = ref new Platform::Collections::Map<unsigned int, Platform::String^>();
		vibrationDevice = VibrationDevice::GetDefault();

		singleton = this;
	}

	VirtualController::~VirtualController(void)
	{
		DeleteCriticalSection(&this->cs);
	}
	
	int VirtualController::GetFormat(void)
	{
		return this->format;
	}

	void VirtualController::UpdateFormat(int format)
	{
		this->format = format;
		switch(format)
		{
		case WXGA:
			this->width = 1280;
			this->height = 768;
			this->touchWidth = 800;
			this->touchHeight = 480;

			break;
		case HD720P:
			this->width = 1280;
			this->height = 720;
			this->touchWidth = 853;
			this->touchHeight = 480;
			break;
		default:
			this->width = 800;
			this->height = 480;
			this->touchWidth = 800;
			this->touchHeight = 480;
			break;
		}
		this->hscale = ((float)this->height) / this->touchHeight;

		this->SetControllerPositionFromSettings();

		this->CreateRenderRectangles();

		if(this->orientation != ORIENTATION_PORTRAIT)
			this->CreateTouchLandscapeRectangles();
		else
			this->CreateTouchPortraitRectangles();		

	}

	void VirtualController::SetControllerPositionFromSettings(void)
	{
		EmulatorSettings ^settings = EmulatorSettings::Current;

		if(this->orientation != ORIENTATION_PORTRAIT)
		{
			padCenterX = settings->PadCenterXL;
			padCenterY = settings->PadCenterYL;
			aCenterX = settings->ACenterXL;
			aCenterY = settings->ACenterYL;
			bCenterX = settings->BCenterXL;
			bCenterY = settings->BCenterYL;
			xCenterX = settings->XCenterXL;
			xCenterY = settings->XCenterYL;
			yCenterX = settings->YCenterXL;
			yCenterY = settings->YCenterYL;
			startLeft = settings->StartLeftL;
			startTop = settings->StartTopL;
			selectRight = settings->SelectRightL;
			selectTop = settings->SelectTopL;
			lLeft = settings->LLeftL;
			lTop = settings->LTopL;
			rRight = settings->RRightL;
			rTop = settings->RTopL;
			turboLeft = settings->TurboLeftL;
			turboTop = settings->TurboTopL;
			comboLeft = settings->ComboLeftL;
			comboTop = settings->ComboTopL;
		}
		else
		{
			padCenterX = settings->PadCenterXP;
			padCenterY = settings->PadCenterYP;
			aCenterX = settings->ACenterXP;
			aCenterY = settings->ACenterYP;
			bCenterX = settings->BCenterXP;
			bCenterY = settings->BCenterYP;
			xCenterX = settings->XCenterXP;
			xCenterY = settings->XCenterYP;
			yCenterX = settings->YCenterXP;
			yCenterY = settings->YCenterYP;
			startLeft = settings->StartLeftP;
			startTop = settings->StartTopP;
			selectRight = settings->SelectRightP;
			selectTop = settings->SelectTopP;
			lLeft = settings->LLeftP;
			lTop = settings->LTopP;
			rRight = settings->RRightP;
			rTop = settings->RTopP;
			turboLeft = settings->TurboLeftP;
			turboTop = settings->TurboTopP;
			comboLeft = settings->ComboLeftP;
			comboTop = settings->ComboTopP;
		}
	}

	void VirtualController::CreateRenderRectangles(void)
	{

		float value = EmulatorSettings::Current->ControllerScale / 100.0f;
		float value2 = EmulatorSettings::Current->ButtonScale / 100.0f;

		// Visible Rectangles
		this->padCrossRectangle.left = padCenterX - 105 * value * this->hscale ;
		this->padCrossRectangle.right =  padCenterX + 105 * value * this->hscale ;
		this->padCrossRectangle.top = padCenterY -  105 * value * this->hscale;
		this->padCrossRectangle.bottom = padCenterY +  105 * value * this->hscale;

		this->aRectangle.left = aCenterX - 45 * value2 * this->hscale;
		this->aRectangle.right =  aCenterX + 45 * value2 * this->hscale;
		this->aRectangle.top = aCenterY - 45 * value2 * this->hscale;
		this->aRectangle.bottom = aCenterY + 45 * value2 * this->hscale;

		this->bRectangle.left = bCenterX - 45 * value2 * this->hscale;
		this->bRectangle.right =  bCenterX + 45 * value2 * this->hscale;
		this->bRectangle.top = bCenterY - 45 * value2 * this->hscale;
		this->bRectangle.bottom = bCenterY + 45 * value2 * this->hscale;

		this->xRectangle.left = xCenterX - 45 * value2 * this->hscale;
		this->xRectangle.right =  xCenterX + 45 * value2 * this->hscale;
		this->xRectangle.top = xCenterY - 45 * value2 * this->hscale;
		this->xRectangle.bottom = xCenterY + 45 * value2 * this->hscale;

		this->yRectangle.left = yCenterX - 45 * value2 * this->hscale;
		this->yRectangle.right =  yCenterX + 45 * value2 * this->hscale;
		this->yRectangle.top = yCenterY - 45 * value2 * this->hscale;
		this->yRectangle.bottom = yCenterY + 45 * value2 * this->hscale;

		this->startRectangle.left = startLeft;
		this->startRectangle.right = this->startRectangle.left + 100 * value2 * this->hscale; 
		this->startRectangle.top = startTop; 
		this->startRectangle.bottom = this->startRectangle.top + 50 * value2 * this->hscale;

		this->selectRectangle.right = selectRight;
		this->selectRectangle.left = this->selectRectangle.right - 100 * value2 * this->hscale; 
		this->selectRectangle.top = selectTop; 
		this->selectRectangle.bottom = this->selectRectangle.top + 50 * value2 * this->hscale;

		this->turboRectangle.left = turboLeft;
		this->turboRectangle.right = this->turboRectangle.left + 50 * value2 * this->hscale;
		this->turboRectangle.top = turboTop;
		this->turboRectangle.bottom = this->turboRectangle.top + 50 * value2 * this->hscale;

		this->comboRectangle.left = comboLeft;
		this->comboRectangle.right = this->comboRectangle.left + 50 * value2 * this->hscale;
		this->comboRectangle.top = comboTop;
		this->comboRectangle.bottom = this->comboRectangle.top + 50 * value2 * this->hscale;

		this->lRectangle.left = lLeft;
		this->lRectangle.right = this->lRectangle.left +  90 * value2 * this->hscale;
		this->lRectangle.top =  lTop; 
		this->lRectangle.bottom = this->lRectangle.top +  53 * value2 * this->hscale;

		
		this->rRectangle.right = rRight;
		this->rRectangle.left = this->rRectangle.right - 90 * value2 * this->hscale; 
		this->rRectangle.top =  rTop; 
		this->rRectangle.bottom = this->rRectangle.top +  53 * value2 * this->hscale; 

		


		this->visibleStickPos.x = (LONG) ((this->padCrossRectangle.right + this->padCrossRectangle.left) / 2.0f);
		this->visibleStickPos.y = (LONG) ((this->padCrossRectangle.top + this->padCrossRectangle.bottom) / 2.0f);

		this->visibleStickOffset.x = 0;
		this->visibleStickOffset.y = 0;

		
	}


	void VirtualController::CreateTouchLandscapeRectangles(void)
	{
		//origin at bottom left corner
		EmulatorSettings ^settings = EmulatorSettings::Current;


		float touchVisualQuotient = (this->width / (float) this->touchWidth);

		this->lRect.X = lRectangle.left / touchVisualQuotient;
		this->lRect.Y = (this->height - lRectangle.bottom) / touchVisualQuotient;
		this->lRect.Width = (lRectangle.right - lRectangle.left) / touchVisualQuotient;
		this->lRect.Height = (lRectangle.bottom - lRectangle.top) / touchVisualQuotient;
	
		this->rRect.X = rRectangle.left / touchVisualQuotient;
		this->rRect.Y = (this->height - rRectangle.bottom) / touchVisualQuotient;
		this->rRect.Width = (rRectangle.right - rRectangle.left) / touchVisualQuotient;
		this->rRect.Height = (rRectangle.bottom - rRectangle.top) / touchVisualQuotient;

		// Cross		
		this->leftRect.X = this->padCrossRectangle.left / touchVisualQuotient;
		this->leftRect.Y = (this->height - this->padCrossRectangle.bottom) / touchVisualQuotient;
		this->leftRect.Width = ((this->padCrossRectangle.right - this->padCrossRectangle.left) / 3.0f) / touchVisualQuotient;
		this->leftRect.Height = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) / touchVisualQuotient;
				
		this->rightRect.Width = ((this->padCrossRectangle.right - this->padCrossRectangle.left) / 3.0f) / touchVisualQuotient;
		this->rightRect.Height = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) / touchVisualQuotient;
		this->rightRect.X = (this->padCrossRectangle.left / touchVisualQuotient) + 2.0f * this->rightRect.Width;
		this->rightRect.Y = (this->height - this->padCrossRectangle.bottom) / touchVisualQuotient;
		
		
		this->upRect.Height = ((this->padCrossRectangle.bottom - this->padCrossRectangle.top) / 3.0f) / touchVisualQuotient;
		this->upRect.Width = (this->padCrossRectangle.right - this->padCrossRectangle.left) / touchVisualQuotient;
		this->upRect.X = (this->padCrossRectangle.left) / touchVisualQuotient;
		this->upRect.Y = (this->height - this->padCrossRectangle.bottom) / touchVisualQuotient + 2.0f * this->upRect.Height;
		
		
		this->downRect.Height = ((this->padCrossRectangle.bottom - this->padCrossRectangle.top) / 3.0f) / touchVisualQuotient;
		this->downRect.Width = (this->padCrossRectangle.right - this->padCrossRectangle.left) / touchVisualQuotient;
		this->downRect.X = (this->padCrossRectangle.left) / touchVisualQuotient;
		this->downRect.Y = (this->height - this->padCrossRectangle.bottom) / touchVisualQuotient;
		
		
				
		
		this->aRect.X = this->aRectangle.left / touchVisualQuotient;
		this->aRect.Y = (this->height - this->aRectangle.bottom) / touchVisualQuotient;
		this->aRect.Width = (this->aRectangle.right - this->aRectangle.left) / touchVisualQuotient;
		this->aRect.Height = (this->aRectangle.bottom - this->aRectangle.top)  / touchVisualQuotient;
			
		this->bRect.X = this->bRectangle.left / touchVisualQuotient;
		this->bRect.Y = (this->height - this->bRectangle.bottom) / touchVisualQuotient;
		this->bRect.Width = (this->bRectangle.right - this->bRectangle.left) / touchVisualQuotient;
		this->bRect.Height = (this->bRectangle.bottom - this->bRectangle.top)  / touchVisualQuotient;

		this->xRect.X = this->xRectangle.left / touchVisualQuotient;
		this->xRect.Y = (this->height - this->xRectangle.bottom) / touchVisualQuotient;
		this->xRect.Width = (this->xRectangle.right - this->xRectangle.left) / touchVisualQuotient;
		this->xRect.Height = (this->xRectangle.bottom - this->xRectangle.top)  / touchVisualQuotient;

		this->yRect.X = this->yRectangle.left / touchVisualQuotient;
		this->yRect.Y = (this->height - this->yRectangle.bottom) / touchVisualQuotient;
		this->yRect.Width = (this->yRectangle.right - this->yRectangle.left) / touchVisualQuotient;
		this->yRect.Height = (this->yRectangle.bottom - this->yRectangle.top)  / touchVisualQuotient;

		this->selectRect.X = this->selectRectangle.left / touchVisualQuotient;
		this->selectRect.Y = (this->height - this->selectRectangle.bottom) / touchVisualQuotient;
		this->selectRect.Width = (this->selectRectangle.right - this->selectRectangle.left) / touchVisualQuotient;
		this->selectRect.Height = (this->selectRectangle.bottom - this->selectRectangle.top)  / touchVisualQuotient;


		this->startRect.X = this->startRectangle.left / touchVisualQuotient;
		this->startRect.Y = (this->height - this->startRectangle.bottom) / touchVisualQuotient;
		this->startRect.Width = (this->startRectangle.right - this->startRectangle.left) / touchVisualQuotient;
		this->startRect.Height = (this->startRectangle.bottom - this->startRectangle.top)  / touchVisualQuotient;


		this->turboRect.X = this->turboRectangle.left / touchVisualQuotient;
		this->turboRect.Y = (this->height - this->turboRectangle.bottom) / touchVisualQuotient;
		this->turboRect.Width = (this->turboRectangle.right - this->turboRectangle.left) / touchVisualQuotient;
		this->turboRect.Height = (this->turboRectangle.bottom - this->turboRectangle.top) / touchVisualQuotient;

		this->comboRect.X = this->comboRectangle.left / touchVisualQuotient;
		this->comboRect.Y = (this->height - this->comboRectangle.bottom) / touchVisualQuotient;
		this->comboRect.Width = (this->comboRectangle.right - this->comboRectangle.left) / touchVisualQuotient;
		this->comboRect.Height = (this->comboRectangle.bottom - this->comboRectangle.top) / touchVisualQuotient;

		int dpad = settings->DPadStyle;


		this->stickBoundaries.X = this->padCrossRectangle.left / touchVisualQuotient;
		this->stickBoundaries.Y = (this->height - this->padCrossRectangle.bottom) / touchVisualQuotient;
		this->stickBoundaries.Width = (this->padCrossRectangle.right - this->padCrossRectangle.left) / touchVisualQuotient;
		this->stickBoundaries.Height = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) / touchVisualQuotient;

		if (dpad >= 2)
		{
			this->stickPos.X = this->stickBoundaries.X + this->stickBoundaries.Width / 2.0f ;
			this->stickPos.Y = this->stickBoundaries.Y  + this->stickBoundaries.Height / 2.0f ;

			this->stickOffset.X = 0.0f;
			this->stickOffset.Y = 0.0f;
		}
	}


	


	
	void VirtualController::CreateTouchPortraitRectangles(void)
	{
		//origin at top left corner
		EmulatorSettings ^settings = EmulatorSettings::Current;
		float touchVisualQuotient = (this->width / (float) this->touchWidth);


		this->lRect.Y = this->lRectangle.top / touchVisualQuotient;
		this->lRect.X = this->lRectangle.left / touchVisualQuotient;
		this->lRect.Height = (this->lRectangle.bottom - this->lRectangle.top) / touchVisualQuotient;
		this->lRect.Width = (this->lRectangle.right - this->lRectangle.left) / touchVisualQuotient;
	
		this->rRect.Y = this->rRectangle.top / touchVisualQuotient;
		this->rRect.X = this->rRectangle.left / touchVisualQuotient;
		this->rRect.Height = (this->rRectangle.bottom - this->rRectangle.top) / touchVisualQuotient;
		this->rRect.Width = (this->rRectangle.right - this->rRectangle.left) / touchVisualQuotient;

		// Cross		
		this->leftRect.X = this->padCrossRectangle.left / touchVisualQuotient;
		this->leftRect.Y = this->padCrossRectangle.top / touchVisualQuotient;
		this->leftRect.Width = ((this->padCrossRectangle.right - this->padCrossRectangle.left) / 3.0f) / touchVisualQuotient;
		this->leftRect.Height = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) / touchVisualQuotient;
		
		this->rightRect.Width = ((this->padCrossRectangle.right - this->padCrossRectangle.left) / 3.0f) / touchVisualQuotient;
		this->rightRect.Y = this->padCrossRectangle.top / touchVisualQuotient;

		this->rightRect.Height = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) /touchVisualQuotient;
		this->rightRect.X = (this->padCrossRectangle.left / touchVisualQuotient) + 2.0f * this->rightRect.Width;
		

		this->upRect.Y = this->padCrossRectangle.top / touchVisualQuotient;
		this->upRect.X = this->padCrossRectangle.left / touchVisualQuotient;
		this->upRect.Height = ((this->padCrossRectangle.bottom - this->padCrossRectangle.top) / 3.0f) / touchVisualQuotient;
		this->upRect.Width = (this->padCrossRectangle.right - this->padCrossRectangle.left) / touchVisualQuotient;
		
		this->downRect.Height = ((this->padCrossRectangle.bottom - this->padCrossRectangle.top) / 3.0f) / touchVisualQuotient;
		this->downRect.Y = (this->padCrossRectangle.top / touchVisualQuotient) + 2.0f * this->downRect.Height;
		this->downRect.X = this->padCrossRectangle.left / touchVisualQuotient;
		this->downRect.Width = (this->padCrossRectangle.right - this->padCrossRectangle.left) / touchVisualQuotient;

		// Buttons
	
		this->aRect.Y = this->aRectangle.top / touchVisualQuotient;
		this->aRect.X = this->aRectangle.left / touchVisualQuotient;
		this->aRect.Height = (this->aRectangle.bottom - this->aRectangle.top) / touchVisualQuotient;
		this->aRect.Width = (this->aRectangle.right - this->aRectangle.left) / touchVisualQuotient;

		this->bRect.Y = this->bRectangle.top / touchVisualQuotient;
		this->bRect.X = this->bRectangle.left / touchVisualQuotient;
		this->bRect.Height = (this->bRectangle.bottom - this->bRectangle.top) / touchVisualQuotient;
		this->bRect.Width = (this->bRectangle.right - this->bRectangle.left) / touchVisualQuotient;
	
		this->xRect.Y = this->xRectangle.top / touchVisualQuotient;
		this->xRect.X = this->xRectangle.left / touchVisualQuotient;
		this->xRect.Height = (this->xRectangle.bottom - this->xRectangle.top) / touchVisualQuotient;
		this->xRect.Width = (this->xRectangle.right - this->xRectangle.left) / touchVisualQuotient;


		this->yRect.Y = this->yRectangle.top / touchVisualQuotient;
		this->yRect.X = this->yRectangle.left / touchVisualQuotient;
		this->yRect.Height = (this->yRectangle.bottom - this->yRectangle.top) / touchVisualQuotient;
		this->yRect.Width = (this->yRectangle.right - this->yRectangle.left) / touchVisualQuotient;

		

		this->selectRect.Y = this->selectRectangle.top / touchVisualQuotient;
		this->selectRect.X = this->selectRectangle.left / touchVisualQuotient;
		this->selectRect.Height = (this->selectRectangle.bottom - this->selectRectangle.top) / touchVisualQuotient;
		this->selectRect.Width = (this->selectRectangle.right - this->selectRectangle.left) / touchVisualQuotient;

		this->startRect.Y = this->startRectangle.top / touchVisualQuotient;
		this->startRect.X = this->startRectangle.left / touchVisualQuotient;
		this->startRect.Height = (this->startRectangle.bottom - this->startRectangle.top) / touchVisualQuotient;
		this->startRect.Width = (this->startRectangle.right - this->startRectangle.left) / touchVisualQuotient;

		this->turboRect.Y = this->turboRectangle.top / touchVisualQuotient;
		this->turboRect.X = this->turboRectangle.left / touchVisualQuotient;
		this->turboRect.Height = (this->turboRectangle.bottom - this->turboRectangle.top) / touchVisualQuotient;
		this->turboRect.Width = (this->turboRectangle.right - this->turboRectangle.left) / touchVisualQuotient;

		this->comboRect.Y = this->comboRectangle.top / touchVisualQuotient;
		this->comboRect.X = this->comboRectangle.left / touchVisualQuotient;
		this->comboRect.Height = (this->comboRectangle.bottom - this->comboRectangle.top) / touchVisualQuotient;
		this->comboRect.Width = (this->comboRectangle.right - this->comboRectangle.left) / touchVisualQuotient;

		int dpad = EmulatorSettings::Current->DPadStyle;

		this->stickBoundaries.X = this->padCrossRectangle.left / touchVisualQuotient;
		this->stickBoundaries.Y = this->padCrossRectangle.top / touchVisualQuotient;
		this->stickBoundaries.Width = (this->padCrossRectangle.right - this->padCrossRectangle.left) / touchVisualQuotient;
		this->stickBoundaries.Height = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) / touchVisualQuotient;

		if (dpad >=2)
		{
			this->stickPos.X = this->stickBoundaries.X + this->stickBoundaries.Width / 2.0f;
			this->stickPos.Y = this->stickBoundaries.Y +  this->stickBoundaries.Height / 2.0f;

			this->stickOffset.X = 0.0f;
			this->stickOffset.Y = 0.0f;
			
		}
	}

	void VirtualController::VirtualControllerOnTop(bool onTop)
	{
		this->virtualControllerOnTop = onTop;
		this->UpdateFormat(this->format);
	}

	void VirtualController::SetOrientation(int orientation)
	{
		this->orientation = orientation;
		this->UpdateFormat(this->format);
	}

	int VirtualController::GetOrientation()
	{
		return this->orientation;
	}

	bool VirtualController::CheckTouchableArea(Windows::Foundation::Point p)
	{
		if (this->stickBoundaries.Contains(p) || this->aRect.Contains(p) || this->bRect.Contains(p) || this->xRect.Contains(p) || this->yRect.Contains(p)
			|| this->lRect.Contains(p)|| this->rRect.Contains(p) || this->startRect.Contains(p) || this->selectRect.Contains(p) 
			||this->turboRect.Contains(p) ||this->comboRect.Contains(p))
			return true;

		else
			return false;

	}

	void VirtualController::PointerPressed(PointerPoint ^point)
	{
		EnterCriticalSection(&this->cs);
		this->pointers->Insert(point->PointerId, point);
		this->pointerDescriptions->Insert(point->PointerId, "");
		
		Windows::Foundation::Point p;

		if (this->orientation == ORIENTATION_PORTRAIT)
			p = Windows::Foundation::Point(point->Position.X, point->Position.Y);
		else
		{
			p = Windows::Foundation::Point(point->Position.Y, point->Position.X);

			if(this->orientation == ORIENTATION_LANDSCAPE_RIGHT)
			{
				p.X = this->touchWidth - p.X;
				p.Y = this->touchHeight - p.Y;
			}
		}

		if (EmulatorSettings::Current->VibrationEnabled && CheckTouchableArea(p))
		{
			Windows::Foundation::TimeSpan time;
			time.Duration = 10000000 * EmulatorSettings::Current->VibrationDuration;

			vibrationDevice->Vibrate( time);
		}

		


		int dpad = EmulatorSettings::Current->DPadStyle;
		if(dpad >= 2)
		{
			
			

			if(this->stickBoundaries.Contains(p) )
			{
				float scale = (int) Windows::Graphics::Display::DisplayProperties::ResolutionScale / 100.0f;
				if(dpad == 3)
				{
					stickPos = p;

					if(this->orientation != ORIENTATION_PORTRAIT)
					{
						this->visibleStickPos.x = this->stickPos.X * scale;
						this->visibleStickPos.y = this->height - this->stickPos.Y * scale;
					}else
					{
						this->visibleStickPos.x = this->stickPos.X * scale;
						this->visibleStickPos.y = this->stickPos.Y * scale;
					}
				}

				stickFingerID = point->PointerId;
				stickFingerDown = true;

				stickOffset.X = p.X - this->stickPos.X;
				stickOffset.Y = p.Y - this->stickPos.Y;

				this->visibleStickOffset.x = this->stickOffset.X * scale;
				this->visibleStickOffset.y = this->stickOffset.Y * scale;
			}
		}

		LeaveCriticalSection(&this->cs);
		
	}

	void VirtualController::PointerMoved(PointerPoint ^point)
	{
		EnterCriticalSection(&this->cs);
		if(this->pointers->HasKey(point->PointerId))
		{
			this->pointers->Insert(point->PointerId, point);
		}
		
		Windows::Foundation::Point p;

		if (this->orientation == ORIENTATION_PORTRAIT)
			p = Windows::Foundation::Point(point->Position.X, point->Position.Y);
		else
		{
			p = Windows::Foundation::Point(point->Position.Y, point->Position.X);

			if(this->orientation == ORIENTATION_LANDSCAPE_RIGHT)
			{
				p.X = this->touchWidth - p.X;
				p.Y = this->touchHeight - p.Y;
			}
		}


		int dpad = EmulatorSettings::Current->DPadStyle;
		if(dpad >= 2)
		{
			if(this->stickFingerDown && point->PointerId == this->stickFingerID)
			{
				
				float scale = (int) Windows::Graphics::Display::DisplayProperties::ResolutionScale / 100.0f;

				stickOffset.X = p.X - this->stickPos.X;
				stickOffset.Y = p.Y - this->stickPos.Y;

				this->visibleStickOffset.x = this->stickOffset.X * scale;
				this->visibleStickOffset.y = this->stickOffset.Y * scale;
			}
		}

		LeaveCriticalSection(&this->cs);
	}

	void VirtualController::PointerReleased(PointerPoint ^point)
	{
		EnterCriticalSection(&this->cs);
		if(this->pointers->HasKey(point->PointerId))
		{
			//get the description
			Platform::String^ desc = pointerDescriptions->Lookup(point->PointerId);
			unsigned int key2 = point->PointerId;

			this->pointers->Remove(point->PointerId);
			this->pointerDescriptions->Remove(point->PointerId);

			//find point that may not be removed due to released event not triggered and mark it
			if (desc != "")
			{
				for (auto i = this->pointerDescriptions->First(); i->HasCurrent; i->MoveNext())
				{
					Platform::String ^desc2= i->Current->Value;
					unsigned int key2 = i->Current->Key;


					if (desc2 == desc ) 
					{
						//remove the points
						this->pointerDescriptions->Remove(key2);
						this->pointers->Remove(key2);
						break; //has to break or the loop will cause Changed_state exception
					}

				}

				if (desc == "turbo") //user just released the turbo button, so we toggle turbo mode
				{
					if (Direct3DBackground::ToggleTurboMode)
					{
						Direct3DBackground::ToggleTurboMode();
					}
				}
			}
			

			int dpad = EmulatorSettings::Current->DPadStyle;
			if(dpad >= 2)
			{
				if(this->stickFingerDown && desc == "joystick")
				{
					this->stickFingerDown = false;
					this->stickFingerID = 0;

					this->stickOffset.X = 0;
					this->stickOffset.Y = 0;

					this->visibleStickOffset.x = 0;
					this->visibleStickOffset.y = 0;
				}
			}
		}

		

		LeaveCriticalSection(&this->cs);
	}

	const ControllerState *VirtualController::GetControllerState(void)
	{
		ZeroMemory(&this->state, sizeof(ControllerState));
		
		int dpad = EmulatorSettings::Current->DPadStyle;

		EnterCriticalSection(&this->cs);
		for (auto i = this->pointers->First(); i->HasCurrent; i->MoveNext())
		{
			PointerPoint ^p = i->Current->Value;
			

			Windows::Foundation::Point point;

			if (this->orientation == ORIENTATION_PORTRAIT)
				point = Windows::Foundation::Point(p->Position.X, p->Position.Y);
			else
			{
				point = Windows::Foundation::Point(p->Position.Y, p->Position.X);
				if(this->orientation == ORIENTATION_LANDSCAPE_RIGHT)
				{
					point.X = this->touchWidth - point.X;
					point.Y = this->touchHeight - point.Y;
				}
			}

			if(dpad == 0 || dpad == 1)
			{
				if(this->leftRect.Contains(point))
				{
					state.LeftPressed = true;
					//add the description for this point
					this->pointerDescriptions->Insert(i->Current->Key, "joystick");
				}
				if(this->upRect.Contains(point))
				{
					state.UpPressed = true;
					this->pointerDescriptions->Insert(i->Current->Key, "joystick");
				}
				if(this->rightRect.Contains(point))
				{
					state.RightPressed = true;
					this->pointerDescriptions->Insert(i->Current->Key, "joystick");
				}
				if(this->downRect.Contains(point))
				{
					state.DownPressed = true;
					this->pointerDescriptions->Insert(i->Current->Key, "joystick");
				}

				if (dpad == 0)
				{
					//this code make the d-pad a 4-way only, i.e. resolve conflict
					if (state.LeftPressed && state.UpPressed)
					{
						Windows::Foundation::Point pointUp = Windows::Foundation::Point(this->upRect.X, this->upRect.Y + upRect.Height);
						Windows::Foundation::Point pointLeft = Windows::Foundation::Point(this->leftRect.X + this->leftRect.Width, this->leftRect.Y);
						if (CalculateDistanceDiff(pointUp, pointLeft, point) < 0)
							state.LeftPressed = false;
						else
							state.UpPressed = false;

					}
					else if (state.LeftPressed && state.DownPressed)
					{
					
						Windows::Foundation::Point pointLeft = Windows::Foundation::Point(leftRect.X, leftRect.Y);
						Windows::Foundation::Point pointDown = Windows::Foundation::Point(downRect.X + downRect.Width, downRect.Y + downRect.Height);
						if (CalculateDistanceDiff(pointLeft, pointDown, point) < 0)
							state.DownPressed = false;
						else
							state.LeftPressed = false;

					}
					else if (state.RightPressed && state.DownPressed)
					{
					
						Windows::Foundation::Point pointRight = Windows::Foundation::Point(rightRect.X, rightRect.Y + rightRect.Height);
						Windows::Foundation::Point pointDown = Windows::Foundation::Point(downRect.X + downRect.Width, downRect.Y);
						if (CalculateDistanceDiff(pointRight, pointDown, point) < 0)
							state.DownPressed = false;
						else
							state.RightPressed = false;

					}
					else if (state.RightPressed && state.UpPressed)
					{
					
						Windows::Foundation::Point pointRight = Windows::Foundation::Point(rightRect.X + rightRect.Width, rightRect.Y + rightRect.Height);
						Windows::Foundation::Point pointUp = Windows::Foundation::Point(upRect.X , upRect.Y);
						if (CalculateDistanceDiff(pointRight, pointUp, point) < 0)
							state.UpPressed = false;
						else
							state.RightPressed = false;

					}
				}

			}else
			{
				if (this->stickBoundaries.Contains(point))
					this->pointerDescriptions->Insert(i->Current->Key, "joystick");

				if(this->stickFingerDown && p->PointerId == this->stickFingerID)
				{
					float deadzone = EmulatorSettings::Current->Deadzone;
					float controllerScale = EmulatorSettings::Current->ControllerScale / 100.0f;
					float length = (float) sqrt(this->stickOffset.X * this->stickOffset.X + this->stickOffset.Y * this->stickOffset.Y);
					float scale = (int) Windows::Graphics::Display::DisplayProperties::ResolutionScale / 100.0f;
					if(length >= deadzone * scale * controllerScale)
					{
						// Deadzone of 15
						float unitX = 1.0f;
						float unitY = 0.0f;
						float normX = this->stickOffset.X / length;
						float normY = this->stickOffset.Y / length;

						float dot = unitX * normX + unitY * normY;
						float rad = (float) acos(dot);

						if(normY < 0.0f)
						{
							rad = 6.28f - rad;
						}

						if(this->orientation == ORIENTATION_PORTRAIT)
						{
							rad = 6.28f - rad;
						}

						if((rad >= 0 && rad < 1.046f) || (rad > 5.234f && rad < 6.28f))
						{
							state.RightPressed = true;

						}
						if(rad >= 0.523f && rad < 2.626f)
						{
							state.UpPressed = true;

						}
						if(rad >= 2.093f && rad < 4.186f)
						{
							state.LeftPressed = true;

						}
						if(rad >= 3.663f && rad < 5.756f)
						{
							state.DownPressed = true;

						}
					}
				}
			}
			if(this->startRect.Contains(point))
			{
				state.StartPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "start");
			}
			if(this->selectRect.Contains(point))
			{
				state.SelectPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "select");
			}
			if (this->turboRect.Contains(point))
			{
				state.TurboPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "turbo");
			}
			if (this->comboRect.Contains(point))
			{
				state.ComboPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "combo");
			}
			if(this->lRect.Contains(point))
			{
				state.LPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "l");
			}
			if(this->rRect.Contains(point))
			{
				state.RPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "r");
			}
			if(this->aRect.Contains(point))
			{
				state.APressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "a");
			}
			if(this->bRect.Contains(point))
			{
				state.BPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "b");
			}
			if(this->xRect.Contains(point))
			{
				state.XPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "x");
			}
			if(this->yRect.Contains(point))
			{
				state.YPressed = true;
				this->pointerDescriptions->Insert(i->Current->Key, "y");
			}
		}
		LeaveCriticalSection(&this->cs);

		return &this->state;
	}

	double VirtualController::CalculateDistanceDiff(Windows::Foundation::Point point1, Windows::Foundation::Point point2, Windows::Foundation::Point target)
	{
		double distance1 = sqrt(pow(point1.X - target.X, 2.0) + pow(point1.Y - target.Y, 2.0));
		double distance2 = sqrt(pow(point2.X - target.X, 2.0) + pow(point2.Y - target.Y, 2.0));
		return distance1 - distance2;
	}

	void VirtualController::GetCrossRectangle(RECT *rect)
	{
		*rect = this->padCrossRectangle;
	}

	void VirtualController::GetARectangle(RECT *rect)
	{
		*rect = this->aRectangle;
	}

	void VirtualController::GetBRectangle(RECT *rect)
	{
		*rect = this->bRectangle;
	}
	void VirtualController::GetXRectangle(RECT *rect)
	{
		*rect = this->xRectangle;
	}
	void VirtualController::GetYRectangle(RECT *rect)
	{
		*rect = this->yRectangle;
	}


	void VirtualController::GetStartRectangle(RECT *rect)
	{
		*rect = this->startRectangle;
	}

	void VirtualController::GetSelectRectangle(RECT *rect)
	{
		*rect = this->selectRectangle;
	}

	void VirtualController::GetTurboRectangle(RECT *rect)
	{
		*rect = this->turboRectangle;
	}

	void VirtualController::GetComboRectangle(RECT *rect)
	{
		*rect = this->comboRectangle;
	}


	void VirtualController::GetLRectangle(RECT *rect)
	{
		*rect = this->lRectangle;
	}

	void VirtualController::GetRRectangle(RECT *rect)
	{
		*rect = this->rRectangle;
	}
	
	void VirtualController::GetStickRectangle(RECT *rect)
	{
		int quarterWidth = (this->padCrossRectangle.right - this->padCrossRectangle.left) / 5;
		int quarterHeight = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) / 5;

		if(this->orientation != ORIENTATION_PORTRAIT)
		{
			rect->left = (this->visibleStickPos.x + this->visibleStickOffset.x) - quarterWidth;
			rect->right = rect->left + 2 * quarterWidth;
			rect->top = (this->visibleStickPos.y - this->visibleStickOffset.y) - quarterHeight;
			rect->bottom = rect->top + 2 * quarterHeight;
		}else
		{

			rect->left = (this->visibleStickPos.x + this->visibleStickOffset.x) - quarterWidth;
			rect->right = rect->left + 2 * quarterWidth;
			rect->top = (this->visibleStickPos.y + this->visibleStickOffset.y) - quarterHeight;
			rect->bottom = rect->top + 2 * quarterHeight;
		}
	}
	
	void VirtualController::GetStickCenterRectangle(RECT *rect)
	{
		int quarterWidth = (this->padCrossRectangle.right - this->padCrossRectangle.left) / 20;
		int quarterHeight = (this->padCrossRectangle.bottom - this->padCrossRectangle.top) / 20;

		if(this->orientation != ORIENTATION_PORTRAIT)
		{
			rect->left = this->visibleStickPos.x - quarterWidth;
			rect->right = rect->left + 2 * quarterWidth;
			rect->top = this->visibleStickPos.y - quarterHeight;
			rect->bottom = rect->top + 2 * quarterHeight;
		}else
		{

			rect->left = this->visibleStickPos.x - quarterWidth;
			rect->right = rect->left + 2 * quarterWidth;
			rect->top = this->visibleStickPos.y - quarterHeight;
			rect->bottom = rect->top + 2 * quarterHeight;
		}
	}

	bool VirtualController::StickFingerDown(void)
	{
		return this->stickFingerDown;
	}
}