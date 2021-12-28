// Copyright 2021 Ikina Games
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using UnityEngine;
using Point.Collections;
using Unity.Collections;
using System.Linq;
using Unity.Jobs;
using Point.Audio.LowLevel;
using FMOD.Studio;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.SceneManagement;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class FMODManager : StaticMonobehaviour<FMODManager>, IStaticInitializer
    {
        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

        internal static FMOD.Studio.System StudioSystem => FMODUnity.RuntimeManager.StudioSystem;
        internal static FMOD.System CoreSystem => FMODUnity.RuntimeManager.CoreSystem;

        private bool m_IsFocusing;

        private JobHandle m_GlobalJobHandle;

        private AudioHandlerContainer m_Handlers;

        #region Class Instruction

        protected override void OnInitialze()
        {
            m_IsFocusing = true;

            unsafe
            {
                m_Handlers = new AudioHandlerContainer(128);
            }

            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            AudioList audioList = AudioList.Instance;
            for (int i = 0; i < audioList.m_OnSceneLoadedParams.Length; i++)
            {
                if (!audioList.m_OnSceneLoadedParams[i].TargetSceneName.Equals(arg0.name)) continue;

                SetGlobalParameter(audioList.m_OnSceneLoadedParams[i].GetParamReference());
            }
        }

        protected override void OnShutdown()
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

            m_Handlers.Dispose();
        }

        private void Start()
        {
            AudioList audioList = AudioList.Instance;
            for (int i = 0; i < audioList.m_StartOnPlay.Length; i++)
            {
                Audio audio = audioList.m_StartOnPlay[i].GetAudio(audioList);
                Play(ref audio);
            }

            Scene currentScene = SceneManager.GetActiveScene();
            for (int i = 0; i < audioList.m_OnSceneLoadedParams.Length; i++)
            {
                if (!audioList.m_OnSceneLoadedParams[i].TargetSceneName.Equals(currentScene.name)) continue;

                SetGlobalParameter(audioList.m_OnSceneLoadedParams[i].GetParamReference());
            }
        }

        #endregion

        #region Updates

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused) return;
#endif
            m_GlobalJobHandle.Complete();

            JobHandle handlerJob = m_Handlers.ScheduleUpdate();
            m_GlobalJobHandle = JobHandle.CombineDependencies(m_GlobalJobHandle, handlerJob);
        }

        private void OnApplicationFocus(bool focus)
        {
            m_IsFocusing = focus;
        }

        #endregion

        public static ParamReference GetGlobalParameter(string name)
        {
            var result = StudioSystem.getParameterDescriptionByName(name, out var description);
            if (result != FMOD.RESULT.OK)
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"Parameter({name}) is not present in the current FMOD.");

                return default(ParamReference);
            }

            StudioSystem.getParameterByID(description.id, out float value);
            var param = new ParamReference(name, value);

            return param;
        }
        public static void SetGlobalParameter(string name, float value)
        {
            var result = StudioSystem.getParameterDescriptionByName(name.ToString(), out var description);
            if (result != FMOD.RESULT.OK)
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"Parameter({name}) is not present in the current FMOD.");

                return;
            }

            StudioSystem.setParameterByID(description.id, value);

            Collections.Point.Log(Collections.Point.LogChannel.Audio,
                $"Global parameter({name}) has set to {value}.");
        }
        public static void SetGlobalParameter(ParamReference parameter)
        {
            var result = StudioSystem.setParameterByID(parameter.description.id, parameter.value);
            if (result != FMOD.RESULT.OK)
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"Parameter({(string)parameter.description.name}) is not present in the current FMOD.");
            }

            Collections.Point.Log(Collections.Point.LogChannel.Audio,
                $"Global parameter({(string)parameter.description.name}) has set to {parameter.value}.");
        }

        public static bool IsBankLoaded(string name) => FMODUnity.RuntimeManager.HasBankLoaded(name);
        public static bool LoadBank(string name, bool loadSamples = false)
        {
            try
            {
                FMODUnity.RuntimeManager.LoadBank(name, loadSamples);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }

            FMODUnity.RuntimeManager.WaitForAllSampleLoading();
            return true;
        }
        public static void UnloadBank(string name)
        {
            FMODUnity.RuntimeManager.UnloadBank(name);
        }
        public static FMOD.Studio.Bank GetBank(string path)
        {
            StudioSystem.getBank(path, out var bank);

            return bank;
        }

        // https://www.fmod.com/resources/documentation-api?version=2.02&page=studio-guide.html#dialogue-and-localization
        // https://www.fmod.com/resources/documentation-api?version=2.02&page=studio-api-eventinstance.html#fmod_studio_event_callback_create_programmer_sound
        //public static void Test(string key)
        //{
        //    StudioSystem.getSoundInfo(key, out var info);
        //    EventInstance asd;
        //    //asd.setCallback();
        //}

        public static Audio GetAudio(in FMODUnity.EventReference eventRef)
        {
            var result = StudioSystem.getEventByID(eventRef.Guid, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                throw new System.Exception();
            }
#endif
            Audio audio = new Audio(ev);

            return audio;
        }
        public static void GetAudio(in FMODUnity.EventReference eventRef, ref Audio audio)
        {
            var result = StudioSystem.getEventByID(eventRef.Guid, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                    $"Error has been raised while retriving event({eventRef.Path}) {result}.");
                return;
            }
#endif
            audio.SetEvent(ev);
        }
        public static Audio GetAudio(in string eventPath)
        {
            var result = StudioSystem.getEvent(eventPath, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                throw new System.Exception();
            }
#endif
            Audio audio = new Audio(ev);

            return audio;
        }

        public static FMOD.Studio.Bus GetBusByID(FMOD.GUID id)
        {
            if (StudioSystem.getBusByID(id, out var bus) != FMOD.RESULT.OK)
            {
                return default(FMOD.Studio.Bus);
            }

            return bus;
        }
        public static FMOD.Studio.VCA GetVCA(FMOD.GUID id)
        {
            if (StudioSystem.getVCAByID(id, out FMOD.Studio.VCA vca) != FMOD.RESULT.OK)
            {
                return default(FMOD.Studio.VCA);
            }

            return vca;
        }

        public static void Play(ref Audio audio)
        {
#if DEBUG_MODE
            if (!audio.IsValidID())
            {
                throw new System.Exception();
            }
#endif
            unsafe
            {
                AudioHandler* handler = Instance.m_Handlers.GetUnusedHandler();
                {
                    handler->translation = audio._translation;
                    handler->rotation = audio._rotation;
                }
                audio.eventDescription.createInstance(out handler->instance);
                handler->instance.set3DAttributes(handler->Get3DAttributes());

                audio.audioHandler = handler;

                //string paramsString = string.Empty;
                for (int i = 0; i < audio.parameters.Length; i++)
                {
                    var result = handler->instance.setParameterByID(
                        audio.parameters[i].description.id,
                        audio.parameters[i].value,
                        audio.parameters[i].ignoreSeekSpeed);

                    if (result != FMOD.RESULT.OK)
                    {
                        Collections.Point.LogError(Collections.Point.LogChannel.Audio,
                            $"Parameter({(string)audio.parameters[i].description.name}) set failed with {result}");
                    }

                    //paramsString += audio.parameters[i].ToString() + " ";
                }

                if (audio.Is3D && audio.OverrideAttenuation)
                {
                    handler->instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, audio.OverrideMinDistance);
                    handler->instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, audio.OverrideMaxDistance);
                }

                handler->instance.start();

                //audio.eventDescription.getPath(out string path);
                //Collections.Point.Log(Collections.Point.LogChannel.Audio,
                //    $"Play({path}) with {audio.parameters.Length} parameters(" + paramsString + ")");
            }
        }
        public static void Stop(ref Audio audio)
        {
#if DEBUG_MODE
            if (!audio.IsValid())
            {
                throw new System.Exception();
            }
#endif
            unsafe
            {
                //if (audio.audioHandler->instance.isValid())
                //{
                    
                //}
                audio.audioHandler->instance.stop(audio.AllowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
        }
    }
}
