using UnityEngine;
using UnityEngine.Rendering;

namespace Kino.PostProcessing
{
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable, VolumeComponentMenu("Post-processing/Kino/Sharpen")]
    public sealed class Sharpen : PostProcessVolumeComponent
    {
        public ClampedFloatParameter intensity = new(0, 0, 1);

        private static class ShaderIDs
        {
            internal static readonly int SharpenIntensity = Shader.PropertyToID("_SharpenIntensity");
        }

        public override bool IsActive() => intensity.value > 0;

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            material ??= CoreUtils.CreateEngineMaterial(data.shaders.SharpenPS);
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            // if (m_Material == null) return;

            material.SetFloat(ShaderIDs.SharpenIntensity, intensity.value);
            cmd.SetPostProcessInputTexture(srcRT);
            cmd.DrawFullScreenTriangle(material, destRT);
        }
    }
}