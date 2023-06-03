using UnityEngine.Rendering.Universal;

namespace Kino.PostProcessing
{
    using UnityEngine;
    using UnityEngine.Assertions.Comparers;
    using UnityEngine.Rendering;
    using static KinoCore;
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable]
    [VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Utility : PostProcessVolumeComponent
    {
        public ClampedFloatParameter saturation = new(1, 0, 2);
        public ClampedFloatParameter hueShift = new(0, -1, 1);
        public ClampedFloatParameter invert = new(0, 0, 1);
        public ColorParameter fade = new(new Color(0, 0, 0, 0), false, true, true);

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        private FloatComparer _floatComparer = new();

        public override bool IsActive() =>
            !_floatComparer.Equals(saturation.value, 1)
            || !_floatComparer.Equals(hueShift.value, 0)
            || invert.value > 0
            || fade.value.a > 0;
        
        public override void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            if (m_Material == null) return;

            m_Material.SetColor(ShaderIDs.FadeColor, fade.value);
            m_Material.SetFloat(ShaderIDs.HueShift, hueShift.value);
            m_Material.SetFloat(ShaderIDs.Invert, invert.value);
            m_Material.SetFloat(ShaderIDs.Saturation, saturation.value);

            cmd.SetPostProcessSourceTexture(srcRT);
            cmd.DrawFullScreenTriangle(m_Material, destRT);
        }
    }
}