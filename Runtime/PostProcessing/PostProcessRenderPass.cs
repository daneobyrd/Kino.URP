namespace Kino.PostProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.Experimental.Rendering;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using static KinoCore;

    public class PostProcessRenderPass : ScriptableRenderPass
    {
        private readonly string _displayName;
        private MaterialLibrary m_Materials;
        private KinoPostProcessData m_Data;

        private RenderTextureDescriptor m_Descriptor;

        private readonly int copiedColor = Shader.PropertyToID("_FullscreenColorCopy");
        private RenderTargetIdentifier m_CopiedColor;

        private RenderTargetIdentifier m_Source;
        private RenderTargetHandle m_Destination;
        private RenderTargetHandle m_Depth;

        private const string TempRT1Name = "tempRT_1";
        private const string TempRT2Name = "tempRT_2";
        private readonly int _tempRT1 = Shader.PropertyToID(TempRT1Name);
        private readonly int _tempRT2 = Shader.PropertyToID(TempRT2Name);

        // Effects Settings
        private readonly List<Type> _volumeTypeList;
        private readonly List<PostProcessVolumeComponent> _activeVolumeList;

        private readonly GraphicsFormat m_DefaultHDRFormat;

        // Pass Variable Settings

        // True when this is the very last pass in the pipeline
        private bool m_IsFinalPass;

        // Some Android devices do not support sRGB backbuffer
        // We need to do the conversion manually on those
        private bool m_EnableSRGBConversionIfNeeded;

        // Use Fast conversions between SRGB and Linear
        private bool m_UseFastSRGBLinearConversion;

        // Blit to screen or color front buffer at the end
        private bool m_ResolveToScreen;

        // Renderer is using swap buffer system
        private bool m_UseSwapBuffer;

        private Material m_BlitMaterial;


        public PostProcessRenderPass(InjectionPoint injectionPoint, KinoPostProcessData data, PostProcessOrderConfig config, Material blitMaterial)
        {
            _displayName      = $"CustomPostProcessPass {injectionPoint}";
            renderPassEvent   = (RenderPassEvent) injectionPoint;
            m_Data            = data;
            m_Materials       = new MaterialLibrary(data);
            m_BlitMaterial    = blitMaterial;
            _activeVolumeList = new List<PostProcessVolumeComponent>();
            _volumeTypeList   = new List<Type>();

            var universalRenderPipelineAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            SetDefaultHDRFormat(out m_DefaultHDRFormat);

            // Collect all custom postprocess volume belong this InjectionPoint
            var allVolumeTypes = CoreUtils.GetAllTypesDerivedFrom<PostProcessVolumeComponent>().ToList();
            foreach (var volumeName in config.GetVolumeList(injectionPoint))
            {
                var volumeType = allVolumeTypes.ToList().Find(t => t.ToString() == volumeName);

                // Check volume type is valid
                Assert.IsNotNull(volumeType, $"Can't find Volume : [{volumeName}] , Remove it from config");
                _volumeTypeList.Add(volumeType);
            }
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, bool enableSRGBConversion)
        {
            RenderTargetHandle depth = new(BuiltinRenderTextureType.Depth);
            Setup(baseDescriptor, depth, enableSRGBConversion);
        }

        public void Setup(in RenderTextureDescriptor baseDescriptor, in RenderTargetHandle depth, bool enableSRGBConversion)
        {
            m_Descriptor                   = baseDescriptor;
            m_Descriptor.useMipMap         = false;
            m_Descriptor.autoGenerateMips  = false;
            m_Depth                        = depth;
            m_EnableSRGBConversionIfNeeded = enableSRGBConversion;
            m_Destination                  = RenderTargetHandle.CameraTarget;
            m_UseSwapBuffer                = true;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            PrepCopiedColor(ref cmd, ref renderingData);

            // If destination is camera target, there is no reason to request a temp
            if (m_Destination == RenderTargetHandle.CameraTarget)
                return;

            // If RenderTargetHandle already has a valid internal render target identifier, we shouldn't request a temp
            if (m_Destination.HasInternalRenderTargetId())
                return;

            var desc = GetCompatibleDescriptor();
            desc.depthBufferBits = 0;
            cmd.GetTemporaryRT(m_Destination.id, desc, FilterMode.Point);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_UseFastSRGBLinearConversion = renderingData.postProcessingData.useFastSRGBLinearConversion;

            if (m_Materials == null)
            {
                Debug.LogError("Custom Post Processing Materials instance is null");
                return;
            }
            if (m_Data == null)
            {
                Debug.LogError("Post Processing Data is null. Go to Create/Rendering/CustomPostProcessing/Kino Post Processing Data");
                return;
            }

            if (_volumeTypeList.Count == 0)
                return;
            if (renderingData.cameraData.postProcessEnabled == false)
                return;

            GetActivePPVolumes(renderingData.cameraData.isSceneViewCamera);

            if (_activeVolumeList.Count <= 0)
                return;

            // Regular render path (not on-tile) - we do everything in a single command buffer as it
            // makes it easier to manage temporary targets' lifetime
            var cmd = CommandBufferPool.Get();
            cmd.name = _displayName;

            Render(cmd, ref renderingData);

            context.ExecuteCommandBuffer(cmd);
            // cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { cmd.ReleaseTemporaryRT(copiedColor); }

        #region Local Functions

        private RenderTargetIdentifier CurrentColorTarget(ref CommandBuffer cmd, ref RenderingData renderingData)
        {
            /* From FullScreenRenderPass (URP 13) */
            //
            // For some reason BlitCameraTexture(cmd, dest, dest) scenario (as with before transparents effects) blitter fails to correctly blit the data
            // Sometimes it copies only one effect out of two, sometimes second, sometimes data is invalid (as if sampling failed?).
            // Adding a temp RT in between solves this issue.

            // I've followed this as a precaution.
            // While PostProcessRenderPass does not use the SRP Blitter API,
            // it's possible that before transparents effects may fail in the way described above.

            ref var cameraData = ref renderingData.cameraData;
            var isBeforeTransparents = renderPassEvent == RenderPassEvent.BeforeRenderingTransparents;
            return isBeforeTransparents ? cameraData.renderer.GetCameraColorBackBuffer(cmd) : cameraData.renderer.cameraColorTarget;
        }

        private void PrepCopiedColor(ref CommandBuffer cmd, ref RenderingData renderingData)
        {
            var colorCopyDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            colorCopyDescriptor.depthBufferBits = (int) DepthBits.None;

            m_CopiedColor = new RenderTargetIdentifier(copiedColor);
            cmd.GetTemporaryRT(copiedColor, colorCopyDescriptor);
        }

        private static void SetDefaultHDRFormat(out GraphicsFormat defaultHDRFormat)
        {
            // Texture format pre-lookup
            if (SystemInfo.IsFormatSupported
                    (GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render) && ((UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline).supportsHDR)
            {
                defaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            }
            else
            {
                defaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
            }
        }

        private RenderTextureDescriptor GetCompatibleDescriptor() => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        private RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            var desc = m_Descriptor;
            desc.depthBufferBits = depthBufferBits;
            desc.msaaSamples     = 1;
            desc.bindMS          = true;
            desc.width           = width;
            desc.height          = height;
            desc.graphicsFormat  = format;
            return desc;
        }

        private bool RequireSRGBConversionBlitToBackBuffer(CameraData cameraData) { return cameraData.requireSrgbConversion() && m_EnableSRGBConversionIfNeeded; }

        private void GetActivePPVolumes(bool isSceneViewCamera)
        {
            _activeVolumeList.Clear();

            foreach (var item in _volumeTypeList)
            {
                var volumeComp = VolumeManager.instance.stack.GetComponent(item) as PostProcessVolumeComponent;

                if (volumeComp == null ||
                    volumeComp.IsActive() == false ||
                    isSceneViewCamera && volumeComp.visibleInSceneView == false)
                {
                    continue;
                }

                _activeVolumeList.Add(volumeComp);
                volumeComp.SetupIfNeed(m_Materials.GetMaterialForComponent(volumeComp));
            }
        }

        #endregion

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // In some cases, accessing values by reference can improve performance by avoiding potentially high-overhead copy operations.
            // For example, the following statements shows how to define a ref local variable for a reference value.
            ref CameraData cameraData = ref renderingData.cameraData;
            ref ScriptableRenderer renderer = ref cameraData.renderer;

            var pixelRect = cameraData.camera.pixelRect;
            float scale = cameraData.isSceneViewCamera ? 1 : cameraData.renderScale;

            int width = (int) (pixelRect.width * scale);
            int height = (int) (pixelRect.height * scale);

            m_Descriptor = GetCompatibleDescriptor(width, height, m_DefaultHDRFormat);

            var amountOfPassesRemaining = _activeVolumeList.Count;

            #region SwapBuffer

            // TODO: Determine if built-in pp will perform anti-aliasing after this pass
            // Should be false to ensure no MSAA occurs during SwapBuffer before final pass
            if (m_UseSwapBuffer && amountOfPassesRemaining > 0) { renderer.EnableSwapBufferMSAA(false); }

            // Don't use these directly unless you have a good reason to, use GetSource() and GetDestination() instead
            bool tempTargetUsed = false;
            bool tempTarget2Used = false;

            RenderTargetIdentifier source = m_UseSwapBuffer ? CurrentColorTarget(ref cmd, ref renderingData) : renderer.GetCameraColorBackBuffer(cmd);
            RenderTargetIdentifier destination = m_UseSwapBuffer ? renderer.GetCameraColorBackBuffer(cmd) : -1;

            RenderTargetIdentifier GetSource() => source;

            RenderTargetIdentifier GetDestination()
            {
                if (m_UseSwapBuffer)
                    return destination;
                else
                {
                    if (destination == -1)
                    {
                        cmd.GetTemporaryRT(_tempRT1, m_Descriptor, FilterMode.Bilinear);
                        destination    = _tempRT2;
                        tempTargetUsed = true;
                    }
                    else if (destination == m_CopiedColor && m_Descriptor.msaaSamples > 1)
                    {
                        // Avoid using m_Source.id as new destination, it may come with a depth buffer that we don't want, may have MSAA that we don't want etc
                        cmd.GetTemporaryRT(_tempRT2, m_Descriptor, FilterMode.Bilinear);
                        destination     = _tempRT2;
                        tempTarget2Used = true;
                    }

                    return destination;
                }
            }

            void Swap(ref ScriptableRenderer r)
            {
                // using for(i) loop instead of decrementing amountOfPassesRemaining at the start of each swap
                // --amountOfPassesRemaining;

                if (m_UseSwapBuffer)
                {
                    // TODO: Determine if built-in pp will perform anti-aliasing after this pass
                    // we want the last blit to be to MSAA
                    // if (amountOfPassesRemaining == 0 && !m_HasFinalPass)
                    // r.EnableSwapBufferMSAA(true);

                    r.SwapColorBuffer(cmd);
                    source      = r.cameraColorTarget;
                    destination = r.GetCameraColorFrontBuffer(cmd);
                }
                else
                {
                    CoreUtils.Swap(ref source, ref destination);
                }
            }

            #endregion

            if (RequireSRGBConversionBlitToBackBuffer(cameraData))
            {
                m_BlitMaterial.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
            }

            if (m_UseFastSRGBLinearConversion)
            {
                const string useFastSrgbLinearConversion = "_USE_FAST_SRGB_LINEAR_CONVERSION";
                m_BlitMaterial.EnableKeyword(useFastSrgbLinearConversion);
            }

            #region Custom post-processing stack

            for (var i = 0; i < _activeVolumeList.Count; i++)
            {
                var volumeComp = _activeVolumeList[i];

                if (i == 0)
                {
                    // copy Current Active
                    cmd.Blit(BuiltinRenderTextureType.CurrentActive, m_CopiedColor);
                }
                else
                {
                    Swap(ref renderer);
                }

                RenderTargetIdentifier renderTarget;

                var lastIndex = _activeVolumeList.Count - 1;
                bool isFinalVolume = (i == lastIndex);

                if (isFinalVolume)
                {
                    bool renderToDefaultColorTexture = renderPassEvent is RenderPassEvent.BeforeRenderingPostProcessing or RenderPassEvent.BeforeRenderingTransparents;

                    if (renderToDefaultColorTexture)
                    {
                        renderTarget = renderer.cameraColorTarget;
                    }
                    else
                    {
                        renderTarget = BuiltinRenderTextureType.CameraTarget;
                    }
                }
                else
                {
                    // Initialize with target? Only overwritten when isFinalVolume is true.
                    renderTarget = GetDestination();
                }

                cmd.SetRenderTarget(renderTarget);

                using (new ProfilingScope(cmd, new ProfilingSampler(volumeComp.displayName)))
                {
                    if (volumeComp is Streak)
                    {
                        volumeComp.Render(ref cmd, ref renderingData.cameraData, m_CopiedColor, renderTarget);
                    }
                    else
                    {
                        volumeComp.Render(ref cmd, ref renderingData.cameraData, GetSource(), renderTarget);
                    }
                }
            }

            // Done with effects for this pass, blit it
            cmd.SetGlobalTexture(ShaderIDs.InputTexture, GetSource());

            #endregion

            #region Setup Blit Targets

            var colorLoadAction = RenderBufferLoadAction.DontCare;
            if (m_Destination == RenderTargetHandle.CameraTarget && !cameraData.isDefaultViewport)
            {
                colorLoadAction = RenderBufferLoadAction.Load;
            }

            RenderTargetIdentifier targetDestination = m_UseSwapBuffer ? destination : m_Destination.id;

            // Note: We rendering to "camera target" we need to get the cameraData.targetTexture as this will get the targetTexture of the camera stack.
            // Overlay cameras need to output to the target described in the base camera while doing camera stack.
            RenderTargetHandle cameraTargetHandle = RenderTargetHandle.CameraTarget;
            // Not using original 2021 method because CameraData.xr is internal and I don't care about XR.
            /* 2021: */ // = GetCameraTarget(cameraData.xr);
            /* 2022: */ // = (RTHandle) k_CameraTarget

            var cameraTarget = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : cameraTargetHandle.Identifier();

            // With camera stacking we not always resolve post to final screen as we might run post-processing in the middle of the stack.
            if (m_UseSwapBuffer)
            {
                cameraTarget = /*(m_ResolveToScreen) ? cameraTarget :*/ targetDestination;
            }
            else
            {
                cameraTarget = (m_Destination == RenderTargetHandle.CameraTarget) ? cameraTarget : m_Destination.Identifier();
                m_ResolveToScreen = cameraData.resolveFinalTarget || (m_Destination == cameraTargetHandle /*|| m_HasFinalPass == true*/);
            }

            #endregion

            // #if VR || XR
            //
            // #else
            {
                cmd.SetRenderTarget(cameraTarget, colorLoadAction, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                cameraData.renderer.ConfigureCameraTarget(cameraTarget, cameraTarget);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

                if ((m_Destination == RenderTargetHandle.CameraTarget && !m_UseSwapBuffer) || (m_ResolveToScreen && m_UseSwapBuffer))
                {
                    cmd.SetViewport(pixelRect);
                }

                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_BlitMaterial);

                // TODO: Implement swapbuffer in 2DRenderer so we can remove this
                // For now, when render post-processing in the middle of the camera stack (not resolving to screen)
                // we do an extra blit to ping pong results back to color texture. In future we should allow a Swap of the current active color texture
                // in the pipeline to avoid this extra blit.
                if (!m_ResolveToScreen && !m_UseSwapBuffer)
                {
                    cmd.SetGlobalTexture(ShaderIDs.InputTexture, cameraTarget);
                    cmd.SetRenderTarget(m_CopiedColor, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_BlitMaterial);
                }

                cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
            }

            if (m_UseSwapBuffer && !m_ResolveToScreen)
            {
                renderer.SwapColorBuffer(cmd);
            }

            cmd.ReleaseTemporaryRT(copiedColor);
            
            if (tempTargetUsed)
            {
                cmd.ReleaseTemporaryRT(_tempRT1);
            }

            if (tempTarget2Used)
            {
                cmd.ReleaseTemporaryRT(_tempRT2);
            }
        }

        #region Internal utilities

        public static class ShaderConstants
        {
            public static readonly int _TempTarget = Shader.PropertyToID("_TempTarget");
            public static readonly int _TempTarget2 = Shader.PropertyToID("_TempTarget2");

            public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");
        }

        public class MaterialLibrary
        {
            // Associate all of your custom effects with Materials
            private readonly Dictionary<Type, Material> materialMap;

            public readonly Material streak;
            public readonly Material overlay;
            public readonly Material recolor;
            public readonly Material glitch;
            public readonly Material sharpen;
            public readonly Material utility;
            public readonly Material slice;

            public readonly Material testCard;
            // public readonly Material finalPass;

            public MaterialLibrary(KinoPostProcessData data)
            {
                streak   = Load(data.shaders.StreakPS);
                overlay  = Load(data.shaders.OverlayPS);
                recolor  = Load(data.shaders.RecolorPS);
                glitch   = Load(data.shaders.GlitchPS);
                sharpen  = Load(data.shaders.SharpenPS);
                utility  = Load(data.shaders.UtilityPS);
                slice    = Load(data.shaders.SlicePS);
                testCard = Load(data.shaders.TestCardPS);

                // Initialize the material map
                materialMap = new Dictionary<Type, Material>
                {
                    {typeof(Streak), streak},
                    {typeof(Overlay), overlay},
                    {typeof(Recolor), recolor},
                    {typeof(Glitch), glitch},
                    {typeof(Sharpen), sharpen},
                    {typeof(Utility), utility},
                    {typeof(Slice), slice},
                    {typeof(TestCard), testCard}
                };
            }

            // Retrieve the material for a given CustomVolumeComponent
            public Material GetMaterialForComponent(PostProcessVolumeComponent component)
            {
                if (materialMap.TryGetValue(component.GetType(), out Material material))
                {
                    return material;
                }

                Debug.LogError($"Could not find material for component of type {component.GetType().Name}");
                return null;
            }

            private Material Load(Shader shader)
            {
                if (shader is null)
                {
                    Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType?.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }
                else if (!shader.isSupported)
                {
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }

            internal void Cleanup()
            {
                CoreUtils.Destroy(streak);
                CoreUtils.Destroy(overlay);
                CoreUtils.Destroy(recolor);
                CoreUtils.Destroy(glitch);
                CoreUtils.Destroy(sharpen);
                CoreUtils.Destroy(utility);
                CoreUtils.Destroy(slice);
                CoreUtils.Destroy(testCard);
            }
        }

        #endregion
    }
}