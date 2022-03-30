using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using FMODUnity;

#if POINT_ARBOR

using Arbor;

namespace Point.Audio.Arbor
{
    [AddComponentMenu("")]
    [AddBehaviourMenu("Point/Audio/Play")]
    public sealed class PlayAudioBehaviour : StateBehaviour
    {
        [SerializeField] private AudioReference m_AudioReference;

        //[SerializeField] FlexibleField<Audio> m_AudioField;

        private Audio m_Audio;

        //protected override void OnCreated()
        //{
        //    //if (!m_Event.IsNull) return;

        //    //m_Audio = m_AudioField.value;
        //    //FMODManager.GetAudio(m_Event, ref m_Audio);

        //    //if (m_Parameters.Length > 0)
        //    //{
        //    //    m_ParsedParameters = new ParamReference[m_Parameters.Length];
        //    //    for (int i = 0; i < m_Parameters.Length; i++)
        //    //    {
        //    //        m_ParsedParameters[i] = m_Parameters[i].GetParamReference(m_Audio.eventDescription);
        //    //    }
        //    //}
        //    //else m_ParsedParameters = Array.Empty<ParamReference>();
        //}
        public override void OnStateAwake()
        {
            m_Audio = m_AudioReference.GetAudio();
        }
        public override void OnStateBegin()
        {
            m_Audio.Play();
        }
    }
}

#endif