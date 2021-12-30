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
using Point.Collections.Threading;
using Point.Audio.LowLevel;
using Unity.Jobs;
using FMOD.Studio;
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

        private AtomicSafeBoolen m_IsFocusing;

        private JobHandle m_GlobalJobHandle;

        private UnsafeAudioHandlerContainer m_Handlers;

        #region Class Instruction

        protected override void OnInitialze()
        {
            m_IsFocusing = true;

            unsafe
            {
                m_Handlers = new UnsafeAudioHandlerContainer(128);
            }

            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            FMODRuntimeVariables variables = FMODRuntimeVariables.Instance;
            variables.StartSceneDependencies(arg0);
        }

        protected override void OnShutdown()
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

            m_Handlers.Dispose();
        }

        #endregion

        #region Monobehaviour Messages

        private void Start()
        {
            Scene currentScene = SceneManager.GetActiveScene();

            FMODRuntimeVariables variables = FMODRuntimeVariables.Instance;
            variables.Initialize();
            variables.StartSceneDependencies(currentScene);
        }
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
            m_IsFocusing.Value = focus;
        }

        #endregion

        public static ParamReference GetGlobalParameter(string name)
        {
            var result = StudioSystem.getParameterDescriptionByName(name, out var description);
            if (result != FMOD.RESULT.OK)
            {
                Collections.PointCore.LogError(Collections.PointCore.LogChannel.Audio,
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
                Collections.PointCore.LogError(Collections.PointCore.LogChannel.Audio,
                    $"Parameter({name}) is not present in the current FMOD.");

                return;
            }

            StudioSystem.setParameterByID(description.id, value);

            Collections.PointCore.Log(Collections.PointCore.LogChannel.Audio,
                $"Global parameter({name}) has set to {value}.");
        }
        public static void SetGlobalParameter(ParamReference parameter)
        {
            var result = StudioSystem.setParameterByID(parameter.description.id, parameter.value);
            if (result != FMOD.RESULT.OK)
            {
                Collections.PointCore.LogError(Collections.PointCore.LogChannel.Audio,
                    $"Parameter({(string)parameter.description.name}) is not present in the current FMOD.");
            }

            Collections.PointCore.Log(Collections.PointCore.LogChannel.Audio,
                $"Global parameter({(string)parameter.description.name}) has set to {parameter.value}.");
        }

        public static bool IsBankLoaded(string name) => FMODUnity.RuntimeManager.HasBankLoaded(name);
        public static bool LoadBank(string name, bool loadSamples = false)
        {
            PointCore.AssertMainThread();

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
            PointCore.AssertMainThread();

            FMODUnity.RuntimeManager.UnloadBank(name);
        }
        public static FMOD.Studio.Bank GetBank(string path)
        {
            PointCore.AssertMainThread();

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
            PointCore.AssertMainThread();

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
            PointCore.AssertMainThread();

            var result = StudioSystem.getEventByID(eventRef.Guid, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                Collections.PointCore.LogError(Collections.PointCore.LogChannel.Audio,
                    $"Error has been raised while retriving event({eventRef.Path}) {result}.");
                return;
            }
#endif
            audio.SetEvent(ev);
        }
        public static Audio GetAudio(in string eventPath)
        {
            PointCore.AssertMainThread();

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
            PointCore.AssertMainThread();

            if (StudioSystem.getBusByID(id, out var bus) != FMOD.RESULT.OK)
            {
                return default(FMOD.Studio.Bus);
            }

            return bus;
        }
        public static FMOD.Studio.VCA GetVCA(FMOD.GUID id)
        {
            PointCore.AssertMainThread();

            if (StudioSystem.getVCAByID(id, out FMOD.Studio.VCA vca) != FMOD.RESULT.OK)
            {
                return default(FMOD.Studio.VCA);
            }

            return vca;
        }

        /// <summary>
        /// 오디오의 인스턴스 객체를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 모든 FMOD 오디오는 인스턴스 객체가 존재하여야 재생될 수 있습니다. 
        /// <seealso cref="Play(ref Audio)"/> 메소드는 인스턴스가 실행되지 않은 오디오일 경우, 
        /// 이 메소드를 통해 인스턴스를 생성하고 재생합니다.
        /// </remarks>
        /// <param name="audio"></param>
        public static void CreateInstance(ref Audio audio)
        {
            PointCore.AssertMainThread();

#if DEBUG_MODE
            if (!audio.IsValidID())
            {
                Collections.PointCore.LogError(Collections.PointCore.LogChannel.Audio,
                    $"This audio has an invalid FMOD id but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif

            unsafe
            {
                UnsafeAudioHandler* handler = Instance.m_Handlers.Insert(ref audio);
                handler->CreateInstance(ref audio);
            }
        }
        /// <summary>
        /// 오디오를 재생합니다.
        /// </summary>
        /// <remarks>
        /// 인스턴스가 생성되지 않았다면, 즉시 생성 후 재생합니다.
        /// </remarks>
        /// <param name="audio"></param>
        public static void Play(ref Audio audio)
        {
#if DEBUG_MODE
            if (!audio.IsValidID())
            {
                Collections.PointCore.LogError(Collections.PointCore.LogChannel.Audio,
                    $"This audio has an invalid FMOD id but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            if (!audio.IsValid())
            {
                CreateInstance(ref audio);
            }

            unsafe
            {
                audio.audioHandler->Set3DAttributes();
                audio.audioHandler->SetParameters(ref audio);

                audio.audioHandler->StartInstance();
            }
        }
        /// <summary>
        /// 오디오를 정지합니다.
        /// </summary>
        /// <param name="audio"></param>
        public static void Stop(ref Audio audio)
        {
#if DEBUG_MODE
            if (!audio.IsValid())
            {
                Collections.PointCore.LogError(Collections.PointCore.LogChannel.Audio,
                    $"This audio is invalid but trying to stop. " +
                    $"This is not allowed.");
            }
#endif
            unsafe
            {
                audio.audioHandler->StopInstance(audio.AllowFadeout);
            }
        }
    }
}
