using UnityEngine;
using UnityEngine.Rendering;

namespace Kino.PostProcessing
{
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable, VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Utility : PostProcessVolumeComponent
    {

        public ClampedFloatParameter saturation = new(1, 0, 2);
        public ClampedFloatParameter hueShift = new(0, -1, 1);
        public ClampedFloatParameter invert = new(0, 0, 1);
        public ColorParameter fade = new(new Color(0, 0, 0, 0), false, true, true);

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        private static class ShaderIDs
        {
            internal static readonly int FadeColor = Shader.PropertyToID("_FadeColor");
            internal static readonly int HueShift = Shader.PropertyToID("_HueShift");
            internal static readonly int Invert = Shader.PropertyToID("_Invert");

            internal static readonly int Saturation = Shader.PropertyToID("_Saturation");
        }

        public override bool IsActive() =>
            !Mathf.Approximately(saturation.value, 1)
            || !Mathf.Approximately(hueShift.value, 0)
            || invert.value > 0
            || fade.value.a > 0;

        public override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            material = CoreUtils.CreateEngineMaterial(data.shaders.UtilityPS);
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            if (material == null) return;

            material.SetColor(ShaderIDs.FadeColor, fade.value);
            material.SetFloat(ShaderIDs.HueShift, hueShift.value);
            material.SetFloat(ShaderIDs.Invert, invert.value);
            material.SetFloat(ShaderIDs.Saturation, saturation.value);

            cmd.SetPostProcessInputTexture(srcRT);
            cmd.DrawFullScreenTriangle(material, destRT);
        }
    }
}