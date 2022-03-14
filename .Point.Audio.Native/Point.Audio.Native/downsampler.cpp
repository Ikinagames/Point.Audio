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

#include <stdlib.h>

#include "pch.h"
#include "downsampler.h"
#include "fmod.hpp"
#include "fmod_dsp.h"
#include "fmod_studio.hpp"

static FMOD_DSP_PARAMETER_DESC p_downsample_count;
static FMOD_DSP_PARAMETER_DESC p_downsample_gain;

enum
{
	DSP_PARAM_SAMPLECOUNT = 0,
	DSP_PARAM_GAIN,

	DSP_PARAM_NUM_PARAMTERS
};

#pragma region Downsampler Class

	class Downsampler
	{
	public:
		Downsampler();
		~Downsampler();

		void reset();

		int getSampleCount();
		void setSampleCount(int);

		float getGain();
		void setGain(float);

		void process(float* inbuffer, float* outbuffer, unsigned int length, int channels);

	private:
		int current_sampleCount;

		float m_target_gain;
		float m_current_gain;

		int m_ramp_samples_left;

		void gainProcess(float* inbuffer, float* outbuffer, unsigned int length, int channels);
	};

	Downsampler::Downsampler()
	{
	}
	Downsampler::~Downsampler()
	{
	}

	void Downsampler::reset() {
		m_current_gain = m_target_gain;
		m_ramp_samples_left = 0;
	}

	int Downsampler::getSampleCount() {
		return current_sampleCount;
	}
	void Downsampler::setSampleCount(int count) {
		current_sampleCount = count;
	}

	float Downsampler::getGain() {
		return m_target_gain;
	}
	void Downsampler::setGain(float level) {
		m_target_gain = DECIBELS_TO_LINEAR(level);
		m_ramp_samples_left = FMOD_NOISE_RAMPCOUNT;
	}

	void Downsampler::gainProcess(float* inbuffer, float* outbuffer, unsigned int length, int channels) {
		
		float gain = m_current_gain;

		if (m_ramp_samples_left) {
			float target = m_target_gain;
			float delta = (target - gain) / m_ramp_samples_left;
			while (length)
			{
				if (--m_ramp_samples_left) {
					
					gain += delta;
					for (int i = 0; i < channels; i++)
					{
						*outbuffer++ = *inbuffer++ * gain;
					}
				}
				else {
					gain = target;
					break;
				}
				--length;
			}
		}

		unsigned int samples = length * channels;
		while (samples--)
		{
			*outbuffer++ = *inbuffer++ * gain;
		}

		m_current_gain = gain;
	}
	void Downsampler::process(float* inbuffer, float* outbuffer, unsigned int length, int channels) {

		gainProcess(inbuffer, outbuffer, length, channels);

		unsigned int samples = length * channels;
		for (int i = 0; i < samples; i += current_sampleCount)
		{
			float norm = inbuffer[i];
			for (int j = 1; j < current_sampleCount; j++)
			{
				outbuffer[i + j] = norm;
			}
		}
	}

#pragma endregion

FMOD_DSP_PARAMETER_DESC* ParameterList[DSP_PARAM_NUM_PARAMTERS] = {
	&p_downsample_count,
	&p_downsample_gain,
};
FMOD_DSP_DESCRIPTION Point_Downsampler_Desc = {
	FMOD_PLUGIN_SDK_VERSION,
	"Point Downsampler",		//	name
	0x00010000,					//	plug-in version
	1,							//	number of input buffers to process
	1,							//	number of output buffers to process
	DSP_CREATE_CALLBACK,		//	create callback
	DSP_RELEASE_CALLBACK,		//	release callback
	DSP_RESET_CALLBACK,			//
	0/*DSP_READ_CALLBACK*/,			//
	DSP_PROCESS_CALLBACK,		//
	0/*DSP_SETPOSITION_CALLBACK*/,	//

	DSP_PARAM_NUM_PARAMTERS,	//	number of parameters
	ParameterList,				//
	DSP_SETPARAM_FLOAT_CALLBACK,
	DSP_SETPARAM_INT_CALLBACK,
	0,
	0,
	DSP_GETPARAM_FLOAT_CALLBACK,
	DSP_GETPARAM_INT_CALLBACK,
	0,
	0
};

FMOD_DSP_DESCRIPTION* get_downsampler() {
	FMOD_DSP_INIT_PARAMDESC_INT(
		p_downsample_count, "Sample Count", " Count", "Count for downsampling. 1 to 32. Default = 4", 
		1, 32, 4, false, 0);
	FMOD_DSP_INIT_PARAMDESC_FLOAT(
		p_downsample_gain, "Gain", "dB", "Gain in dB. -80 to 10. Default = 0",
		GAIN_MIN, GAIN_MAX, 0
		);

	return &Point_Downsampler_Desc;
}

#pragma region Callbacks

	FMOD_RESULT F_CALL DSP_CREATE_CALLBACK(FMOD_DSP_STATE* dsp_state)
	{
		dsp_state->plugindata = (Downsampler*)FMOD_DSP_ALLOC(dsp_state, sizeof(Downsampler));
		if (!dsp_state->plugindata) {
			return FMOD_ERR_MEMORY;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_RELEASE_CALLBACK(FMOD_DSP_STATE* dsp_state)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;
		FMOD_DSP_FREE(dsp_state, state);
		return FMOD_OK;
	}

	FMOD_RESULT F_CALL DSP_RESET_CALLBACK(FMOD_DSP_STATE* dsp_state)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		state->reset();

		return FMOD_OK;
	}

	FMOD_RESULT F_CALL DSP_READ_CALLBACK(FMOD_DSP_STATE* dsp_state, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int* outchannels)
	{
		return FMOD_OK;
	}

	FMOD_RESULT F_CALL DSP_PROCESS_CALLBACK(FMOD_DSP_STATE* dsp_state, unsigned int length, const FMOD_DSP_BUFFER_ARRAY* inbufferarray, FMOD_DSP_BUFFER_ARRAY* outbufferarray, FMOD_BOOL inputsidle, FMOD_DSP_PROCESS_OPERATION op)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		if (op == FMOD_DSP_PROCESS_QUERY) {

			return FMOD_OK;
		}

		state->process(inbufferarray->buffers[0], outbufferarray->buffers[0], length, outbufferarray->buffernumchannels[0]);

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_SETPOSITION_CALLBACK(FMOD_DSP_STATE* dsp_state, unsigned int pos)
	{
		return FMOD_OK;
	}

	/*																									*/

	FMOD_RESULT F_CALL DSP_SETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float value)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_GAIN:

			state->setGain(value);
			break;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_GETPARAM_FLOAT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, float* value, char* valuestr)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_GAIN:
			*value = state->getGain();
			if (valuestr) {
				sprintf(valuestr, "%.1f dB", state->getGain());
			}

			break;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_SETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int value)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_SAMPLECOUNT:

			state->setSampleCount(value);
			break;
		}

		return FMOD_OK;
	}
	FMOD_RESULT F_CALL DSP_GETPARAM_INT_CALLBACK(FMOD_DSP_STATE* dsp_state, int index, int* value, char* valuestr)
	{
		Downsampler* state = (Downsampler*)dsp_state->plugindata;

		switch (index)
		{
		case DSP_PARAM_SAMPLECOUNT:

			*value = state->getSampleCount();
			//if (valuestr) sprintf(valuestr, "%s", state->getSampleCount());

			break;
		}

		return FMOD_OK;
	}

#pragma endregion