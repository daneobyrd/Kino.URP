using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CustomPostProcessing.UniversalRP;

namespace Kino.PostProcessing
{
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable, VolumeComponentMenu("Post-processing/Kino/Test Card")]
    public sealed class TestCard : CustomPostProcessVolumeComponent
    {
        private static class ShaderIDs
        {
            internal static readonly int TestCardOpacity = Shader.PropertyToID("_TestCardOpacity");
        }

        public ClampedFloatParameter opacity = new(0, 0, 1);

        public override bool IsActive() => opacity.value > 0;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        protected override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            Initialize(data.shaders.TestCardPS);
        }

        public override void Render(CommandBuffer cmd, CameraData unused, RTHandle srcRT, RTHandle destRT)
        {
            material.SetFloat(ShaderIDs.TestCardOpacity, opacity.value);
            cmd.SetPostProcessInputTexture(srcRT);
            material.DrawFullScreen(cmd, srcRT, destRT);
        }
    }
}