#pragma once

#include "renderer.h"
#include "CPositionVirtualController.h"



// This class renders a simple spinning cube.
ref class CPositionRenderer sealed : public Renderer
{
public:
	CPositionRenderer();
	virtual ~CPositionRenderer(void);

	// Direct3DBase methods.
	virtual void CreateDeviceResources() override;
	virtual void Render() override;
	virtual void UpdateForWindowSizeChange(float width, float height) override;
	
	
	// Method for updating time-dependent objects.
	virtual void Update(float timeTotal, float timeDelta) override;

	void UpdateController(void);
	void ChangeOrientation(int orientation);

internal:
	void SetVirtualController(Emulator::CPositionVirtualController *controller);


private:
	Emulator::CPositionVirtualController *controller;
	const Emulator::ControllerState *cstate;

};
