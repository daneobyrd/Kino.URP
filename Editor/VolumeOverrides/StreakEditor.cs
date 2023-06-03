namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [VolumeComponentEditor(typeof(Streak))]
    sealed class StreakEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_Stretch;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Tint;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Streak>(serializedObject);

            m_Threshold      = Unpack(o.Find(x => x.threshold));
            m_Stretch        = Unpack(o.Find(x => x.stretch));
            m_Intensity      = Unpack(o.Find(x => x.intensity));
            m_Tint           = Unpack(o.Find(x => x.tint));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Threshold);
            PropertyField(m_Stretch);
            PropertyField(m_Intensity);
            PropertyField(m_Tint);
        }
    }
}