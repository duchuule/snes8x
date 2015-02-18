#pragma once


#include "Renderer.h"


using namespace Emulator;
using namespace DirectX;
using namespace concurrency;
using namespace Windows::Storage;
using namespace Windows::UI::Input;
using namespace Microsoft::WRL;
using namespace PhoneDirect3DXamlAppComponent;

// This class renders a simple spinning cube.
ref class EmulatorRenderer sealed : public Renderer
{
public:
	EmulatorRenderer();
	virtual ~EmulatorRenderer(void);

	// Direct3DBase methods.
	virtual void CreateDeviceResources() override;
	virtual void Render() override;
	virtual void UpdateForWindowSizeChange(float width, float height) override;
	
	// Method for updating time-dependent objects.
	virtual void Update(float timeTotal, float timeDelta) override;

	void ChangeOrientation(int orientation);

internal:
	void SetVirtualController(VirtualController *controller);
	void GetBackbufferData(uint16 **backbufferPtr, size_t *pitch, int *imageWidth, int *imageHeight);

private:


	VirtualController *controller;

	void *MapBuffer(int index, size_t *rowPitch);
	void GetMogaMapping(int pressedButton, bool* a, bool* b, bool* x, bool* y, bool* l, bool* r );
	void GetMotionMapping(int tiltDirection, bool* left, bool* right, bool* up, bool* down, bool* a, bool* b, bool* x, bool* y, bool* l, bool* r);
};
