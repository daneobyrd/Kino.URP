using UnityEngine;
using UnityEngine.Rendering;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
    [Serializable, VolumeComponentMenu("Post-processing/Kino/Glitch")]
    public sealed class Glitch : PostProcessVolumeComponent
    {
        public ClampedFloatParameter block = new(0, 0, 1);
        public ClampedFloatParameter drift = new(0, 0, 1);
        public ClampedFloatParameter jitter = new(0, 0, 1);
        public ClampedFloatParameter jump = new(0, 0, 1);
        public ClampedFloatParameter shake = new(0, 0, 1);
        
        #region Private members
        
        float _prevTime;
        float _jumpTime;

        float _blockTime;
        int _blockSeed1 = 71;
        int _blockSeed2 = 113;
        int _blockStride = 1;
        
        private static class ShaderIDs
        {
            internal static readonly int GlitchSeed = Shader.PropertyToID("_GlitchSeed");
            internal static readonly int BlockSeed1 = Shader.PropertyToID("_BlockSeed1");
            internal static readonly int BlockSeed2 = Shader.PropertyToID("_BlockSeed2");
            internal static readonly int BlockStrength = Shader.PropertyToID("_BlockStrength");
            internal static readonly int BlockStride = Shader.PropertyToID("_BlockStride");
            internal static readonly int Drift = Shader.PropertyToID("_Drift");
            internal static readonly int Jitter = Shader.PropertyToID("_Jitter");
            internal static readonly int Jump = Shader.PropertyToID("_Jump");
            internal static readonly int Shake = Shader.PropertyToID("_Shake");
        }
        
        #endregion
        
        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override bool IsActive()
        {
            return block.value > 0 ||
                   drift.value > 0 ||
                   jitter.value > 0 ||
                   jump.value > 0 ||
                   shake.value > 0;
        }

        public override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            material ??= CoreUtils.CreateEngineMaterial(data.shaders.GlitchPS);
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            // if (m_Material == null) return;

            // Update the time parameters.
            var time = Time.time;
            var delta = time - _prevTime;
            _jumpTime += delta * jump.value * 11.3f;
            _prevTime =  time;

            // Block parameters
            var block3 = block.value * block.value * block.value;

            // Shuffle block parameters every 1/30 seconds.
            _blockTime += delta * 60;
            if (_blockTime > 1)
            {
                if (Random.value < 0.09f) _blockSeed1  += 251;
                if (Random.value < 0.29f) _blockSeed2  += 373;
                if (Random.value < 0.25f) _blockStride =  Random.Range(1, 32);
                _blockTime = 0;
            }

            // Drift parameters (time, displacement)
            var vdrift = new Vector2
            (
                time * 606.11f % (Mathf.PI * 2),
                drift.value * 0.04f
            );

            // Jitter parameters (threshold, displacement)
            var jv = jitter.value;
            var vjitter = new Vector3
            (
                Mathf.Max(0, 1.001f - jv * 1.2f),
                0.002f + jv * jv * jv * 0.05f
            );

            // Jump parameters (scroll, displacement)
            var vjump = new Vector2(_jumpTime, jump.value);

            // Invoke the shader.
            material.SetInt(ShaderIDs.GlitchSeed, (int) (time * 10000));
            material.SetFloat(ShaderIDs.BlockStrength, block3);
            material.SetInt(ShaderIDs.BlockStride, _blockStride);
            material.SetInt(ShaderIDs.BlockSeed1, _blockSeed1);
            material.SetInt(ShaderIDs.BlockSeed2, _blockSeed2);
            material.SetVector(ShaderIDs.Drift, vdrift);
            material.SetVector(ShaderIDs.Jitter, vjitter);
            material.SetVector(ShaderIDs.Jump, vjump);
            material.SetFloat(ShaderIDs.Shake, shake.value * 0.2f);
            cmd.SetPostProcessInputTexture(source);

            // Shader pass number
            var pass = 0;
            if (drift.value > 0 || jitter.value > 0 || jump.value > 0 || shake.value > 0) pass += 1;
            if (block.value > 0) pass                                                          += 2;

            // Blit
            cmd.DrawFullScreenTriangle(material, destination, pass);
        }
    }
}