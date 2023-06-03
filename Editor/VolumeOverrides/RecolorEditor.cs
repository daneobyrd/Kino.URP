namespace Kino.PostProcessing.Editor
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Rendering;

    [VolumeComponentEditor(typeof(Recolor))]
    public sealed class RecolorEditor : VolumeComponentEditor
    {
        static class Labels
        {
            internal static readonly GUIContent Source    = new GUIContent("Source");
            internal static readonly GUIContent Threshold = new GUIContent("Threshold");
            internal static readonly GUIContent Contrast  = new GUIContent("Contrast");
            internal static readonly GUIContent Color     = new GUIContent("Color");
            internal static readonly GUIContent Gradient  = new GUIContent("Gradient");
            internal static readonly GUIContent Opacity   = new GUIContent("Opacity");
            internal static readonly GUIContent Type      = new GUIContent("Type");
            internal static readonly GUIContent Strength  = new GUIContent("Strength");
        }
        
        SerializedDataParameter m_EdgeSource;
        SerializedDataParameter m_EdgeThreshold;
        SerializedDataParameter m_EdgeContrast;
        SerializedDataParameter m_EdgeColor;
        SerializedDataParameter m_FillGradient;
        SerializedDataParameter m_FillOpacity;
        SerializedDataParameter m_DitherType;
        SerializedDataParameter m_DitherStrength;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Recolor>(serializedObject);
            
            m_EdgeColor      = Unpack(o.Find(x => x.edgeColor));
            m_EdgeSource     = Unpack(o.Find(x => x.edgeSource));
            m_EdgeThreshold  = Unpack(o.Find(x => x.edgeThreshold));
            m_EdgeContrast   = Unpack(o.Find(x => x.edgeContrast));
            m_FillGradient   = Unpack(o.Find(x => x.fillGradient));
            m_FillOpacity    = Unpack(o.Find(x => x.fillOpacity));
            m_DitherType     = Unpack(o.Find(x => x.ditherType));
            m_DitherStrength = Unpack(o.Find(x => x.ditherStrength));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Edge", EditorStyles.miniLabel);

            PropertyField(m_EdgeColor, Labels.Color);
            PropertyField(m_EdgeSource, Labels.Source);
            PropertyField(m_EdgeThreshold, Labels.Threshold);
            PropertyField(m_EdgeContrast, Labels.Contrast);

            EditorGUILayout.LabelField("Fill", EditorStyles.miniLabel);

            PropertyField(m_FillGradient, Labels.Gradient);
            PropertyField(m_FillOpacity, Labels.Opacity);

            EditorGUILayout.LabelField("Dithering", EditorStyles.miniLabel);

            PropertyField(m_DitherType, Labels.Type);
            PropertyField(m_DitherStrength, Labels.Strength);
        }
    }
}
