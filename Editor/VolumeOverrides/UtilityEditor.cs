namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [VolumeComponentEditor(typeof(Utility))]
    sealed class UtilityEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Saturation;
        SerializedDataParameter m_HueShift;
        SerializedDataParameter m_Invert;
        SerializedDataParameter m_Fade;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Utility>(serializedObject);

            m_Saturation = Unpack(o.Find(x => x.saturation));
            m_HueShift   = Unpack(o.Find(x => x.hueShift));
            m_Invert     = Unpack(o.Find(x => x.invert));
            m_Fade       = Unpack(o.Find(x => x.fade));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Saturation);
            PropertyField(m_HueShift);
            PropertyField(m_Invert);
            PropertyField(m_Fade);
        }
    }
}