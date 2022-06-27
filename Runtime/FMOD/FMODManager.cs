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
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Point.Collections.Buffer.LowLevel;
using System;
using System.Reflection;
using Point.Collections.Buffer;
using Sirenix.Utilities;

namespace Point.Audio
{
    [AddComponentMenu("")]
    public sealed class FMODManager : StaticMonobehaviour<FMODManager>, IStaticInitializer
    {
        protected override bool EnableLog => false;
        protected override bool HideInInspector => true;

        internal static FMOD.Studio.System StudioSystem => FMODUnity.RuntimeManager.StudioSystem;
        internal static FMOD.System CoreSystem => FMODUnity.RuntimeManager.CoreSystem;
        internal static ResonanceAudioHelper ResonanceAudio => Instance.m_ResonanceAudioHelper;

        public const string
            EventPrefix = "event:/",
            BusPrefix = "bus:/",
            SnapshotPrefix = "snapshot:/";

        private ResonanceAudioHelper m_ResonanceAudioHelper;
        private Dictionary<Hash, IFMODEvent> m_ExposedEvents = new Dictionary<Hash, IFMODEvent>();
        
        private IUserPropertyProcessor[] m_GlobalPropertyProcesors = Array.Empty<IUserPropertyProcessor>();
        private GlobalParameterLinker[] m_GlobalParameterLinkers = Array.Empty<GlobalParameterLinker>();

        private AtomicSafeBoolen m_IsFocusing;

        private ObjectPool<FMODUnity.StudioListener> m_ListenerPool;

        private JobHandle m_GlobalJobHandle;

        private UnsafeAudioHandlerContainer m_Handlers;

        public IFMODEvent this[string name]
        {
            get
            {
                Hash hash = new Hash(name);
                if (m_ExposedEvents.TryGetValue(hash, out var value)) return value;
                return null;
            }
            set
            {
                Hash hash = new Hash(name);
                m_ExposedEvents[hash] = value;

                $"Exposed Event({name}) has been set".ToLog(LogChannel.Audio);
            }
        }

        #region Class Instruction

        protected override void OnInitialize()
        {
            m_IsFocusing = true;

            m_ResonanceAudioHelper = new ResonanceAudioHelper();
            m_Handlers = new UnsafeAudioHandlerContainer(128);

            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            LoadDynamicPlugins();
            LoadBanks();
        }
        private void SetupObjectPool()
        {
            m_ListenerPool = new ObjectPool<FMODUnity.StudioListener>(
                factory: ListenerFactory, 
                onGet: ListenerOnGet,
                onReserve: ListenerOnReserve,
                onRelease: ListenerOnRelease
                );

        }
        private void LoadDynamicPlugins()
        {
            const string pluginName = "Point.Audio.Native";
            FMOD.RESULT result;
#if UNITY_EDITOR
            RuntimePlatform currentPlatform = Application.platform;
            if (currentPlatform == RuntimePlatform.WindowsEditor ||
                currentPlatform == RuntimePlatform.OSXEditor ||
                currentPlatform == RuntimePlatform.LinuxEditor)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets(pluginName + " l:Dll l:Audio");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    result = CoreSystem.loadPlugin(path, out _);
                    if (result != FMOD.RESULT.OK)
                    {
                        PointHelper.LogError(Channel.Audio,
                            $"Err. falid to load plugin({path}), {result}");
                    }
                }

                return;
            }
#endif
            var platform = FMODUnity.Settings.Instance.FindCurrentPlatform();
            string pluginPath = platform.GetPluginPath(pluginName);

            uint handle;

            result = CoreSystem.loadPlugin(pluginPath, out handle);

#if UNITY_64 || UNITY_EDITOR_64
            // Add a "64" suffix and try again
            if (result == FMOD.RESULT.ERR_FILE_BAD || result == FMOD.RESULT.ERR_FILE_NOTFOUND)
            {
                string pluginPath64 = platform.GetPluginPath(pluginName + "64");
                result = CoreSystem.loadPlugin(pluginPath64, out handle);
            }
#endif
        }
        private void LoadBanks()
        {
            const string
                c_MasterBank = "Master";

            FMODUnity.RuntimeManager.LoadBank(c_MasterBank);
            FMODUnity.RuntimeManager.LoadBank(c_MasterBank + ".strings");
        }
        private void RegisterUserPropertyProcessors()
        {
            List<IUserPropertyProcessor> global = new List<IUserPropertyProcessor>();
            foreach (var item in TypeHelper.GetTypesIter(t => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<IUserPropertyProcessor>.Type.IsAssignableFrom(t)))
            {
                global.Add((IUserPropertyProcessor)Activator.CreateInstance(item));

#if UNITY_EDITOR
                PointHelper.Log(Channel.Audio,
                    $"Registered FMOD parameter processor({TypeHelper.ToString(item)}).");
#endif
            }

            m_GlobalPropertyProcesors = global.ToArray();
        }
        private void RegisterGlobalParameterProcessors()
        {
            List<GlobalParameterLinker> linkers = new List<GlobalParameterLinker>();

            foreach (var item in TypeHelper.GetTypesIter(t => !t.IsAbstract && !t.IsInterface && TypeHelper.InheritsFrom<IGlobalParameterProcessor>(t)))
            {
                //Type[] interfaces = TypeHelper.GetInterfacesWithOpenGenericInterface<IGlobalParameterProcessor>(item);

                //foreach (var interfaceType in interfaces)
                //{
                //    interfaceType
                //}
                IGlobalParameterProcessor processor 
                    = (IGlobalParameterProcessor)Activator.CreateInstance(item);

                if (processor is IGlobalParameterUpdate updater)
                {
                    linkers.Add(new GlobalParameterUpdateLinker(updater));
                }
            }

            m_GlobalParameterLinkers = linkers.ToArray();
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            FMODRuntimeVariables variables = FMODRuntimeVariables.Instance;

            variables.StopGlobalAudios();
            variables.StartSceneDependencies(arg0);
        }

        protected override void OnShutdown()
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

            m_Handlers.Dispose();
            m_ResonanceAudioHelper.Dispose();
        }

        private static FMODUnity.StudioListener ListenerFactory()
        {
            GameObject obj = new GameObject("Listener");
            return obj.AddComponent<FMODUnity.StudioListener>();
        }
        private static void ListenerOnGet(FMODUnity.StudioListener listener)
        {
            listener.enabled = true;
        }
        private static void ListenerOnReserve(FMODUnity.StudioListener listener)
        {
            listener.enabled = false;
        }
        private static void ListenerOnRelease(FMODUnity.StudioListener listener)
        {
            Destroy(listener.gameObject);
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

            //PauseAllEvents(UnityEditor.EditorUtility.audioMasterMute);
#endif
            for (int i = 0; i < m_GlobalParameterLinkers.Length; i++)
            {
                m_GlobalParameterLinkers[i].Update();
            }


            #region Jobs

            m_GlobalJobHandle.Complete();

            JobHandle handlerJob = m_Handlers.Data.ScheduleUpdate();
            m_GlobalJobHandle = JobHandle.CombineDependencies(m_GlobalJobHandle, handlerJob);

            #endregion
        }

        private void OnApplicationFocus(bool focus)
        {
            m_IsFocusing.Value = focus;
        }

        #endregion

        #region General Controls

        public static FMODUnity.StudioListener SetupListener(GameObject obj)
        {
            return obj.AddComponent<FMODUnity.StudioListener>();
        }
        public static FMODUnity.StudioListener SetupListener()
        {
            FMODUnity.StudioListener listener = Instance.m_ListenerPool.Get();

            return listener;
        }
        public static FMODUnity.StudioListener GetMainListener()
        {
            int main = GetMainListenerIndex();
            for (int i = 0; i < FMODUnity.StudioListener.listeners.Count; i++)
            {
                if (FMODUnity.StudioListener.listeners[i].ListenerNumber == main)
                {
                    return FMODUnity.StudioListener.listeners[i];
                }
            }

            return null;
        }
        public static int GetMainListenerIndex()
        {
            StudioSystem.getNumListeners(out int numListeners);
            int index = 0;
            float max;
            StudioSystem.getListenerWeight(0, out max);
            for (int i = 1; i < numListeners; i++)
            {
                StudioSystem.getListenerWeight(i, out float temp);
                if (temp > max)
                {
                    max = temp;
                    index = i;
                }
            }

            return index;
        }
        public static FMOD.RESULT SetupListenerWeight(FMODUnity.StudioListener listener, in float weight)
            => SetupListenerWeight(listener.ListenerNumber, in weight);
        public static FMOD.RESULT SetupListenerWeight(int listener, in float weight)
        {
            StudioSystem.getNumListeners(out int numListeners);

            float sum = 0;
            List<int> temp = new List<int>();
            for (int i = 0; i < numListeners; i++)
            {
                StudioSystem.getListenerWeight(i, out float targetWeight);
                if (targetWeight > 0 && i != listener)
                {
                    temp.Add(i);

                    sum += targetWeight * 100;
                }
            }

            if (100 - sum + weight < 0)
            {
                float a = (sum + weight - 100) / temp.Count;
                for (int i = 0; i < temp.Count; i++)
                {
                    StudioSystem.getListenerWeight(temp[i], out float currentWeight);
                    StudioSystem.setListenerWeight(temp[i], currentWeight - a);
                }
            }
            else
            {
                float a = (100 - sum + weight) / temp.Count;
                for (int i = 0; i < temp.Count; i++)
                {
                    StudioSystem.getListenerWeight(temp[i], out float currentWeight);
                    StudioSystem.setListenerWeight(temp[i], currentWeight + a);
                }
            }

            FMOD.RESULT result = StudioSystem.setListenerWeight(listener, weight);
            return result;
        }
        public static void ReserveListener(FMODUnity.StudioListener listener)
        {
            Instance.m_ListenerPool.Reserve(listener);
        }

        public static ParamReference GetGlobalParameter<TEnum>()
            where TEnum : struct, IConvertible
        {
#if DEBUG_MODE
            if (!FMODExtensions.IsFMODEnum<TEnum>())
            {
                PointHelper.LogError(Channel.Audio,
                    $"");

                return default(ParamReference);
            }
#endif
            return GetGlobalParameter(FMODExtensions.ConvertToName<TEnum>());
        }
        public static ParamReference GetGlobalParameter(in string name)
        {
            var result = StudioSystem.getParameterDescriptionByName(name, out var description);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Parameter({name}) is not present in the current FMOD.");

                return default(ParamReference);
            }
#endif
            StudioSystem.getParameterByID(description.id, out float value);
            var param = new ParamReference(name, value);

            return param;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEnum"><seealso cref="FMODEnumAttribute"/> 를 상속받는 타입</typeparam>
        /// <param name="value"></param>
        public static void SetGlobalParameter<TEnum>(TEnum value)
            where TEnum : struct, IConvertible
        {
#if DEBUG_MODE
            if (!FMODExtensions.IsFMODEnum<TEnum>())
            {
                PointHelper.LogError(Channel.Audio,
                    $"");

                return;
            }
#endif
            int temp = value.ToInt32();
            SetGlobalParameter(FMODExtensions.ConvertToName<TEnum>(), temp);
        }
        public static void SetGlobalParameter(string name, float value)
        {
            var result = StudioSystem.getParameterDescriptionByName(name.ToString(), out var description);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Parameter({name}) is not present in the current FMOD.");

                return;
            }
#endif

            StudioSystem.setParameterByID(description.id, value);

#if DEBUG_MODE
            PointHelper.Log(Channel.Audio,
                $"Global parameter({name}) has set to {value}.");
#endif
        }
        public static void SetGlobalParameter(ParamReference parameter)
        {
            var result = StudioSystem.setParameterByID(parameter.description.id, parameter.value);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Parameter({(string)parameter.description.name}) is not present in the current FMOD.");
            }

            PointHelper.Log(Channel.Audio,
                $"Global parameter({(string)parameter.description.name}) has set to {parameter.value}.");
#endif
        }

        public static EventDescription GetEventDescription(FMODUnity.EventReference ev)
        {
            return FMODUnity.RuntimeManager.GetEventDescription(ev);
        }
        public static Snapshot GetSnapshot(in string name)
        {
            var result = StudioSystem.getEvent(SnapshotPrefix + name, out var ev);
#if DEBUG_MODE
            if ((result & FMOD.RESULT.OK) != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Could not retriving information of snapshot({name}).");
            }
#endif
            return new Snapshot(ev);
        }

        public static bool IsBankLoaded(string name) => FMODUnity.RuntimeManager.HasBankLoaded(name);
        public static bool LoadBank(string name, bool loadSamples = false)
        {
            PointHelper.AssertMainThread();

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
            PointHelper.AssertMainThread();

            FMODUnity.RuntimeManager.UnloadBank(name);
        }
        public static FMOD.Studio.Bank GetBank(string path)
        {
            PointHelper.AssertMainThread();

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
            PointHelper.AssertMainThread();

            var result = StudioSystem.getEventByID(eventRef.Guid, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Critical Error. Please fix other issues before play the game. \n" +
                    $"{result}");
                Debug.Break();
                throw new System.Exception();
            }
#endif
            Audio audio = new Audio(Instance.m_Handlers, ev);

            return audio;
        }
        public static void GetAudio(in FMODUnity.EventReference eventRef, ref Audio audio)
        {
            PointHelper.AssertMainThread();

            var result = StudioSystem.getEventByID(eventRef.Guid, out FMOD.Studio.EventDescription ev);
#if UNITY_EDITOR
            if (result != FMOD.RESULT.OK)
            {
                PointHelper.LogError(Channel.Audio,
                    $"Error has been raised while retriving event({eventRef.Path}) {result}.");
                return;
            }
#endif
            audio.SetEvent(ev);
        }
        public static Audio GetAudio(in string eventPath)
        {
            PointHelper.AssertMainThread();

            var result = StudioSystem.getEvent(eventPath, out FMOD.Studio.EventDescription ev);
#if DEBUG_MODE
            if (result != FMOD.RESULT.OK)
            {
                throw new System.Exception();
            }
#endif
            Audio audio = new Audio(Instance.m_Handlers, ev);

            return audio;
        }

        public static FMOD.Studio.Bus GetBusByID(FMOD.GUID id)
        {
            PointHelper.AssertMainThread();

            if (StudioSystem.getBusByID(id, out var bus) != FMOD.RESULT.OK)
            {
                return default(FMOD.Studio.Bus);
            }

            return bus;
        }
        public static FMOD.Studio.VCA GetVCA(FMOD.GUID id)
        {
            PointHelper.AssertMainThread();

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
            PointHelper.AssertMainThread();
#if DEBUG_MODE
            if (!audio.IsValidID())
            {
                PointHelper.LogError(Channel.Audio,
                    $"This audio has an invalid FMOD id but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            UnsafeReference<UnsafeAudioHandler> handler = Instance.m_Handlers.Data.GetUnusedHandler();
            handler.Value.instanceHash = audio.hash;
            handler.Value.translation = audio._translation;
            handler.Value.rotation = audio._rotation;
            handler.Value.generation = unchecked(handler.Value.generation + 1);

            audio.SetAudioHandler(handler);

            handler.Value.CreateInstance(ref audio);
        }
        public struct FindInstanceEnumerator : IEnumerable<EventInstance>
        {
            private UnsafeAudioHandlerContainer.FindEventEnumerator m_Handler;

            internal FindInstanceEnumerator(UnsafeAudioHandlerContainer.FindEventEnumerator handler)
            {
                m_Handler = handler;
            }

            public IEnumerator<EventInstance> GetEnumerator()
            {
                foreach (var item in m_Handler)
                {
                    yield return item.Value.instance;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public static FindInstanceEnumerator FindEventInstancesOf(EventDescription description)
        {
            return new FindInstanceEnumerator(Instance.m_Handlers.Data.FindEventInstancesOf(description));
        }

        private void ProcessUserProperty(ref Audio audio)
        {
            for (int i = 0; i < m_GlobalPropertyProcesors.Length; i++)
            {
                foreach (USER_PROPERTY item in audio.GetPropertyEnumerator())
                {
                    m_GlobalPropertyProcesors[i].OnProcess(ref audio, in item);
                }
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
                PointHelper.LogError(Channel.Audio,
                    $"This audio has an invalid FMOD id but trying to play. " +
                    $"This is not allowed.");

                return;
            }
#endif
            if (!audio.IsValid())
            {
                CreateInstance(ref audio);
            }

            audio.audioHandler.Value.Set3DAttributes();
            audio.audioHandler.Value.SetParameters(ref audio);

            Instance.ProcessUserProperty(ref audio);

            audio.audioHandler.Value.StartInstance();

            //PointHelper.Log(Channel.Audio, $"Playing audio({audio})");
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
                PointHelper.LogError(Channel.Audio,
                    $"This audio is invalid but trying to stop. " +
                    $"This is not allowed.");
            }
#endif
            audio.audioHandler.Value.StopInstance(audio.AllowFadeout);

            //PointHelper.Log(Channel.Audio, $"Stop audio({audio})");
        }

        #endregion

        /// <summary>
        /// <see cref="FMODAudioRoom"/> 에서 기준을 잡을 <see cref="Transform"/> 을 설정합니다.
        /// </summary>
        /// <param name="tr"></param>
        public static void SetAudioRoomTarget(Transform tr)
        {
            ResonanceAudio.SetRoomTarget(tr);
        }

        #region Inner Classes

        public sealed class ResonanceAudioHelper : IDisposable
        {
            public const float
                /// Maximum allowed gain value in decibels.
                maxGainDb = 24.0f,
                /// Minimum allowed gain value in decibels.
                minGainDb = -24.0f,
                /// Maximum allowed reverb brightness modifier value.
                maxReverbBrightness = 1.0f,
                /// Minimum allowed reverb brightness modifier value.
                minReverbBrightness = -1.0f,
                /// Maximum allowed reverb time modifier value.
                maxReverbTime = 3.0f,
                /// Maximum allowed reflectivity multiplier of a room surface material.
                maxReflectivity = 2.0f;

            // Right-handed to left-handed matrix converter (and vice versa).
            private static readonly Matrix4x4 flipZ = Matrix4x4.Scale(new Vector3(1, 1, -1));

            // Plugin data parameter index for the room properties.
            private static readonly int roomPropertiesIndex = 1;

            // Container to store the currently active rooms in the scene.
            private List<FMODAudioRoom> enabledRooms;
            private UnsafeAllocator<AudioRoom> m_TempRoomBuffer;
            private FMOD.DSP m_ResonanceAudioListenerPlugin;

            private Transform m_RoomTarget;

            public Transform RoomTarget
            {
                get => m_RoomTarget;
                set => SetRoomTarget(value);
            }
            public Vector3 RoomTargetPosition
            {
                get
                {
                    if (m_RoomTarget == null)
                    {
                        StudioSystem.getListenerAttributes(0, out FMOD.ATTRIBUTES_3D att);
                        return new Vector3(att.position.x, att.position.y, att.position.z);
                    }

                    return m_RoomTarget.position;
                }
            }

            public ResonanceAudioHelper()
            {
                enabledRooms = new List<FMODAudioRoom>();
                m_TempRoomBuffer = new UnsafeAllocator<AudioRoom>(1, Allocator.Persistent);
                m_ResonanceAudioListenerPlugin = LoadResonanceAudioPlugin();

                m_RoomTarget = null;
            }
            private static FMOD.DSP LoadResonanceAudioPlugin()
            {
                const string
                    c_PluginName = "Resonance Audio Listener",
                    c_ResonanceAudioBusName = BusPrefix + c_PluginName;

                FMOD.DSP dsp = default(FMOD.DSP);
                FMOD.RESULT result = StudioSystem.getBus(c_ResonanceAudioBusName, out Bus resonanceAudioBus);
                StudioSystem.flushCommands();
                if ((result & FMOD.RESULT.OK) != FMOD.RESULT.OK)
                {
                    return dsp;
                }

                result = resonanceAudioBus.getChannelGroup(out FMOD.ChannelGroup group);
                StudioSystem.flushCommands();
                if ((result & FMOD.RESULT.OK) != FMOD.RESULT.OK)
                {
                    PointHelper.LogError(Channel.Audio,
                        $"Could not resolve Resonance Audio Listener group.");

                    return dsp;
                }

                return group.getDSP(c_ResonanceAudioBusName);
            }

            public void SetRoomTarget(Transform transform)
            {
                m_RoomTarget = transform;
            }
            public void RegisterAudioRoom(FMODAudioRoom room)
            {
                enabledRooms.Add(room);
            }
            public void RemoveAudioRoom(FMODAudioRoom room)
            {
                enabledRooms.Remove(room);
            }

            /// Updates the room effects of the environment with given |room| properties.
            /// @note This should only be called from the main Unity thread.
            public void UpdateAudioRoom(FMODAudioRoom room, bool roomEnabled)
            {
                // Update the enabled rooms list.
                if (roomEnabled)
                {
                    if (!enabledRooms.Contains(room))
                    {
                        enabledRooms.Add(room);
                    }
                }
                else
                {
                    enabledRooms.Remove(room);
                }
                // Update the current room effects to be applied.

                FMOD.RESULT result;
                if (enabledRooms.Count > 0)
                {
                    FMODAudioRoom currentRoom = enabledRooms[enabledRooms.Count - 1];
                    m_TempRoomBuffer[0] = GetRoomProperties(currentRoom);

                    result = m_ResonanceAudioListenerPlugin.setParameterData(roomPropertiesIndex, 
                        UnsafeBufferUtility.ToBytes(m_TempRoomBuffer.Ptr, (int)m_TempRoomBuffer.Size));
                }
                else
                {
                    // Set the room properties to a null room, which will effectively disable the room effects.
                    result = m_ResonanceAudioListenerPlugin.setParameterData(roomPropertiesIndex, Array.Empty<byte>());
                }

                if ((result & FMOD.RESULT.OK) != FMOD.RESULT.OK)
                {
                    "err".ToLogError();
                }
            }

            #region Utils

            // Converts given |db| value to its amplitude equivalent where 'dB = 20 * log10(amplitude)'.
            private static float ConvertAmplitudeFromDb(float db)
            {
                return Mathf.Pow(10.0f, 0.05f * db);
            }

            // Converts given |position| and |rotation| from Unity space to audio space.
            private static void ConvertAudioTransformFromUnity(ref Vector3 position,
              ref Quaternion rotation)
            {
                // Compose the transformation matrix.
                Matrix4x4 transformMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
                // Convert the transformation matrix from left-handed to right-handed.
                transformMatrix = flipZ * transformMatrix * flipZ;
                // Update |position| and |rotation| respectively.
                position = transformMatrix.GetColumn(3);
                rotation = Quaternion.LookRotation(transformMatrix.GetColumn(2), transformMatrix.GetColumn(1));
            }
            // Returns room properties of the given |room|.
            private static AudioRoom GetRoomProperties(FMODAudioRoom room)
            {
                AudioRoom roomProperties;
                Vector3 position = room.transform.position;
                Quaternion rotation = room.transform.rotation;
                Vector3 scale = Vector3.Scale(room.transform.lossyScale, room.size);
                ConvertAudioTransformFromUnity(ref position, ref rotation);
                roomProperties.positionX = position.x;
                roomProperties.positionY = position.y;
                roomProperties.positionZ = position.z;
                roomProperties.rotationX = rotation.x;
                roomProperties.rotationY = rotation.y;
                roomProperties.rotationZ = rotation.z;
                roomProperties.rotationW = rotation.w;
                roomProperties.dimensionsX = scale.x;
                roomProperties.dimensionsY = scale.y;
                roomProperties.dimensionsZ = scale.z;
                roomProperties.materialLeft = room.leftWall;
                roomProperties.materialRight = room.rightWall;
                roomProperties.materialBottom = room.floor;
                roomProperties.materialTop = room.ceiling;
                roomProperties.materialFront = room.frontWall;
                roomProperties.materialBack = room.backWall;
                roomProperties.reverbGain = ConvertAmplitudeFromDb(room.reverbGainDb);
                roomProperties.reverbTime = room.reverbTime;
                roomProperties.reverbBrightness = room.reverbBrightness;
                roomProperties.reflectionScalar = room.reflectivity;
                return roomProperties;
            }

            #endregion

            public void Dispose()
            {
                m_TempRoomBuffer.Dispose();
            }
        }

        private abstract class GlobalParameterLinker
        {
            public abstract void Update();
        }
        private sealed class GlobalParameterUpdateLinker : GlobalParameterLinker
        {
            private IGlobalParameterUpdate m_Updater;
            private ParamReference m_Parameter;

            public GlobalParameterUpdateLinker(IGlobalParameterUpdate t)
            {
                m_Updater = t;
                m_Parameter = GetGlobalParameter(t.Name);
            }

            public override void Update()
            {
                if (!m_Updater.Enable) return;

                m_Parameter.value = m_Updater.Update();
                SetGlobalParameter(m_Parameter);
            }
        }

        #endregion
    }

    public interface IGlobalParameterProcessor
    {
        
    }
    public interface IGlobalParameterUpdate : IGlobalParameterProcessor
    {
        string Name { get; }
        bool Enable { get; }
        float Update();
    }
    //struct DefaultParameterProcessor : IGlobalParameterUpdate
    //{
    //    private readonly FixedString512Bytes m_Name;

    //    bool IGlobalParameterUpdate.EnableUpdate => true;
    //    string IGlobalParameterName.Name => m_Name.ToString();

    //    public float Update()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
