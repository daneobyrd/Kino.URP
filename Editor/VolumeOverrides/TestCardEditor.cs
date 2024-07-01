using CustomPostProcessing.UniversalRP.Editor;

namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [CustomEditor(typeof(TestCard))]
    sealed class TestCardEditor : CustomPostProcessVolumeComponentEditor
    {
        SerializedDataParameter m_Opacity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<TestCard>(serializedObject);

            m_Opacity        = Unpack(o.Find(x => x.opacity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Opacity);
        }
    }
}