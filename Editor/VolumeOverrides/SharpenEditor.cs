namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [VolumeComponentEditor(typeof(Sharpen))]
    sealed class SharpenEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Intensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Sharpen>(serializedObject);

            m_Intensity      = Unpack(o.Find(x => x.intensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Intensity);
        }
    }
}