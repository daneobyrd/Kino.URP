using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CustomPostProcessing.UniversalRP;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
    [Serializable, VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Slice : CustomPostProcessVolumeComponent
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

        public override bool IsActive() => displacement.value != 0;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        protected override void Setup(ScriptableObject resourceData)
        {
            var data = (KinoPostProcessData) resourceData;
            Initialize(data.shaders.SlicePS);
        }

        public override void Render(CommandBuffer cmd, CameraData unused, RTHandle srcRT, RTHandle destRT)
        {
            var rad = angle.value * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var seed = (uint) randomSeed.value;
            seed = (seed << 16) | (seed >> 16);

            material.SetVector(ShaderIDs.SliceDirection, dir);
            material.SetFloat(ShaderIDs.Displacement, displacement.value);
            material.SetFloat(ShaderIDs.Rows, rowCount.value);
            material.SetInt(ShaderIDs.SliceSeed, (int) seed);
            cmd.SetPostProcessInputTexture(srcRT);
            material.DrawFullScreen(cmd, srcRT, destRT);
        }
    }
}