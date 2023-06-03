using UnityEngine;

namespace Kino.PostProcessing
{
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using static KinoCore;
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable]
    [VolumeComponentMenu("Post-processing/Kino/Test Card")]
    public sealed class TestCard : PostProcessVolumeComponent
    {
        public ClampedFloatParameter opacity = new(0, 0, 1);

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override bool IsActive() => opacity.value > 0;

        public override void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            m_Material.SetFloat(ShaderIDs.TestCardOpacity, opacity.value);
            cmd.SetPostProcessSourceTexture(srcRT);
            cmd.DrawFullScreenTriangle(m_Material, destRT);
        }
    }
}