using UnityEngine;
using UnityEngine.Rendering;

namespace Kino.PostProcessing
{
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable, VolumeComponentMenu("Post-processing/Kino/Test Card")]
    public sealed class TestCard : PostProcessVolumeComponent
    {
        private static class ShaderIDs
        {
            internal static readonly int TestCardOpacity = Shader.PropertyToID("_TestCardOpacity");
        }

        public ClampedFloatParameter opacity = new(0, 0, 1);

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override bool IsActive() => opacity.value > 0;

        public override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            material ??= CoreUtils.CreateEngineMaterial(data.shaders.TestCardPS);
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            material.SetFloat(ShaderIDs.TestCardOpacity, opacity.value);
            cmd.SetPostProcessInputTexture(srcRT);
            cmd.DrawFullScreenTriangle(material, destRT);
        }
    }
}