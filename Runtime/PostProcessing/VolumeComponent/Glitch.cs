using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Kino.PostProcessing.KinoCore;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
    [Serializable]
    [VolumeComponentMenu("Post-processing/Kino/Glitch")]
    public sealed class Glitch : PostProcessVolumeComponent
    {
        public ClampedFloatParameter block = new(0, 0, 1);
        public ClampedFloatParameter drift = new(0, 0, 1);
        public ClampedFloatParameter jitter = new(0, 0, 1);
        public ClampedFloatParameter jump = new(0, 0, 1);
        public ClampedFloatParameter shake = new(0, 0, 1);
        
        public override InjectionPoint InjectionPoint => InjectionPoint.AfterPostProcess;

        public override bool IsActive()
        {
            return block.value > 0 ||
                   drift.value > 0 ||
                   jitter.value > 0 ||
                   jump.value > 0 ||
                   shake.value > 0;
        }

        float _prevTime;
        float _jumpTime;

        float _blockTime;
        int _blockSeed1 = 71;
        int _blockSeed2 = 113;
        int _blockStride = 1;

        public override void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier source, RenderTargetIdentifier dest)
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
            m_Material.SetInt(ShaderIDs.GlitchSeed, (int) (time * 10000));
            m_Material.SetFloat(ShaderIDs.BlockStrength, block3);
            m_Material.SetInt(ShaderIDs.BlockStride, _blockStride);
            m_Material.SetInt(ShaderIDs.BlockSeed1, _blockSeed1);
            m_Material.SetInt(ShaderIDs.BlockSeed2, _blockSeed2);
            m_Material.SetVector(ShaderIDs.Drift, vdrift);
            m_Material.SetVector(ShaderIDs.Jitter, vjitter);
            m_Material.SetVector(ShaderIDs.Jump, vjump);
            m_Material.SetFloat(ShaderIDs.Shake, shake.value * 0.2f);
            cmd.SetPostProcessSourceTexture(source);

            // Shader pass number
            var pass = 0;
            if (drift.value > 0 || jitter.value > 0 || jump.value > 0 || shake.value > 0) pass += 1;
            if (block.value > 0) pass                                                          += 2;

            // Blit
            cmd.DrawFullScreenTriangle(m_Material, dest, pass);
        }
    }
}