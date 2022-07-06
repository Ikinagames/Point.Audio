// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// pch.cpp: source file corresponding to the pre-compiled header

#include <stdlib.h>
#include <math.h>

#include "pch.h"
#include "fmod.hpp"
#include "fmod_common.h"

#include "downsampler.h"
#include "fmod_gain.h"

// http://ffmpeg.org/
// https://www.openal.org/
// http://audiere.sourceforge.net/
// https://github.com/micknoise/Maximilian

static FMOD_PLUGINLIST Plugin_List[] = {
	{ FMOD_PLUGINTYPE_DSP, get_downsampler() },
	{ FMOD_PLUGINTYPE_DSP, get_doubler() },
	//{ FMOD_PLUGINTYPE_DSP, },
};

DLLEXPORT FMOD_PLUGINLIST* F_CALL FMODGetPluginDescriptionList() {
	return Plugin_List;
}

//void Calculate(int blocksize) {
//	const double forthSamplerateSec = 2.2675736961451248e-05;
//	return forthSamplerateSec * blocksize;
//}