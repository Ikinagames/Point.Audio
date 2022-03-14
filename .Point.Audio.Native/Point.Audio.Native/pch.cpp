// pch.cpp: source file corresponding to the pre-compiled header

#include <stdlib.h>

#include "pch.h"
#include "fmod.hpp"
#include "downsampler.h"



static FMOD_PLUGINLIST Plugin_List[1] = {
	{ FMOD_PLUGINTYPE_DSP, get_downsampler() },
};

DLLEXPORT FMOD_PLUGINLIST* F_CALL FMODGetPluginDescriptionList() {
	return Plugin_List;
}