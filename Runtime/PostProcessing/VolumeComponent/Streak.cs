using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace Kino.PostProcessing
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using static KinoCore;
    using SerializableAttribute = System.SerializableAttribute;

    [Serializable]
    [VolumeComponentMenu("Post-processing/Kino/Streak")]
    public sealed class Streak : PostProcessVolumeComponent
    {
        public ClampedFloatParameter threshold = new(1, 0, 5);
        public ClampedFloatParameter stretch = new(0.75f, 0, 1);
        public ClampedFloatParameter intensity = new(0, 0, 1);
        public ColorParameter tint = new(new Color(0.55f, 0.55f, 1), false, false, true);

        public override InjectionPoint InjectionPoint => InjectionPoint.BeforePostProcess;

        public override bool IsActive() => intensity.value > 0;

        // Image pyramid storage
        // We have to use different pyramids for each camera, so we use a
        // dictionary and camera GUIDs as a key to store each pyramid.
        private Dictionary<int, StreakPyramid> _pyramids;
        const GraphicsFormat RTFormat = GraphicsFormat.R16G16B16A16_SFloat;

        private StreakPyramid GetPyramid(ref CommandBuffer cmd, CameraData cameraData)
        {
            var cameraID = cameraData.camera.GetInstanceID();
            var size = new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.width);
            if (_pyramids.TryGetValue(cameraID, out var candid))
            {
                // Reallocate the RTs when the screen size was changed.
                if (!candid.CheckSize(cameraData.camera))
                    candid.Reallocate(cmd, size);
            }
            else
            {
                // No one found: Allocate a new pyramid.
                _pyramids[cameraID] = candid = new StreakPyramid(size);
            }

            return candid;
        }

        public override void Initialize(Material material)
        {
            base.Initialize(material);
            _pyramids = new Dictionary<int, StreakPyramid>();
        }

        // -------Prefilter--------
        // Source -> Prefilter -> MIP 0.down

        // -------Downsample-------
        // MIP 0.down - MIP 1.down
        // MIP 1.down - MIP 2.down
        // ...
        // MIP 6.down - MIP 7.down

        // -------Upsample---------
        // MIP 7.down - MIP 6.up
        // MIP 6.up   - MIP 5.up
        // ...
        // MIP 2.up   - MIP 1.up

        // -------Composite--------
        // MIP 1.up   - Destination

        private BuiltinRenderTextureType BlitDstDiscardContent(CommandBuffer cmd, RenderTargetIdentifier rt)
        {
            // We set depth to DontCare because rt might be the source of PostProcessing used as a temporary target
            // Source typically comes with a depth buffer and right now we don't have a way to only bind the color attachment of a RenderTargetIdentifier
            cmd.SetRenderTarget
            (
                new RenderTargetIdentifier(rt, 0, CubemapFace.Unknown, -1),
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare
            );
            return BuiltinRenderTextureType.CurrentActive;
        }

        public override void Render(ref CommandBuffer cmd, ref CameraData cameraData, RenderTargetIdentifier source, RenderTargetIdentifier dest)
        {
            var pyramid = GetPyramid(ref cmd, cameraData);

            var desc = cameraData.cameraTargetDescriptor;
            desc.graphicsFormat = RTFormat;

            // Start at half-res
            int tw = pyramid._baseWidth;
            int th = pyramid._baseHeight >> 1;

            float linearThreshold = Mathf.GammaToLinearSpace(threshold.value);

            #region Set Shader Properties

            // Common parameters
            m_Material.SetFloat(ShaderIDs.Threshold, linearThreshold);
            m_Material.SetFloat(ShaderIDs.Stretch, stretch.value);
            m_Material.SetFloat(ShaderIDs.StreakIntensity, intensity.value);
            m_Material.SetColor(ShaderIDs.StreakColor, tint.value);
            cmd.SetGlobalTexture(ShaderIDs.SourceColorTexture, source);

            #endregion

            // ShaderPass Indices
            const int prefilterPass = 0;
            const int downsamplePass = 1;
            const int upsamplePass = 2;
            const int compositePass = 3;

            //
            // Prefilter
            //
            cmd.GetTemporaryRT(pyramid[0].down, desc, FilterMode.Bilinear);
            cmd.SetPostProcessSourceTexture(source);
            // Source -> Prefilter -> MIP 0
            cmd.DrawFullScreenTriangle(m_Material, pyramid[0].down, prefilterPass);

            //
            // Downsample
            //
            var lastDown = pyramid[0].down;
            for (var i = 1; i < pyramid.mipCount; i++)
            {
                tw = Mathf.Max(1, tw >> 1);

                var mipDown = pyramid[i].down;

                desc.width  = tw;
                desc.height = th;

                cmd.GetTemporaryRT(lastDown, desc, FilterMode.Bilinear);
                cmd.GetTemporaryRT(mipDown, desc, FilterMode.Bilinear);
                // cmd.SetPostProcessSourceTexture(pyramid[i - 1].down);
                CustomPostProcessUtils.Blit(cmd, source: mipDown, destination: BlitDstDiscardContent(cmd, pyramid[i - 1].down), m_Material, downsamplePass);

                lastDown = mipDown;
            }

            var lastRT = lastDown;

            //
            // Upsample & combine
            //
            for (var i = pyramid.mipCount - 2; i >= 0; i--)
            {
                var mip = pyramid[i];

                int lowMip = (i == pyramid.mipCount - 2) ? pyramid[i + 1].down : pyramid[i + 1].up;
                int highMip = mip.down;
                int dst = mip.up;

                cmd.SetGlobalTexture(ShaderIDs.SourceTexLowMip, lowMip);
                cmd.SetPostProcessSourceTexture(lastRT);
                cmd.DrawFullScreenTriangle(m_Material, mip.up, upsamplePass);
                lastRT = mip.up;
            }

            // Final composition
            cmd.SetPostProcessSourceTexture(lastRT);
            cmd.DrawFullScreenTriangle(m_Material, dest, compositePass);

            pyramid.Release(cmd);
        }
    }

    #region Image pyramid class used in Streak effect

    public sealed class StreakPyramid
    {
        private const int MaxMipLevel = 16;

        public int _baseWidth;
        public int _baseHeight;
        private int maxSize;
        private int iterations;
        public int mipCount;

        readonly (int down, int up)[] _mips = new (int, int) [MaxMipLevel];

        public (int down, int up) this[int index] => _mips[index];

        public StreakPyramid(Vector2Int size) { Allocate(size); }

        public bool CheckSize(Camera camera)
        {
            return _baseWidth == Mathf.FloorToInt(camera.pixelRect.width) &&
                   _baseHeight == Mathf.FloorToInt(camera.pixelRect.width);
        }

        public void Reallocate(CommandBuffer cmd, Vector2Int size)
        {
            Release(cmd);
            Allocate(size);
        }

        public void Release(CommandBuffer cmd)
        {
            foreach (var (mipDown, mipUp) in _mips)
            {
                cmd.ReleaseTemporaryRT(mipDown);
                cmd.ReleaseTemporaryRT(mipUp);
            }
        }

        void Allocate(Vector2Int size)
        {
            _baseWidth  = Mathf.Max(size.x, 1);
            _baseHeight = Mathf.Max(size.y, 1);

            maxSize = Mathf.Max(_baseWidth, _baseHeight >> 1);

            // Determine the iteration count
            // 1920 = 9, 2560 = 10, 3840 = 10
            iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            mipCount   = Mathf.Clamp(iterations, 1, MaxMipLevel);

            _mips[0] = (Shader.PropertyToID("_StreakMipDown" + "0"), Shader.PropertyToID("_StreakMipUp" + "0"));

            // Will break if (width < 4)
            for (var i = 1; i < mipCount; i++)
            {
                _mips[i] = (Shader.PropertyToID("_StreakMipDown" + i),
                            Shader.PropertyToID("_StreakMipUp" + i));
            }
        }
    }

    #endregion
}