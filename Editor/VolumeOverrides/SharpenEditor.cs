using CustomPostProcessing.UniversalRP.Editor;

namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [CustomEditor(typeof(Sharpen))]
    sealed class SharpenEditor : CustomPostProcessVolumeComponentEditor
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