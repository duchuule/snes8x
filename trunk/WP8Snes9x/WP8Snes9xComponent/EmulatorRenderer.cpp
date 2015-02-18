#include "pch.h"
#include "EmulatorRenderer.h"
#include "EmulatorFileHandler.h"
#include "DDSTextureLoader.h"
#include "WP8Snes9xComponent.h"

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::UI::Core;
using namespace Windows::Graphics::Display;
using namespace PhoneDirect3DXamlAppComponent;


typedef uint8_t u8;

bool cameraPressed = false;
bool autoFireToggle = false;
int mappedButton = 0;

float lastElapsed = 0.0f;
int framesNotRendered = 0;
extern bool turbo;


double SolveForAngle(double a, double b, double c)
{
	const double epsilon = 0.0000000000000001;
	double x = -1001.0, x2 = -1001.0;
	//solve the function A cos(x) + B sin(x) = C
	if (abs(a + c) < epsilon) //when a + c = 0
	{
		x = (-2 * atan(a / b)) / 3.14159265 * 180;
	}
	else
	{
		double check = a*a + b*b + a*c - b*sqrt(a*a + b*b - c*c);
		if (abs(check) > epsilon)
		{
			//2 ArcTan[(b - Sqrt[a ^ 2 + b ^ 2 - c ^ 2]) / (a + c)]
			x = (2 * atan((b - sqrt(a*a + b*b - c*c)) / (a + c))) / 3.14159265 * 180;
		}

		check = a*a + b*b + a*c + b*sqrt(a*a + b*b - c*c);
		if (abs(check > epsilon))
		{
			//x == 2 ArcTan[(b + Sqrt[a^2 + b^2 - c^2])/(a + c)]
			x2 = (2 * atan((b + sqrt(a*a + b*b - c*c)) / (a + c))) / 3.14159265 * 180;
		}
	}

	if (x < -1000 && x2 > -1000)
		x = x2;

	if (x > -1000 && x2 > -1000) //2 solutions, choose the most likely one
	{
		if (abs(x2) < abs(x))
			x = x2;
	}

	return x;
}

double deg2rad(double deg)
{
	return deg / 180.0 * 3.14159265;
}

u8 getMotionInput()
{
	//bit order: left: 1, right: 2, up: 4, down: 8
	int ret = 0;


	VirtualController *controller = VirtualController::GetInstance();
	if (!controller)
		return ret;

	bool left = false;
	bool right = false;
	bool up = false;
	bool down = false;

	EmulatorSettings ^settings = EmulatorSettings::Current;

	Windows::Devices::Sensors::Accelerometer^ accl = Direct3DBackground::getAccelormeter();
	Windows::Devices::Sensors::Inclinometer^ incl = Direct3DBackground::getInclinometer();

	//Motion Control
	//see Tilt Sensing Using a Three-Axis Accelerometer for complete equations
	//[Gx, Gy, Gz]^T = Rx(phi) Ry(theta) [Gx0, Gy0, Gz0]^T
	// Combined rotation matrix: [Gx0*cos(theta) - Gz0*sin(theta), Gx0*sin(theta)*sin(phi)+Gy0*cos(phi) + cos(theta)*sin(phi)*Gz0,
	//							Gx0*cos(phi)*sin(theta) + Gy0*(-sin(phi)) + cos(theta)*cos(phi)*Gz0 ]

	double rotationDeadZone = 10;
	double g0[3] = { settings->RestAngleX, settings->RestAngleY, settings->RestAngleZ };


	//correct for orientation if needed
	if (settings->MotionAdaptOrientation)
	{
		if (settings->UseMotionControl == 1)
		{
			if (controller->GetOrientation() == ORIENTATION_PORTRAIT)
			{
				if (abs(settings->RestAngleX) < abs(settings->RestAngleY)) //phone is calibrated in portrait, RestAngleX = 0, Y = -0.7
				{
					g0[0] = settings->RestAngleX;
					g0[1] = settings->RestAngleY;

				}
				else //phone is calibrated in landscape left
				{
					if (settings->RestAngleX < 0)  //calibrated in landscape left
					{
						g0[0] = settings->RestAngleY;
						g0[1] = settings->RestAngleX;
					}
					else  //calibrated in landscape right
					{
						g0[0] = -settings->RestAngleY;
						g0[1] = -settings->RestAngleX;
					}
				}
			}
			else if (controller->GetOrientation() == ORIENTATION_LANDSCAPE) //current orientation is landscape left
			{
				if (abs(settings->RestAngleX) < abs(settings->RestAngleY)) //phone is calibrated in portrait, RestAngleX = 0, Y = -0.7
				{
					g0[0] = settings->RestAngleY;
					g0[1] = -settings->RestAngleX;
				}
				else //phone is calibrated in landscape left
				{
					if (settings->RestAngleX < 0)  //calibrated in landscape left
					{
						g0[0] = settings->RestAngleX;
						g0[1] = settings->RestAngleY;
					}
					else  //calibrated in landscape right
					{
						g0[0] = -settings->RestAngleX;
						g0[1] = -settings->RestAngleY;
					}
				}
			}
			else //current orientation is landscape right
			{
				if (abs(settings->RestAngleX) < abs(settings->RestAngleY)) //phone is calibrated in portrait, RestAngleX = 0, Y = -0.7
				{
					g0[0] = -settings->RestAngleY;
					g0[1] = settings->RestAngleX;
				}
				else //phone is calibrated in landscape left
				{
					if (settings->RestAngleX < 0)  //calibrated in landscape left
					{
						g0[0] = -settings->RestAngleX;
						g0[1] = -settings->RestAngleY;
					}
					else  //calibrated in landscape right
					{
						g0[0] = settings->RestAngleX;
						g0[1] = settings->RestAngleY;
					}
				}
			}
		}
		else if (settings->UseMotionControl == 2) //inclinometer
		{
			if (controller->GetOrientation() == ORIENTATION_PORTRAIT) //RestAngleX = 0, Y = 45
			{
				if (abs(settings->RestAngleX) < abs(settings->RestAngleY)) //phone is calibrated in portrait, RestAngleX = 0, Y = 45
				{
					g0[0] = settings->RestAngleX;
					g0[1] = settings->RestAngleY;

				}
				else //phone is calibrated in landscape 
				{
					if (settings->RestAngleX < 0)  //calibrated in landscape left
					{
						g0[0] = settings->RestAngleY;
						g0[1] = -settings->RestAngleX;
					}
					else  //calibrated in landscape right
					{
						g0[0] = -settings->RestAngleY;
						g0[1] = settings->RestAngleX;
					}
				}
			}
			else if (controller->GetOrientation() == ORIENTATION_LANDSCAPE) //current orientation is landscape left, RestAngleX = -45, Y = 0
			{
				if (abs(settings->RestAngleX) < abs(settings->RestAngleY)) //phone is calibrated in portrait, RestAngleX = 0, Y = 45
				{
					g0[0] = -settings->RestAngleY;
					g0[1] = settings->RestAngleX;
				}
				else //phone is calibrated in landscape left
				{
					if (settings->RestAngleX < 0)  //calibrated in landscape left
					{
						g0[0] = settings->RestAngleX;
						g0[1] = settings->RestAngleY;
					}
					else  //calibrated in landscape right
					{
						g0[0] = -settings->RestAngleX;
						g0[1] = -settings->RestAngleY;
					}
				}
			}
			else //current orientation is landscape right, RestAngleX = 45, Y = 0
			{
				if (abs(settings->RestAngleX) < abs(settings->RestAngleY)) //phone is calibrated in portrait, RestAngleX = 0, Y = 45
				{
					g0[0] = settings->RestAngleY;
					g0[1] = -settings->RestAngleX;
				}
				else //phone is calibrated in landscape left
				{
					if (settings->RestAngleX < 0)  //calibrated in landscape left
					{
						g0[0] = -settings->RestAngleX;
						g0[1] = -settings->RestAngleY;
					}
					else  //calibrated in landscape right
					{
						g0[0] = settings->RestAngleX;
						g0[1] = settings->RestAngleY;
					}
				}
			}

		}

	}



	double g[3];

	//NOTE: x is phone's short edge, y is phone's long endge, z is phone's thickness
	if (settings->UseMotionControl == 1 && accl != nullptr)
	{
		Windows::Devices::Sensors::AccelerometerReading^ reading = accl->GetCurrentReading();

		g[0] = reading->AccelerationX;
		g[1] = reading->AccelerationY;
		g[2] = reading->AccelerationZ;

		double theta = SolveForAngle(g0[0], -g0[2], g[0]);



		double phi = SolveForAngle(g0[1], g0[0] * sin(deg2rad(theta)) + g0[2] * cos(deg2rad(theta)), g[1]);

		//account for different orientation

		if (controller->GetOrientation() == ORIENTATION_PORTRAIT)
		{

			if (theta < -settings->MotionDeadzoneH)
				left = true;
			else if (theta > settings->MotionDeadzoneH)
				right = true;


			if (phi < -settings->MotionDeadzoneV)
				up = true;
			else if (phi > settings->MotionDeadzoneV)
				down = true;
		}
		else if (controller->GetOrientation() == ORIENTATION_LANDSCAPE)
		{
			if (theta < -settings->MotionDeadzoneH)
				down = true;
			else if (theta > settings->MotionDeadzoneH)
				up = true;


			if (phi < -settings->MotionDeadzoneV)
				left = true;
			else if (phi > settings->MotionDeadzoneV)
				right = true;

		}
		else
		{
			if (theta < -settings->MotionDeadzoneH)
				up = true;
			else if (theta > settings->MotionDeadzoneH)
				down = true;


			if (phi < -settings->MotionDeadzoneV)
				right = true;
			else if (phi > settings->MotionDeadzoneV)
				left = true;
		}
	}
	else if (settings->UseMotionControl == 2 && incl != nullptr)
	{
		Windows::Devices::Sensors::InclinometerReading^ reading = incl->GetCurrentReading();


		//account for different orientation

		if (controller->GetOrientation() == ORIENTATION_PORTRAIT)
		{
			if (reading->RollDegrees - g0[0] < -settings->MotionDeadzoneH)
				left = true;
			else if (reading->RollDegrees - g0[0] > settings->MotionDeadzoneH)
				right = true;

			if (reading->PitchDegrees - g0[1] < -settings->MotionDeadzoneV)
				up = true;
			else if (reading->PitchDegrees - g0[1] > settings->MotionDeadzoneV)
				down = true;
		}
		else if (controller->GetOrientation() == ORIENTATION_LANDSCAPE)
		{
			if (reading->RollDegrees - g0[0] < -settings->MotionDeadzoneH)
				down = true;
			else if (reading->RollDegrees - g0[0] > settings->MotionDeadzoneH)
				up = true;

			if (reading->PitchDegrees - g0[1] < -settings->MotionDeadzoneV)
				left = true;
			else if (reading->PitchDegrees - g0[1] > settings->MotionDeadzoneV)
				right = true;

		}
		else
		{

			if (reading->RollDegrees - g0[0] < -settings->MotionDeadzoneH)
				up = true;
			else if (reading->RollDegrees - g0[0] > settings->MotionDeadzoneH)
				down = true;

			if (reading->PitchDegrees - g0[1] < -settings->MotionDeadzoneV)
				right = true;
			else if (reading->PitchDegrees - g0[1] > settings->MotionDeadzoneV)
				left = true;
		}


	}

	if (left)
		ret |= 1;
	if (right)
		ret |= 2;
	if (up)
		ret |= 4;
	if (down)
		ret |= 8;

	return ret;

}


EmulatorRenderer::EmulatorRenderer()
{ 
	emulator = EmulatorGame::GetInstance();
	frontbuffer = 0;
	controller = nullptr; 
	autosaving = false;
	elapsedTime = 0.0f;
	settings = EmulatorSettings::Current;
	frames = 0;
	should_show_resume_text = false;

	useButtonColor = !settings->GrayVControllerButtons;

	this->waitEvent = CreateEventEx(NULL, NULL, NULL, EVENT_ALL_ACCESS);
}

EmulatorRenderer::~EmulatorRenderer(void)
{
	if(this->m_d3dContext)
	{
		this->m_d3dContext->Unmap(this->buffers[(this->frontbuffer + 1) % 2].Get(), 0);
	}

	CloseHandle(this->waitEvent);

	delete this->spriteBatch;
	this->spriteBatch = nullptr;

	delete this->commonStates;
	this->commonStates = nullptr;
}

void EmulatorRenderer::CreateDeviceResources()
{
	Renderer::CreateDeviceResources();


	if(this->controller)
	{
		this->controller->UpdateFormat(this->format);
	}

	

	// Map backbuffer so it can be unmapped on first update
	int backbuffer = (this->frontbuffer + 1) % 2;
	this->backbufferPtr = (uint16 *) this->MapBuffer(backbuffer, &this->pitch);

	
}



void EmulatorRenderer::UpdateForWindowSizeChange(float width, float height)
{
	Direct3DBase::UpdateForWindowSizeChange(width, height);

	float scale = ((int)DisplayProperties::ResolutionScale) / 100.0f;
	this->height = width * scale;
	this->width = height * scale;

	if(this->height > 700.0f && this->height < 740.0f)
	{
		this->format = HD720P;
	}else if(this->height > 760.0f)
	{
		this->format = WXGA;
	}else 
	{
		this->format = WVGA;
	}

	if(this->controller)
	{
		this->controller->UpdateFormat(this->format);
	}

	this->CreateTransformMatrix();
}

void EmulatorRenderer::SetVirtualController(VirtualController *controller)
{
	this->controller = controller;
	this->controller->UpdateFormat(this->format);
	this->controller->SetOrientation(this->orientation);
	this->controller->pressCount = 0;
	this->controller->errCount = 0;
}

void EmulatorRenderer::GetBackbufferData(uint16 **backbufferPtr, size_t *pitch, int *imageWidth, int *imageHeight)
{
	*backbufferPtr = this->backbufferPtr;
	*pitch = this->pitch;
	*imageWidth = EmulatorGame::SnesImageWidth;
	*imageHeight = EmulatorGame::SnesImageHeight;
}

void EmulatorRenderer::ChangeOrientation(int orientation)
{
	this->orientation = orientation;
	if(this->controller)
	{
		this->controller->SetOrientation(this->orientation);
	}
	this->CreateTransformMatrix();
}



void EmulatorRenderer::Update(float timeTotal, float timeDelta)
{
	lastElapsed = timeDelta;

	if(!emulator->IsPaused())
	{
		this->elapsedTime += timeDelta;
	}
	/*if(!settings->LowFrequencyModeMeasured)
	{
		if(this->elapsedTime >= 3.0f)
		{
			if(this->frames < 100)
			{
				settings->LowFrequencyMode = true;
			}else
			{
				settings->LowFrequencyMode = false;
			}
			settings->LowFrequencyModeMeasured = true;
		}
	}*/
	if(this->elapsedTime >= AUTOSAVE_INTERVAL)
	{
		if(!emulator->IsPaused())
		{
			this->elapsedTime -= AUTOSAVE_INTERVAL;
			autosaving = true;
			SaveSRAMAsync().then([this]()
			{
				int oldSlot = SavestateSlot;
				SavestateSlot = AUTOSAVE_SLOT;
				SaveStateAsync().wait();
				SavestateSlot = oldSlot;
				//Settings.Mute = !EmulatorSettings::Current->SoundEnabled;
				emulator->Unpause();
				SetEvent(this->waitEvent);
			});
		}else
		{
			this->elapsedTime = AUTOSAVE_INTERVAL;
		}
	}

	bool left = false;
	bool right = false;
	bool up = false;
	bool down = false;
	bool start = false;
	bool select = false;
	bool a = false;
	bool b = false;
	bool x = false;
	bool y = false;
	bool l = false;
	bool r = false;
	turbo = false;
		
	u8 motionInput = getMotionInput();

	if (motionInput & 1)
		GetMotionMapping(settings->MotionLeft, &left, &right, &up, &down, &a, &b, &x, &y, &l, &r);
	else if (motionInput & 2)
		GetMotionMapping(settings->MotionRight, &left, &right, &up, &down, &a, &b, &x, &y, &l, &r);

	if (motionInput & 4)
		GetMotionMapping(settings->MotionUp, &left, &right, &up, &down, &a, &b, &x, &y, &l, &r);
	else if (motionInput & 8)
		GetMotionMapping(settings->MotionDown, &left, &right, &up, &down, &a, &b, &x, &y, &l, &r);

	//Moga
	using namespace Moga::Windows::Phone;
	Moga::Windows::Phone::ControllerManager^ ctrl = Direct3DBackground::getController();
	if(EmulatorSettings::Current->UseMogaController && ctrl != nullptr && ctrl->GetState(Moga::Windows::Phone::ControllerState::Connection) == ControllerResult::Connected)
	{
		//pro only, the pocket version has the left joy stick output to both Axis and directional Keycode.
		if (ctrl->GetState(Moga::Windows::Phone::ControllerState::SelectedVersion) != ControllerResult::VersionMoga ) 
		{
			float axis_x = ctrl->GetAxisValue(Axis::X);
			float axis_y = ctrl->GetAxisValue(Axis::Y);

			float angle = 0;
			if (axis_x == 0)
			{
				if (axis_y > 0)
					angle = 1.571f;
				else if (axis_y < 0)
					angle = 4.712f;
				else
					angle = 1000.0f; //non existent value
			}
			else if (axis_x > 0)
			{
				angle = atan(axis_y / axis_x);

				if (angle < 0)
					angle += 6.283f;
			}
			else if (axis_x <0)
			{
				angle = atan(axis_y / axis_x) +  3.142f;
			}

			//convert to degree
			angle = angle / 3.142f * 180.0f;
		
			if ( angle <= 22.5f)
			{
				right = true;
			}
			else if ( angle <= 67.5f)
			{
				right = true;
				up = true;
			}
			else if (angle <= 112.5f)
			{
				up = true;
			}
			else if (  angle <= 157.5f)
			{
				up = true;
				left = true;
			}
			else if (angle <= 202.5f)
			{
				left = true;
			}
			else if (angle <= 247.5f)
			{
				left = true;
				down = true;
			}
			else if (angle <= 292.5f)
			{
				down = true;
			}
			else if (angle <= 337.5f)
			{
				down = true;
				right = true;
			}
			else if (angle <= 360.0f)
			{
				right = true;
			}
		}

		
		if(ctrl->GetKeyCode(KeyCode::Start) == ControllerAction::Pressed)
		{
			start = true;
		}

		if(ctrl->GetKeyCode(KeyCode::Select) == ControllerAction::Pressed)
		{
			select = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionLeft) == ControllerAction::Pressed)
		{
			left = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionRight) == ControllerAction::Pressed)
		{
			right = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionUp) == ControllerAction::Pressed)
		{
			up = true;
		}

		if(ctrl->GetKeyCode(KeyCode::DirectionDown) == ControllerAction::Pressed)
		{
			down = true;
		}

		if(ctrl->GetKeyCode(KeyCode::A) == ControllerAction::Pressed )
		{
			GetMogaMapping(settings->MogaA, &a, &b, &x, &y, &l, &r );
		}

		
		if(ctrl->GetKeyCode(KeyCode::B) == ControllerAction::Pressed)
			GetMogaMapping(settings->MogaB, &a, &b, &x, &y, &l, &r );


		if (ctrl->GetKeyCode(KeyCode::X) == ControllerAction::Pressed)
			GetMogaMapping(settings->MogaX, &a, &b, &x, &y, &l, &r );


		if(ctrl->GetKeyCode(KeyCode::Y) == ControllerAction::Pressed)
			GetMogaMapping(settings->MogaY, &a, &b, &x, &y, &l, &r );

		if(ctrl->GetKeyCode(KeyCode::L1) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaL1, &a, &b, &x, &y, &l, &r );

		if (abs(ctrl->GetAxisValue(Axis::LeftTrigger)) > 0.5f)
			GetMogaMapping(settings->MogaL2, &a, &b, &x, &y, &l, &r );


		if(ctrl->GetKeyCode(KeyCode::R1) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaR1, &a, &b, &x, &y, &l, &r );


		if (abs(ctrl->GetAxisValue(Axis::RightTrigger)) > 0.5f)
			GetMogaMapping(settings->MogaR2, &a, &b, &x, &y, &l, &r );

		if(ctrl->GetKeyCode(KeyCode::ThumbLeft) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaLeftJoystick, &a, &b, &x, &y, &l, &r );

		if(ctrl->GetKeyCode(KeyCode::ThumbRight) == ControllerAction::Pressed )
			GetMogaMapping(settings->MogaRightJoystick, &a, &b, &x, &y, &l, &r );

	}


	if(this->controller)
	{
		const Emulator::ControllerState *state = this->controller->GetControllerState();

		left |= state->LeftPressed;
		right |= state->RightPressed;
		up |= state->UpPressed;
		down |= state->DownPressed;
		start |= state->StartPressed;
		select |= state->SelectPressed;
		


		if(settings->CameraButtonAssignment == 0) //camera is turbo
		{
			l |= state->LPressed;
			r |= state->RPressed;
			a |= state->APressed;
			b |= state->BPressed;
			x |= state->XPressed;
			y |= state->YPressed;
			//note: turbo is set in Emulator.cpp/UpdateAsync
		}
		else if(settings->CameraButtonAssignment == 1) //camera is R button
		{
			l |= state->LPressed;

			a |= state->APressed;
			b |= state->BPressed;
			x |= state->XPressed;
			y |= state->YPressed;

			turbo |= state->RPressed;

			if (cameraPressed) //this is true when camera button is pressed
			{
				if (settings->EnableAutoFire)
				{
					if (autoFireToggle)
						r = true;
					autoFireToggle = !autoFireToggle;
				}
				else //no autofire, just keep pressing the button
				{
					r = true;
				}

			}
		}
		else if(settings->CameraButtonAssignment == 2) //camera is L buttion
		{

			r |= state->RPressed;
			a |= state->APressed;
			b |= state->BPressed;
			x |= state->XPressed;
			y |= state->YPressed;

			turbo |= state->LPressed;

			if (cameraPressed) //this is true when camera button is pressed
			{
				if (settings->EnableAutoFire)
				{
					if (autoFireToggle)
						l = true;
					autoFireToggle = !autoFireToggle;
				}
				else //no autofire, just keep pressing the button
				{
					l = true;
				}

			}
		}
		else if (settings->CameraButtonAssignment == 3) //camera is A buttion
		{
			l |= state->LPressed;
			r |= state->RPressed;

			b |= state->BPressed;
			x |= state->XPressed;
			y |= state->YPressed;

			turbo |= state->APressed;

			if (cameraPressed) //this is true when camera button is pressed
			{
				if (settings->EnableAutoFire)
				{
					if (autoFireToggle)
						a = true;
					autoFireToggle = !autoFireToggle;
				}
				else //no autofire, just keep pressing the button
				{
					a = true;
				}

			}
		}
		else if (settings->CameraButtonAssignment == 4) //camera is B buttion
		{
			l |= state->LPressed;
			r |= state->RPressed;
			a |= state->APressed;

			x |= state->XPressed;
			y |= state->YPressed;

			turbo |= state->BPressed;

			if (cameraPressed) //this is true when camera button is pressed
			{
				if (settings->EnableAutoFire)
				{
					if (autoFireToggle)
						b = true;
					autoFireToggle = !autoFireToggle;
				}
				else //no autofire, just keep pressing the button
				{
					b = true;
				}

			}
		}
		else if (settings->CameraButtonAssignment == 5) //camera is X buttion
		{
			l |= state->LPressed;
			r |= state->RPressed;
			a |= state->APressed;
			b |= state->BPressed;

			y |= state->YPressed;

			turbo |= state->XPressed;

			if (cameraPressed) //this is true when camera button is pressed
			{
				if (settings->EnableAutoFire)
				{
					if (autoFireToggle)
						x = true;
					autoFireToggle = !autoFireToggle;
				}
				else //no autofire, just keep pressing the button
				{
					x = true;
				}

			}
		}
		else if (settings->CameraButtonAssignment == 6) //camera is Y buttion
		{
			l |= state->LPressed;
			r |= state->RPressed;
			a |= state->APressed;
			b |= state->BPressed;
			x |= state->XPressed;

			turbo |= state->YPressed;

			if (cameraPressed) //this is true when camera button is pressed
			{
				if (settings->EnableAutoFire)
				{
					if (autoFireToggle)
						y = true;
					autoFireToggle = !autoFireToggle;
				}
				else //no autofire, just keep pressing the button
				{
					y = true;
				}

			}
		}
	}








	this->emulator->SetButtonState(JOYPAD_LEFT, left);
	this->emulator->SetButtonState(JOYPAD_UP, up);
	this->emulator->SetButtonState(JOYPAD_RIGHT, right);
	this->emulator->SetButtonState(JOYPAD_DOWN, down);
	this->emulator->SetButtonState(JOYPAD_START, start);
	this->emulator->SetButtonState(JOYPAD_SELECT, select);
	this->emulator->SetButtonState(JOYPAD_A, a);
	this->emulator->SetButtonState(JOYPAD_B, b);
	this->emulator->SetButtonState(JOYPAD_X, x);
	this->emulator->SetButtonState(JOYPAD_Y, y);
	this->emulator->SetButtonState(JOYPAD_L, l);
	this->emulator->SetButtonState(JOYPAD_R, r);


	//set render parameter
	float opacity = 1.0f;
	if(this->orientation != ORIENTATION_PORTRAIT || !this->useButtonColor) //only opacity in landscape or when using simple button
		opacity = this->settings->ControllerOpacity / 100.0f;


	XMFLOAT4A color = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity);
	XMFLOAT4A color2 = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity + 0.2f);
	joystick_color = color;
	joystick_center_color = color2;
	l_color = color;
	r_color = color;
	select_color = color;
	start_color = color;
	a_color = color;
	b_color = color;
	x_color = color;
	y_color = color;

	float text_opacity = (sinf(timeTotal*2) + 1.0f) / 2.0f;
	resume_text_color = XMFLOAT4A(1.0f, 0.0f, 0.0f, text_opacity);

	if(settings->DPadStyle == 0 || settings->DPadStyle == 1)
		pad_to_draw = 0;
	else if(settings->DPadStyle == 2 || (settings->DPadStyle == 3 && this->controller->StickFingerDown()))
		pad_to_draw = 1;
	else
		pad_to_draw = -1;
}


void EmulatorRenderer::GetMogaMapping(int pressedButton, bool* a, bool* b, bool* x, bool* y, bool* l, bool* r )
{
	if (pressedButton & 1)
		*a = true;
	if (pressedButton & 2)
		*b = true;
	if (pressedButton & 4)
		*x = true;
	if (pressedButton & 8)
		*y = true;
	if (pressedButton & 16) 
		*l = true;
	if (pressedButton & 32)
		*r = true;
	
}

void EmulatorRenderer::GetMotionMapping(int tiltDirection, bool* left, bool* right, bool* up, bool* down, bool* a, bool* b, bool* x, bool* y, bool* l, bool* r)
{
	if (tiltDirection & 1)
		*left = true;
	if (tiltDirection & 2)
		*right = true;
	if (tiltDirection & 4)
		*up = true;
	if (tiltDirection & 8)
		*down = true;
	if (tiltDirection & 16)
		*a = true;
	if (tiltDirection & 32)
		*b = true;
	if (tiltDirection & 64)
		*x = true;
	if (tiltDirection & 128)
		*y = true;
	if (tiltDirection & 256)
		*l = true;
	if (tiltDirection & 512)
		*r = true;

}

void EmulatorRenderer::Render()
{
	if(autosaving)
	{
		WaitForSingleObjectEx(this->waitEvent, INFINITE, false);
		autosaving = false;
	}

	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);


	float bgcolor[] = { 0.0f, 0.0f, 0.0f, 1.000f }; //black
	if (this->useButtonColor && this->orientation == ORIENTATION_PORTRAIT)
	{
		bgcolor[0] = (float)this->settings->BgcolorR / 255;
		bgcolor[1] = (float)this->settings->BgcolorG / 255;
		bgcolor[2] = (float)this->settings->BgcolorB / 255;
	}


	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		bgcolor
		);

	m_d3dContext->ClearDepthStencilView(
		m_depthStencilView.Get(),
		D3D11_CLEAR_DEPTH,
		1.0f,
		0
		);


	if(!this->emulator->IsPaused())
	{
		if(framesNotRendered >= settings->PowerFrameSkip)
		{
			framesNotRendered = 0;

			if(!this->emulator->LastFrameSkipped())
			{
				// Swap buffers for background emulation
				int backbuffer = this->frontbuffer;
				this->frontbuffer = (this->frontbuffer + 1) % 2;
				uint16 *buffer = (uint16 *) this->MapBuffer(backbuffer, &this->pitch);
				this->backbufferPtr = buffer;
				//memset (buffer, 0, EXT_PITCH * EXT_HEIGHT);

				this->emulator->Update((void *) buffer, this->pitch, lastElapsed);
				this->m_d3dContext->Unmap(this->buffers[this->frontbuffer].Get(), 0);
			}else
			{
				this->emulator->Update((void *) this->backbufferPtr, this->pitch, lastElapsed);
			}
		}else
		{
			framesNotRendered++;
		}
	}

	int height, width;
	RECT rect;

	if(this->orientation != ORIENTATION_PORTRAIT)
	{
		height = this->height;
		if(settings->FullscreenStretch)
		{
			width = this->width;
			rect.left = 0;
			rect.right = width;
			rect.top = 0;
			rect.bottom = height;
		}else
		{
			height *= (this->settings->ImageScaling / 100.0f);
			width = (int)(height * ((float) SNES_WIDTH) / (float) SNES_HEIGHT);
			int leftOffset = (this->width - width) / 2;
			rect.left = leftOffset;
			rect.right = width + leftOffset;
			rect.top = 0;
			rect.bottom = height;
		}
	}else
	{
		width = this->height;
		height = (int)(width * ((float) SNES_HEIGHT / (float) SNES_WIDTH));
		rect.left = 0;
		rect.right = width;
		rect.top = 0;
		rect.bottom = height;
	}

	RECT source;
	source.left = 0;
	source.right = EmulatorGame::SnesImageWidth;
	source.top = 0;
	source.bottom = EmulatorGame::SnesImageHeight;
			
	this->controller->GetARectangle(&aRectangle);
	this->controller->GetBRectangle(&bRectangle);
	this->controller->GetXRectangle(&xRectangle);
	this->controller->GetYRectangle(&yRectangle);
	this->controller->GetCrossRectangle(&crossRectangle);
	this->controller->GetStartRectangle(&startRectangle);
	this->controller->GetSelectRectangle(&selectRectangle);
	this->controller->GetLRectangle(&lRectangle);
	this->controller->GetRRectangle(&rRectangle);
	this->controller->GetStickRectangle(&stickRect);
	this->controller->GetStickCenterRectangle(&centerRect);

	float opacity = this->settings->ControllerOpacity / 100.0f;
	XMFLOAT4A colorf = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity);
	XMFLOAT4A colorf2 = XMFLOAT4A(1.0f, 1.0f, 1.0f, opacity + 0.2f);
	if(this->orientation == ORIENTATION_PORTRAIT)
	{
		colorf.w = 0.3f + 0.7f * opacity;
	}
	XMVECTOR colorv = XMLoadFloat4A(&colorf);
	XMVECTOR colorv2 = XMLoadFloat4A(&colorf2);
	
	// Render last frame to screen
	this->spriteBatch->Begin(
		DirectX::SpriteSortMode::SpriteSortMode_Deferred, 
		this->alphablend.Get(), nullptr, nullptr, nullptr, nullptr, 
		this->outputTransform);
	this->spriteBatch->Draw(this->bufferSRVs[this->frontbuffer].Get(), rect, &source);

	//display resume text if paused
	if(should_show_resume_text)
	{
		int textWidth = 0.5*width;
		int textHeight = textWidth / 480.0f * 80.0f;

		RECT resume_text_rect = {(width - textWidth) / 2, (height - textHeight) / 2, (width + textWidth) / 2, (height + textHeight) / 2};

		this->spriteBatch->Draw(this->resumeTextSRV.Get(), resume_text_rect, nullptr, XMLoadFloat4A(&resume_text_color));


	}

	//===draw virtual controller if moga controller is not loaded
	using namespace Moga::Windows::Phone;
	Moga::Windows::Phone::ControllerManager^ ctrl = Direct3DBackground::getController();
	if (!(EmulatorSettings::Current->UseMogaController && ctrl != nullptr && ctrl->GetState(Moga::Windows::Phone::ControllerState::Connection) == ControllerResult::Connected))
	{
		this->DrawController();
	}

	this->spriteBatch->End();

	frames++;
}

void *EmulatorRenderer::MapBuffer(int index, size_t *rowPitch)
{
	D3D11_MAPPED_SUBRESOURCE map;
	ZeroMemory(&map, sizeof(D3D11_MAPPED_SUBRESOURCE));

	DX::ThrowIfFailed(
		this->m_d3dContext->Map(this->buffers[index].Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &map)
		);

	*rowPitch = map.RowPitch;
	return map.pData;
}

