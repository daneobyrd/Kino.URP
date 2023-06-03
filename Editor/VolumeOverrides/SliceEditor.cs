namespace Kino.PostProcessing
{
    using UnityEditor;
    using UnityEditor.Rendering;

    [VolumeComponentEditor(typeof(Slice))]
    sealed class SliceEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_RowCount;
        SerializedDataParameter m_Angle;
        SerializedDataParameter m_Displacement;
        SerializedDataParameter m_RandomSeed;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Slice>(serializedObject);

            m_RowCount     = Unpack(o.Find(x => x.rowCount));
            m_Angle        = Unpack(o.Find(x => x.angle));
            m_Displacement = Unpack(o.Find(x => x.displacement));
            m_RandomSeed   = Unpack(o.Find(x => x.randomSeed));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_RowCount);
            PropertyField(m_Angle);
            PropertyField(m_Displacement);
            PropertyField(m_RandomSeed);
        }
    }
}