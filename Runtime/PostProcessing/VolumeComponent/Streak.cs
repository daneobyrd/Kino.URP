using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using CustomPostProcessing.UniversalRP;
using SerializableAttribute = System.SerializableAttribute;

namespace Kino.PostProcessing
{
    [Serializable, VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Streak : CustomPostProcessVolumeComponent
    {
        public ClampedFloatParameter threshold = new(1, 0, 5);
        public ClampedFloatParameter stretch = new(0.75f, 0, 1);
        public ClampedFloatParameter intensity = new(0, 0, 1);
        public ColorParameter tint = new(new Color(0.55f, 0.55f, 1), false, false, true);

        public override bool IsActive() => intensity.value > 0;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        #region Private members

        private static class ShaderIDs
        {
            internal static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");
            // internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");
            internal static readonly int StreakColor = Shader.PropertyToID("_StreakColor");
            internal static readonly int HighTexture = Shader.PropertyToID("_HighTexture");
            internal static readonly int StreakIntensity = Shader.PropertyToID("_StreakIntensity");
            internal static readonly int Stretch = Shader.PropertyToID("_Stretch");
            internal static readonly int Threshold = Shader.PropertyToID("_Threshold");
        }

        // Image pyramid storage
        // We have to use different pyramids for each camera, so we use a
        // dictionary and camera GUIDs as a key to store each pyramid.
        private Dictionary<int, StreakPyramid> _pyramids;
        private const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat;

        #endregion

        private StreakPyramid GetPyramid(CameraData cameraData)
        {
            var cameraID = cameraData.camera.GetInstanceID();
            cameraData.cameraTargetDescriptor.graphicsFormat = RTFormat;

            if (_pyramids.TryGetValue(cameraID, out var candid))
            {
                // Reallocate the RTs when the screen size was changed.
                if (!candid.CheckSize(cameraData))
                {
                    candid.Reallocate(cameraData);
                }
            }
            else
            {
                // _pyramids.Clear();
                // No one found: Allocate a new pyramid.
                _pyramids[cameraID] = candid = new StreakPyramid(cameraData);
            }

            return candid;
        }

        protected override void Setup(ScriptableObject resourceData)
        {
            _pyramids = new Dictionary<int, StreakPyramid>();

            var data = (KinoPostProcessData) resourceData;
            Initialize(data.shaders.StreakPS);
        }

        // ShaderPass Indices
        private const int PrefilterPassIndex = 0;
        private const int DownsamplePassIndex = 1;
        private const int UpsamplePassIndex = 2;
        private const int CompositePassIndex = 3;

        // _mips[0].up and the last non-null mip.up are not used.
        // See in the following example: _mips[7].up is not used.

        // -------Prefilter--------
        // Source -> Prefilter Shader -> _mips[0].down

        // -------Downsample-------
        // _mips[0].down -> _mips[1].down
        // _mips[1].down -> _mips[2].down
        // ...continue
        // _mips[6].down -> _mips[7].down

        // -------Upsample---------
        // _mips[7].down -> _mips[6].up
        // _mips[6].up   -> _mips[5].up
        // ...continue
        // _mips[2].up   -> _mips[1].up

        // -------Composite--------
        // _mips[1].up   -> Destination

        public override void Render(CommandBuffer cmd, CameraData cameraData, RTHandle source, RTHandle destination)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Streak")))
            {
                var pyramid = GetPyramid(cameraData);

                // float linearThreshold = Mathf.GammaToLinearSpace(threshold.value);

                // Common parameters
                material.SetFloat(ShaderIDs.Threshold, threshold.value);
                material.SetFloat(ShaderIDs.Stretch, stretch.value);
                material.SetFloat(ShaderIDs.StreakIntensity, intensity.value);
                material.SetColor(ShaderIDs.StreakColor, tint.value);

                //
                // Prefilter
                //
                // Source -> Prefilter -> MIP 0
                material.SetTexture(ShaderIDs.SourceTexture, source);
                var prefilterDestination = pyramid[0].down;

                using (new ProfilingScope(cmd, new ProfilingSampler("Prefilter")))
                {
                    material.DrawFullScreen(cmd, source, prefilterDestination, pass: PrefilterPassIndex);
                }

                //
                // Downsample
                //
                var prevMipDown = prefilterDestination;
                var mipLevel = 1;
                using (new ProfilingScope(cmd, new ProfilingSampler("Downsample")))
                {
                    for (; mipLevel < StreakPyramid.MaxMipLevel && pyramid[mipLevel].down != null; mipLevel++)
                    {
                        var mipDown = pyramid[mipLevel].down;

                        cmd.SetPostProcessInputTexture(prevMipDown);
                        material.DrawFullScreen(cmd, prevMipDown, mipDown, DownsamplePassIndex);

                        prevMipDown = mipDown;
                    }
                }

                // Set last render target as final downsample target
                // var lastRT = prevMipDown;
                var lastRT = pyramid[--mipLevel].down;
                //
                // Upsample & combine
                //
                using (new ProfilingScope(cmd, new ProfilingSampler("Upsample")))
                {
                    for (mipLevel--; mipLevel >= 1; mipLevel--)
                    {
                        var mip = pyramid[mipLevel];

                        cmd.SetPostProcessInputTexture(lastRT);
                        material.SetTexture(ShaderIDs.HighTexture, mip.down);
                        material.DrawFullScreen(cmd, lastRT, mip.up, UpsamplePassIndex);

                        lastRT = mip.up;
                    }
                }

                using (new ProfilingScope(cmd, new ProfilingSampler("Composite")))
                {
                    // Final composition
                    // lastRT should be pyramid[1].up
                    cmd.SetPostProcessInputTexture(lastRT);
                    material.DrawFullScreen(cmd, lastRT, destination, CompositePassIndex);
                }
            }
        }

        public override void Cleanup()
        {
            if (_pyramids != null)
            {
                foreach (var pyramid in _pyramids.Values)
                {
                    pyramid?.Release();
                }
            }
        }

        #region Image pyramid class used in Streak effect

        public sealed class StreakPyramid
        {
            public const int MaxMipLevel = 16;

            private int _baseWidth;
            private int _baseHeight;
            private int _maxSize;
            private int _iterations;
            public int MipCount;

            private readonly (RTHandle down, RTHandle up)[] _mips = new (RTHandle, RTHandle) [MaxMipLevel];

            public (RTHandle down, RTHandle up) this[int index] => _mips[index];

            public StreakPyramid(CameraData cameraData) { Allocate(cameraData); }

            // unused
            /*
            private void SetMipCount()
            {
                _maxSize    = Mathf.Max(_baseWidth, _baseHeight >> 1);
                _iterations = Mathf.FloorToInt(Mathf.Log(_maxSize, 2f) - 1);
                MipCount    = Mathf.Clamp(_iterations, 1, MaxMipLevel);
            }
            */

            private Vector2Int GetSize(CameraData cameraData)
            {
                return new Vector2Int
                (
                    Mathf.FloorToInt(cameraData.camera.scaledPixelWidth),
                    Mathf.FloorToInt(cameraData.camera.scaledPixelHeight)
                );
            }

            public bool CheckSize(CameraData cameraData)
            {
                return _baseWidth == Mathf.FloorToInt(cameraData.camera.scaledPixelWidth) &&
                       _baseHeight == Mathf.FloorToInt(cameraData.camera.scaledPixelHeight);
            }

            public void Reallocate(CameraData cameraData)
            {
                Release();
                Allocate(cameraData);
            }

            public void Release()
            {
                foreach (var mip in _mips)
                {
                    mip.down?.Release();
                    mip.up?.Release();
                }
            }

            void Allocate(CameraData cameraData)
            {
                var screenSize = GetSize(cameraData);
                _baseWidth  = screenSize.x;
                _baseHeight = screenSize.y;

                // SetMipCount();

                var descriptor = cameraData.cameraTargetDescriptor;

                int width = _baseWidth;
                int height = _baseHeight >> 1;

                descriptor.width          = width;
                descriptor.height         = height;
                descriptor.graphicsFormat = RTFormat;

                _mips[0] = (down: RTHandles.Alloc(width, height, colorFormat: RTFormat, name: "_StreakMipDown0"), up: null);

                for (var i = 1; i < MaxMipLevel; i++)
                {
                    width = Mathf.Max(1, width >> 1);

                    _mips[i] = width < 4
                        ? (down: null, up: null)
                        : (down: RTHandles.Alloc(width, height, colorFormat: RTFormat, name: "_StreakMipDown" + i),
                           up: RTHandles.Alloc(width, height, colorFormat: RTFormat, name: "_StreakMipUp" + i));
                }
            }
        }

        #endregion
    }
}