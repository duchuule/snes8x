#include "pch.h"
#include "CPositionVirtualController.h"
#include "EmulatorSettings.h"

using namespace PhoneDirect3DXamlAppComponent;

namespace Emulator
{
	CPositionVirtualController::CPositionVirtualController(void)
	{
		virtualControllerOnTop = false;
		stickFingerDown = false;
		InitializeCriticalSectionEx(&this->cs, 0, 0);
		this->pointerInfos = new std::map<unsigned int, PointerInfo*>();
	}

	CPositionVirtualController::~CPositionVirtualController(void)
	{
		DeleteCriticalSection(&this->cs);
	}
	
	void CPositionVirtualController::SetControllerPosition(Windows::Foundation::Collections::IVector<int>^ cpos)
	{
		padCenterX = cpos->GetAt(0);
		padCenterY = cpos->GetAt(1);
		aCenterX = cpos->GetAt(2);
		aCenterY = cpos->GetAt(3);
		bCenterX = cpos->GetAt(4);
		bCenterY = cpos->GetAt(5);
		startLeft = cpos->GetAt(6);
		startTop = cpos->GetAt(7);
		selectRight = cpos->GetAt(8);
		selectTop = cpos->GetAt(9);
		lLeft = cpos->GetAt(10);
		lTop = cpos->GetAt(11);
		rRight = cpos->GetAt(12);
		rTop = cpos->GetAt(13);
		xCenterX = cpos->GetAt(14);
		xCenterY = cpos->GetAt(15);
		yCenterX = cpos->GetAt(16);
		yCenterY = cpos->GetAt(17);

		//update 
		this->CreateRenderRectangles();

		if(this->orientation != ORIENTATION_PORTRAIT)
			this->CreateTouchLandscapeRectangles();
		else
			this->CreateTouchPortraitRectangles();
	}


	void CPositionVirtualController::GetControllerPosition(Windows::Foundation::Collections::IVector<int>^ ret)
	{

		ret->SetAt(0, padCenterX);
		ret->SetAt(1, padCenterY);
		ret->SetAt(2, aCenterX);
		ret->SetAt(3, aCenterY);
		ret->SetAt(4, bCenterX);
		ret->SetAt(5, bCenterY);
		ret->SetAt(6, startLeft);
		ret->SetAt(7, startTop);
		ret->SetAt(8, selectRight);
		ret->SetAt(9, selectTop);
		ret->SetAt(10, lLeft);
		ret->SetAt(11, lTop);
		ret->SetAt(12, rRight);
		ret->SetAt(13, rTop);
		ret->SetAt(14, xCenterX);
		ret->SetAt(15, xCenterY);
		ret->SetAt(16, yCenterX);
		ret->SetAt(17, yCenterY);

	}


	
	void CPositionVirtualController::PointerPressed(PointerPoint ^point)
	{
		EnterCriticalSection(&this->cs);
		PointerInfo* pinfo = new PointerInfo();
		pinfo->point = point;
		pinfo->description = "";
		pinfo->IsMoved = false;

		this->pointerInfos->insert(std::pair<unsigned int, PointerInfo*>(point->PointerId, pinfo));
		

		LeaveCriticalSection(&this->cs);
		
	}

	void CPositionVirtualController::PointerMoved(PointerPoint ^point)
	{
		EnterCriticalSection(&this->cs);

		std::map<unsigned int, PointerInfo*>::iterator iter = this->pointerInfos->find(point->PointerId);

		if(iter != this->pointerInfos->end())
		{
			PointerInfo* pinfo = iter->second;
			pinfo->IsMoved = true;


			//move the control
			float dx = 0; 
			float dy = 0; 
			float scale = (int) Windows::Graphics::Display::DisplayProperties::ResolutionScale / 100.0f;
			
			if(this->orientation != ORIENTATION_PORTRAIT)
			{
				dx = (point->Position.Y - pinfo->point->Position.Y) * scale;
				dy = -(point->Position.X - pinfo->point->Position.X) * scale;
			}
			else
			{
				dx = (point->Position.X - pinfo->point->Position.X) * scale;
				dy = (point->Position.Y - pinfo->point->Position.Y) * scale;
			}

			if (pinfo->description == "joystick")
			{
				this->padCenterX += dx;
				this->padCenterY += dy;
			}
			else if (pinfo->description == "l")
			{
				this->lLeft += dx;
				this->lTop += dy;
			}
			else if (pinfo->description == "r")
			{
				this->rRight += dx;
				this->rTop += dy;
			}
			else if (pinfo->description == "a")
			{
				this->aCenterX += dx;
				this->aCenterY += dy;
			}
			else if (pinfo->description == "b")
			{
				this->bCenterX += dx;
				this->bCenterY += dy;
			}
			else if (pinfo->description == "x")
			{
				this->xCenterX += dx;
				this->xCenterY += dy;
			}
			else if (pinfo->description == "y")
			{
				this->yCenterX += dx;
				this->yCenterY += dy;
			}
			else if (pinfo->description == "select")
			{
				this->selectRight += dx;
				this->selectTop += dy;
			}
			else if (pinfo->description == "start")
			{
				this->startLeft += dx;
				this->startTop += dy;
			}

			//record new touch position
			pinfo->point = point;

			//update controller position on screen
			this->CreateRenderRectangles();

			//update touh region on screen
			if(this->orientation != ORIENTATION_PORTRAIT)
				this->CreateTouchLandscapeRectangles();
			else
				this->CreateTouchPortraitRectangles();		

		}

		
		

		LeaveCriticalSection(&this->cs);
	}

	void CPositionVirtualController::PointerReleased(PointerPoint ^point)
	{
		EnterCriticalSection(&this->cs);
		std::map<unsigned int, PointerInfo*>::iterator iter = this->pointerInfos->find(point->PointerId);
		if(iter != this->pointerInfos->end())
		{
			this->pointerInfos->erase(iter);
		}

		

		LeaveCriticalSection(&this->cs);
	}

	const ControllerState *CPositionVirtualController::GetControllerState(void)
	{
		ZeroMemory(&this->state, sizeof(ControllerState));

		int dpad = EmulatorSettings::Current->DPadStyle;

		EnterCriticalSection(&this->cs);
		//typedef std::map<unsigned int, PointerInfo*>::iterator it_type;




		auto i = this->pointerInfos->begin();
		if (i != this->pointerInfos->end()) //we only read in one point
		{
			PointerPoint ^p = i->second->point;

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

			if (this->stickBoundaries.Contains(point))
			{
				i->second->description = "joystick";
				state.JoystickPressed = true;
			}

		
			if(this->startRect.Contains(point))
			{
				state.StartPressed = true;
				i->second->description = "start";

			}
			if(this->selectRect.Contains(point))
			{
				state.SelectPressed = true;
				i->second->description = "select";

			}
			if(this->lRect.Contains(point))
			{
				state.LPressed = true;
				i->second->description = "l";

			}
			if(this->rRect.Contains(point))
			{
				state.RPressed = true;
				i->second->description = "r";

			}
			if(this->aRect.Contains(point))
			{
				state.APressed = true;
				i->second->description = "a";

			}
			if(this->bRect.Contains(point))
			{
				state.BPressed = true;
				i->second->description = "b";
			}
			if(this->xRect.Contains(point))
			{
				state.XPressed = true;
				i->second->description = "x";

			}
			if(this->yRect.Contains(point))
			{
				state.YPressed = true;
				i->second->description = "y";
			}
		}
		LeaveCriticalSection(&this->cs);

		return &this->state;
	}

	
}