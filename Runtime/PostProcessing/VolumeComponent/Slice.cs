using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kino.PostProcessing
{
    using static KinoCore;
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable]
    [VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Slice : PostProcessVolumeComponent
    {
        public FloatParameter rowCount = new(30);
        public ClampedFloatParameter angle = new(0, -90, 90);
        public ClampedFloatParameter displacement = new(0, -1, 1);
        public IntParameter randomSeed = new(0);

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override bool IsActive() => displacement.value != 0;

        public override void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            var rad = angle.value * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var seed = (uint) randomSeed.value;
            seed = (seed << 16) | (seed >> 16);

            m_Material.SetVector(ShaderIDs.SliceDirection, dir);
            m_Material.SetFloat(ShaderIDs.Displacement, displacement.value);
            cmd.SetPostProcessSourceTexture(srcRT);
            m_Material.SetFloat(ShaderIDs.Rows, rowCount.value);
            m_Material.SetInt(ShaderIDs.SliceSeed, (int) seed);

            cmd.DrawFullScreenTriangle(m_Material, destRT);
        }
    }
}