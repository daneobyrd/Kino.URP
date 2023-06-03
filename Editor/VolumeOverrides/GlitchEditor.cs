namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [VolumeComponentEditor(typeof(Glitch))]
    sealed class GlitchEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Block;
        SerializedDataParameter m_Drift;
        SerializedDataParameter m_Jitter;
        SerializedDataParameter m_Jump;
        SerializedDataParameter m_Shake;
        
        public override void OnEnable()
        {
            var o = new PropertyFetcher<Glitch>(serializedObject);
            
            m_Block          = Unpack(o.Find(x => x.block));
            m_Drift          = Unpack(o.Find(x => x.drift));
            m_Jitter         = Unpack(o.Find(x => x.jitter));
            m_Jump           = Unpack(o.Find(x => x.jump));
            m_Shake          = Unpack(o.Find(x => x.shake));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Block);
            PropertyField(m_Drift);
            PropertyField(m_Jitter);
            PropertyField(m_Jump);
            PropertyField(m_Shake);
        }
    }
}