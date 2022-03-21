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

using Point.Collections;
using System.Reflection;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace Point.Audio
{
    public static class FMODExtensions
    {
        public static FMOD.DSP getDSP(in this FMOD.Studio.Bus bus, in string dspName)
        {
            FMOD.DSP dsp = default(FMOD.DSP);

            bus.getChannelGroup(out var group);
            FMODManager.StudioSystem.flushCommands();
            if (!group.hasHandle()) return dsp;

            group.getNumDSPs(out int count);
            string name;
            for (int i = 0; i < count; i++)
            {
                group.getDSP(i, out dsp);
                dsp.getInfo(out name, out _, out _, out _, out _);

                if (name.Equals(dspName)) return dsp;
            }

            return dsp;
        }
        public static FMOD.DSP getDSP(in this FMOD.ChannelGroup group, in string dspName)
        {
            FMOD.DSP dsp = default(FMOD.DSP);

            if (!group.hasHandle()) return dsp;

            group.getNumDSPs(out int count);
            string name;
            for (int i = 0; i < count; i++)
            {
                group.getDSP(i, out dsp);
                dsp.getInfo(out name, out _, out _, out _, out _);

                if (name.Equals(dspName)) return dsp;
            }

            return dsp;
        }

        public static bool IsListenerInsideRoom(this FMODAudioRoom room)
        {
            // Compute the room position relative to the listener.
            //FMODManager.StudioSystem.getListenerAttributes(0, out FMOD.ATTRIBUTES_3D att);
            
            Quaternion rotationInverse = Quaternion.Inverse(room.transform.rotation);
            Vector3 listenerPosition = rotationInverse * FMODManager.ResonanceAudio.RoomTargetPosition;

            bool result = room.Bounds.Contains(listenerPosition);

            return result;
        }

        public static bool IsFMODEnum<TEnum>()
        {
            if (!TypeHelper.TypeOf<TEnum>.Type.IsEnum)
            {
                return false;
            }
            else if (TypeHelper.TypeOf<TEnum>.Type.GetCustomAttribute<FMODEnumAttribute>() == null)
            {
                return false;
            }

            return true;
        }
        public static string ConvertToName<TEnum>()
        {
            var att = TypeHelper.TypeOf<TEnum>.Type.GetCustomAttribute<FMODEnumAttribute>();
            return att.ConvertToName();
        }
        public static string ConvertToName<TEnumAttribute>(this TEnumAttribute att)
            where TEnumAttribute : FMODEnumAttribute
        {
            return string.IsNullOrEmpty(att.Name) ? TypeHelper.TypeOf<TEnumAttribute>.Name : att.Name;
        }

        public static FMOD.RESULT SetWeight(this FMODUnity.StudioListener t, [AssumeRange(0, 1)] in float value)
        {
            return FMODManager.SetupListenerWeight(t, value);
        }
    }
}
