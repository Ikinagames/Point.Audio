using System;

using UnityEngine;

#if POINT_ARBOR

using Arbor;

namespace Point.Audio.Arbor
{
    [AddComponentMenu("")]
    [AddBehaviourMenu("Point/Audio/Set Global Parameter")]
    public sealed class SetGlobalParameterBehaviour : StateBehaviour
    {
        [FMODParam(true, true)]
        [SerializeField] private ParamField[] m_Parameters = Array.Empty<ParamField>();

        public override void OnStateBegin()
        {
            m_Parameters.Execute();
        }
    }
}

#endif