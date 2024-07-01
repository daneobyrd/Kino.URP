using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CustomPostProcessing.UniversalRP;

namespace Kino.PostProcessing
{
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable, VolumeComponentMenu("Post-processing/Kino/Sharpen")]
    public sealed class Sharpen : CustomPostProcessVolumeComponent
    {
        public ClampedFloatParameter intensity = new(0, 0, 1);

        private static class ShaderIDs
        {
            internal static readonly int SharpenIntensity = Shader.PropertyToID("_SharpenIntensity");
        }

        public override bool IsActive() => intensity.value > 0;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        protected override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            Initialize(data.shaders.SharpenPS);
        }

        public override void Render(CommandBuffer cmd, CameraData unused, RTHandle srcRT, RTHandle destRT)
        {
            if (material == null) return;

            material.SetFloat(ShaderIDs.SharpenIntensity, intensity.value);
            cmd.SetPostProcessInputTexture(srcRT);
            material.DrawFullScreen(cmd, srcRT, destRT);
        }

        public override void Cleanup()
        {
            // if (material)
            // {
            //     CoreUtils.Destroy(material);
            // }
        }
    }
}