#include "pch.h"
#include "EmulatorSettings.h"

namespace PhoneDirect3DXamlAppComponent
{
	EmulatorSettings ^EmulatorSettings::instance;

	EmulatorSettings::EmulatorSettings()
	{ 
		this->SettingsChanged = nullptr;
	}
}