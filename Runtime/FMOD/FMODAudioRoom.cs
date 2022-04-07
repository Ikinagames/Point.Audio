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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Point.Collections;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Point.Audio
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Point/FMOD/Audio Room")]
    public sealed class FMODAudioRoom : FMODBehaviour
    {
        /// Room surface material in negative x direction.
        public SurfaceMaterial leftWall = SurfaceMaterial.ConcreteBlockCoarse;
        /// Room surface material in positive x direction.
        public SurfaceMaterial rightWall = SurfaceMaterial.ConcreteBlockCoarse;
        /// Room surface material in negative y direction.
        public SurfaceMaterial floor = SurfaceMaterial.ParquetOnConcrete;
        /// Room surface material in positive y direction.
        public SurfaceMaterial ceiling = SurfaceMaterial.PlasterRough;
        /// Room surface material in negative z direction.
        public SurfaceMaterial backWall = SurfaceMaterial.ConcreteBlockCoarse;
        /// Room surface material in positive z direction.
        public SurfaceMaterial frontWall = SurfaceMaterial.ConcreteBlockCoarse;

        [Space]
        /// Reflectivity scalar for each surface of the room.
        [Range(0, 2)]
        public float reflectivity = 1.0f;
        /// Reverb gain modifier in decibels.
        [Range(-24, 24)]
        public float reverbGainDb = 0.0f;
        /// Reverb brightness modifier.
        [Range(-1, 1)]
        public float reverbBrightness = 0.0f;
        /// Reverb time modifier.
        [Range(0, 3)]
        public float reverbTime = 1.0f;
        /// Size of the room (normalized with respect to scale of the game object).
        public Vector3 size = Vector3.one;

        [Space]
        [Header("Connected Emitters")]
        public FMODUnity.StudioEventEmitter[] m_Emitters = Array.Empty<FMODUnity.StudioEventEmitter>();

        [Space]
        [Header("Trigger")]
        public ParamField[] m_OnEnter = Array.Empty<ParamField>();
        public ParamField[] m_OnExit = Array.Empty<ParamField>();

        public AABB Bounds => new AABB(transform.position, size);
        private bool m_IsEntered = false;

        public event Action OnEntered;
        public event Action OnExited;

        void OnEnable()
        {
            ExecuteTriggerAction(FMODExtensions.IsListenerInsideRoom(this));
            FMODManager.ResonanceAudio.UpdateAudioRoom(this, m_IsEntered);
        }
        void OnDisable()
        {
            if (PointApplication.IsShutdown) return;

            FMODManager.ResonanceAudio.UpdateAudioRoom(this, false);
            m_IsEntered = false;
        }

        private void ExecuteTriggerAction(bool entered)
        {
            if (entered && !m_IsEntered)
            {
                for (int i = 0; i < m_OnEnter.Length; i++)
                {
                    ExecuteParam(m_OnEnter[i]);
                }

                m_IsEntered = true;
                OnEntered?.Invoke();

                return;
            }
            else if (!entered && m_IsEntered)
            {
                for (int i = 0; i < m_OnExit.Length; i++)
                {
                    ExecuteParam(m_OnExit[i]);
                }

                m_IsEntered = false;
                OnExited?.Invoke();

                return;
            }
        }
        private void ExecuteParam(ParamField param)
        {
            if (param.IsGlobal)
            {
                param.Execute();
                return;
            }

            for (int i = 0; i < m_Emitters.Length; i++)
            {
                param.Execute(m_Emitters[i].EventInstance);
            }
        }
        void Update()
        {
            if (FMODExtensions.IsListenerInsideRoom(this))
            {
                FMODManager.ResonanceAudio.UpdateAudioRoom(this, true);
                ExecuteTriggerAction(true);
            }
            else
            {
                FMODManager.ResonanceAudio.UpdateAudioRoom(this, false);
                ExecuteTriggerAction(false);
            }
        }
#if DEBUG_MODE
        void OnDrawGizmosSelected()
        {
            // Draw shoebox model wireframe of the room.
            Gizmos.color = Color.yellow;
            //Gizmos.matrix = transform.localToWorldMatrix;
            var bounds = Bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
    }
}
