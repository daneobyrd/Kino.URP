using UnityEngine;

namespace Kino.PostProcessing
{
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using static KinoCore;
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable]
    [VolumeComponentMenu("Post-processing/Kino/Sharpen")]
    public sealed class Sharpen : PostProcessVolumeComponent
    {
        public ClampedFloatParameter intensity = new(0, 0, 1);

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override bool IsActive() => intensity.value > 0;
        public override void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            if (m_Material == null) return;

            m_Material.SetFloat(ShaderIDs.SharpenIntensity, intensity.value);
            cmd.SetPostProcessSourceTexture(srcRT);
            cmd.DrawFullScreenTriangle(m_Material, destRT);
        }
    }
}