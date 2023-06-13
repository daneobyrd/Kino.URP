using UnityEngine;
using UnityEngine.Rendering;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
    [Serializable, VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Slice : PostProcessVolumeComponent
    {
        public FloatParameter rowCount = new(30);
        public ClampedFloatParameter angle = new(0, -90, 90);
        public ClampedFloatParameter displacement = new(0, -1, 1);
        public IntParameter randomSeed = new(0);
        
        private static class ShaderIDs
        {
            internal static readonly int SliceDirection = Shader.PropertyToID("_SliceDirection");
            internal static readonly int Displacement = Shader.PropertyToID("_Displacement");
            internal static readonly int Rows = Shader.PropertyToID("_Rows");

            internal static readonly int SliceSeed = Shader.PropertyToID("_SliceSeed");
        }
        
        public override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            material ??= CoreUtils.CreateEngineMaterial(data.shaders.SlicePS);
        }

        public override bool IsActive() => displacement.value != 0;

        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier srcRT, RenderTargetIdentifier destRT)
        {
            var rad = angle.value * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var seed = (uint) randomSeed.value;
            seed = (seed << 16) | (seed >> 16);

            material.SetVector(ShaderIDs.SliceDirection, dir);
            material.SetFloat(ShaderIDs.Displacement, displacement.value);
            cmd.SetPostProcessInputTexture(srcRT);
            material.SetFloat(ShaderIDs.Rows, rowCount.value);
            material.SetInt(ShaderIDs.SliceSeed, (int) seed);

            cmd.DrawFullScreenTriangle(material, destRT);
        }
    }
}