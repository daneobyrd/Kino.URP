namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [VolumeComponentEditor(typeof(Overlay))]
    sealed class OverlayEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SourceType;
        SerializedDataParameter m_BlendMode;
        SerializedDataParameter m_Opacity;
        SerializedDataParameter m_Color;
        SerializedDataParameter m_Gradient;
        SerializedDataParameter m_Angle;
        SerializedDataParameter m_Texture;
        SerializedDataParameter m_SourceAlpha;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Overlay>(serializedObject);
            
            m_SourceType     = Unpack(o.Find(x => x.sourceType));
            m_BlendMode      = Unpack(o.Find(x => x.blendMode));
            m_Opacity        = Unpack(o.Find(x => x.opacity));
            m_Color          = Unpack(o.Find(x => x.color));
            m_Gradient       = Unpack(o.Find(x => x.gradient));
            m_Angle          = Unpack(o.Find(x => x.angle));
            m_Texture        = Unpack(o.Find(x => x.texture));
            m_SourceAlpha    = Unpack(o.Find(x => x.sourceAlpha));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_SourceType);

            var sourceType = (SourceType)m_SourceType.value.enumValueIndex;

            if (sourceType == SourceType.Color)
            {
                PropertyField(m_Color);
            }
            else if (sourceType == SourceType.Gradient)
            {
                PropertyField(m_Gradient);
                PropertyField(m_Angle);
            }
            else // Overlay.SourceType.Texture
            {
                PropertyField(m_Texture);
                PropertyField(m_SourceAlpha);
            }

            PropertyField(m_BlendMode);
            PropertyField(m_Opacity);
        }
    }
}
