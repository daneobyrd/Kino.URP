using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
    using static CustomPostProcessUtils;

    [Serializable, VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Streak : PostProcessVolumeComponent
    {
        public ClampedFloatParameter threshold = new(1, 0, 5);
        public ClampedFloatParameter stretch = new(0.75f, 0, 1);
        public ClampedFloatParameter intensity = new(0, 0, 1);
        public ColorParameter tint = new(new Color(0.55f, 0.55f, 1), false, false, true);
        public override bool IsActive() => intensity.value > 0;

        public override InjectionPoint InjectionPoint => InjectionPoint.BeforePostProcess;

        #region Private members

        private static class ShaderIDs
        {
            internal static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");

            internal static readonly int StreakColor = Shader.PropertyToID("_StreakColor");
            internal static readonly int SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
            internal static readonly int StreakIntensity = Shader.PropertyToID("_StreakIntensity");
            internal static readonly int Stretch = Shader.PropertyToID("_Stretch");

            internal static readonly int Threshold = Shader.PropertyToID("_Threshold");
        }

        // Image pyramid storage
        // We have to use different pyramids for each camera, so we use a
        // dictionary and camera GUIDs as a key to store each pyramid.
        private Dictionary<int, StreakPyramid> _pyramids;
        private const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat;
        private CameraData _cameraData;
        private CommandBuffer _commandBuffer;

        #endregion
        
        public void SetCameraData(CameraData cameraData) { _cameraData = cameraData; }

        private StreakPyramid GetPyramid(ref CommandBuffer cmd)
        {
            var cameraID = _cameraData.camera.GetInstanceID();
            _cameraData.cameraTargetDescriptor.graphicsFormat = RTFormat;


            if (_pyramids.TryGetValue(cameraID, out var candid))
            {
                // Reallocate the RTs when the screen size was changed.
                if (!candid.CheckSize(_cameraData.cameraTargetDescriptor))
                    candid.SetupShaderIDs();
                candid.Reallocate(cmd, _cameraData.cameraTargetDescriptor);
            }
            else
            {
                // No one found: Allocate a new pyramid.
                _pyramids[cameraID] = candid = new StreakPyramid(cmd, _cameraData.cameraTargetDescriptor);
            }

            return candid;
        }

        public override void Setup(ScriptableObject scriptableObject)
        {
            var data = (KinoPostProcessData) scriptableObject;
            material  ??= CoreUtils.CreateEngineMaterial(data.shaders.StreakPS);
            _pyramids =   new Dictionary<int, StreakPyramid>();
        }

        // -------Prefilter--------
        // Source -> Prefilter Shader -> _mips[0].down

        // -------Downsample-------
        // _mips[0].down -> _mips[1].down
        // _mips[1].down -> _mips[2].down
        // ...
        // _mips[6].down -> _mips[7].down

        // -------Upsample---------
        // _mips[7].down -> _mips[6].up
        // _mips[6].up   -> _mips[5].up
        // ...
        // _mips[2].up   -> _mips[1].up

        // -------Composite--------
        // _mips[1].up   - Destination

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            _commandBuffer = cmd;
            var pyramid = GetPyramid(ref cmd);

            float linearThreshold = Mathf.GammaToLinearSpace(threshold.value);

            #region Set Shader Properties

            // Common parameters
            material.SetFloat(ShaderIDs.Threshold, linearThreshold);
            material.SetFloat(ShaderIDs.Stretch, stretch.value);
            material.SetFloat(ShaderIDs.StreakIntensity, intensity.value);
            material.SetColor(ShaderIDs.StreakColor, tint.value);
            cmd.SetGlobalTexture(ShaderIDs.SourceTexture, source);

            #endregion

            // ShaderPass Indices
            const int prefilterPass = 0;
            const int downsamplePass = 1;
            const int upsamplePass = 2;
            const int compositePass = 3;

            //
            // Prefilter
            //
            // Source -> Prefilter -> _mips[0]
            var prefilterDestination = pyramid[0].down;
            PostProcessBlit(cmd, prefilterDestination, material, prefilterPass);

            //
            // Downsample
            //
            var lastDown = prefilterDestination;
            var mipLevel = 1;
            for (; mipLevel < pyramid.MipCount; mipLevel++)
            {
                // lastDown = pyramid[i-1].down;
                var mipDown = pyramid[mipLevel].down;

                cmd.SetPostProcessInputTexture(lastDown);
                PostProcessBlit(cmd, mipDown, material, downsamplePass);

                // set with used mipDown 
                lastDown = mipDown;
            }

            var lastRT = lastDown;

            //
            // Upsample & combine
            //
            var maxMipLevel = pyramid.MipCount - 1;
            for (; mipLevel >= 0; mipLevel--)
            {
                var isFirstUpsample = (mipLevel == maxMipLevel);

                var mip = pyramid[mipLevel];

                int lowMip =
                    isFirstUpsample
                        ? pyramid[mipLevel + 1].down
                        : pyramid[mipLevel + 1].up;
                int highMip = mip.down;
                int dst = mip.up;

                cmd.SetGlobalTexture(ShaderIDs.SourceTexLowMip, lowMip);
                cmd.SetPostProcessInputTexture(highMip);
                cmd.DrawFullScreenTriangle(material, dst, upsamplePass);
                lastRT = dst;
            }

            // Final composition
            cmd.SetPostProcessInputTexture(lastRT);
            cmd.DrawFullScreenTriangle(material, destination, compositePass);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            foreach (var pyramid in _pyramids.Values) pyramid.Release(_commandBuffer);
        }

        #region Image pyramid class used in Streak effect

        public sealed class StreakPyramid
        {
            private const int MaxMipLevel = 16;

            private int _baseWidth;
            private int _baseHeight;
            private int _maxSize;
            private int _iterations;
            public int MipCount; // default

            readonly (int down, int up)[] _mips = new (int, int) [MaxMipLevel];

            public (int down, int up) this[int index] => _mips[index];

            public StreakPyramid(CommandBuffer cmd, RenderTextureDescriptor descriptor)
            {
                SetMipCount(descriptor);
                SetupShaderIDs();
                Allocate(cmd, descriptor);
            }

            private void SetMipCount(RenderTextureDescriptor descriptor)
            {
                _baseWidth  =   Mathf.Max(descriptor.width, 1);
                _baseHeight =   Mathf.Max(descriptor.height, 1);
                
                _baseWidth  >>= 1;
                _baseHeight >>= 1;

                _maxSize = Mathf.Max(_baseWidth, _baseHeight >> 1);

                // Determine the iteration count
                // 1920 = 9, 2560 = 10, 3840 = 10
                _iterations = Mathf.FloorToInt(Mathf.Log(_maxSize, 2f) - 1);
                MipCount    = Mathf.Clamp(_iterations, 1, MaxMipLevel);
                Debug.Log($"{nameof(_iterations)}: {_iterations}");
                Debug.Log($"{nameof(MipCount)}: {MipCount}");
            }

            public bool CheckSize(RenderTextureDescriptor descriptor)
            {
                return _baseWidth == Mathf.FloorToInt(descriptor.width) &&
                       _baseHeight == Mathf.FloorToInt(descriptor.height);
            }

            public void Reallocate(CommandBuffer cmd, RenderTextureDescriptor descriptor)
            {
                SetMipCount(descriptor);
                Release(cmd);
                SetupShaderIDs();
                Allocate(cmd, descriptor);
            }

            public void Release(CommandBuffer cmd)
            {
                for (var i = 0; i < MipCount; i++)
                {
                    cmd.ReleaseTemporaryRT(_mips[i].down);
                    cmd.ReleaseTemporaryRT(_mips[i].up);
                    // Clear _mips int values
                    _mips[i] = (-1, -1);
                }
            }

            public void SetupShaderIDs()
            {
                for (var i = 0; i < _mips.Length; i++)
                {
                    _mips[i] = (Shader.PropertyToID("_StreakMipDown" + i),
                                Shader.PropertyToID("_StreakMipUp" + i));
                }
            }

            void Allocate(CommandBuffer cmd, RenderTextureDescriptor descriptor)
            {
                // Start at half-res
                int width = _baseWidth;
                int height = _baseHeight >> 1;

                descriptor.width  = width;
                descriptor.height = height;

                cmd.GetTemporaryRT(_mips[0].down, descriptor);
                cmd.GetTemporaryRT(_mips[0].up, descriptor);

                // should break when (width < 4)
                for (var i = 1; i < MipCount; i++)
                {
                    width = Mathf.Max(1, width >> 1);

                    descriptor.width  = width;
                    descriptor.height = height;

                    cmd.GetTemporaryRT(_mips[i].down, descriptor);
                    cmd.GetTemporaryRT(_mips[i].up, descriptor);
                }
            }
        }

        #endregion
    }
}