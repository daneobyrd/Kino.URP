using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Kino.PostProcessing.CustomPostProcessUtils;

namespace Kino.PostProcessing
{
    public class PostProcessRenderPass : ScriptableRenderPass
    {
        private readonly string _displayName;

        // private MaterialLibrary m_Materials;
        private KinoPostProcessData m_Data;
        private RenderTargetIdentifier m_Source      { get; set; }
        private RenderTargetHandle     m_Destination { get; set; }
        private RenderTargetHandle m_Depth;

        private RenderTextureDescriptor m_Descriptor;
        
        // Effects Settings
        private readonly List<Type> _volumeTypeList;
        private readonly List<PostProcessVolumeComponent> _activeVolumeList;

        #region Post Settings

        // Blit to screen or color front buffer at the end
        private bool m_ResolveToScreen = false;

        // Renderer is using swap buffer system
        private bool m_UseSwapBuffer;

        #endregion

        private Material m_BlitMaterial;

        private bool DestinationIsCameraTarget() { return m_Destination == RenderTargetHandle.CameraTarget; }

        public PostProcessRenderPass(InjectionPoint injectionPoint, KinoPostProcessData data, PostProcessOrderConfig config, Material blitMaterial)
        {
            _displayName    = $"Custom {injectionPoint} Pass";
            renderPassEvent = (RenderPassEvent) injectionPoint;
            m_Data          = data;
            // m_Materials       = new MaterialLibrary(data);
            m_BlitMaterial    = blitMaterial;
            _activeVolumeList = new List<PostProcessVolumeComponent>();
            _volumeTypeList   = new List<Type>();

            GetVolumeTypeList(injectionPoint, config);
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            m_Descriptor                 = baseDescriptor;
            m_Descriptor.msaaSamples     = 1;
            m_Descriptor.depthBufferBits = (int) DepthBits.None;
            // m_Descriptor.sRGB            = true;
            // m_Descriptor.useMipMap        = false;
            // m_Descriptor.autoGenerateMips = false;
            m_Destination   = RenderTargetHandle.CameraTarget;
            m_UseSwapBuffer = true;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            // m_Source = new RenderTargetIdentifier(ShaderConstants.MainTex);
            // cmd.GetTemporaryRT(ShaderConstants.MainTex, descriptor, FilterMode.Point);

            /* Comment from FullScreenRenderPass (URP 13) */
            // For some reason BlitCameraTexture(cmd, dest, dest) scenario (as with before transparents effects) blitter fails to correctly blit the data
            // Sometimes it copies only one effect out of two, sometimes second, sometimes data is invalid (as if sampling failed?).
            // Adding a temp RT in between solves this issue.

            var isBeforeTransparents = renderPassEvent == RenderPassEvent.BeforeRenderingTransparents;
            var source = isBeforeTransparents ? cameraData.renderer.GetCameraColorBackBuffer(cmd) : cameraData.renderer.cameraColorTarget;

            // var isAfterPostProcess = renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing;
            // m_Source = isAfterPostProcess ? new RenderTargetIdentifier("_AfterPostProcessColor") : source;
            m_Source = source;
            
            // Blitter.BlitCameraTexture(cmd, (RTHandle)source, m_Source);
            
            // If destination is camera target, there is no reason to request a temp
            if (DestinationIsCameraTarget())
                return;

            // If RenderTargetHandle already has a valid internal render target identifier, we shouldn't request a temp
            if (m_Destination.HasInternalRenderTargetId())
                return;

            cmd.GetTemporaryRT(m_Destination.id, descriptor, FilterMode.Point);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            #region Null checks

            // if (m_Materials == null)
            // {
            //     Debug.LogError("Custom Post Processing Materials instance is null");
            //     return;
            // }

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

            #endregion

            // Regular render path (not on-tile) - we do everything in a single command buffer as it
            // makes it easier to manage temporary targets' lifetime
            var cmd = CommandBufferPool.Get();
            cmd.name = _displayName;

            // if (m_UseSwapBuffer)
            {
                RenderWithSwapBuffer(cmd, ref renderingData);
            }
            // else
            {
                // Render(cmd, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            if (!DestinationIsCameraTarget())
            {
                cmd.ReleaseTemporaryRT(m_Destination.id);
                m_Destination = RenderTargetHandle.CameraTarget;
            }

            // m_Materials.Cleanup();
        }

        #region Local Functions

        private void GetVolumeTypeList(InjectionPoint injectionPoint, PostProcessOrderConfig config)
        {
            // Collect all custom postprocess volume belong this InjectionPoint
            var allVolumeTypes = CoreUtils.GetAllTypesDerivedFrom<PostProcessVolumeComponent>().ToList();
            foreach (var volumeName in config.GetVolumeList(injectionPoint))
            {
                var volumeType = allVolumeTypes.ToList().Find(t => t.ToString() == volumeName);

                // Should be obsolete because we are using CoreUtils.GetAllTypesDerivedFrom<PostProcessVolumeComponent>()
                // Check volume type is valid
                // Assert.IsNotNull(volumeType, $"Can't find Volume : [{volumeName}] , Remove it from config");
                _volumeTypeList.Add(volumeType);
            }
        }

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
                volumeComp.SetupIfNeeded(m_Data);
            }
        }

        private RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            var desc = m_Descriptor;
            desc.useMipMap        = false;
            desc.autoGenerateMips = false;
            desc.depthBufferBits  = depthBufferBits;
            desc.width            = width;
            desc.height           = height;
            desc.graphicsFormat   = format;
            return desc;
        }

        private RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, RenderTextureFormat format, int depthBufferBits = 0)
        {
            var desc = m_Descriptor;
            desc.useMipMap        = false;
            desc.autoGenerateMips = false;
            desc.depthBufferBits  = depthBufferBits;
            desc.width            = width;
            desc.height           = height;
            desc.colorFormat      = format;
            return desc;
        }

        private RenderTextureDescriptor GetCompatibleDescriptor() => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        #endregion

        private void RenderWithSwapBuffer(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // In some cases, accessing values by reference can improve performance by avoiding potentially high-overhead copy operations.
            // For example, the following statements shows how to define a ref local variable for a reference value.
            ref var cameraData = ref renderingData.cameraData;
            ref var renderer = ref cameraData.renderer;

            var pixelRect = cameraData.camera.pixelRect;
            float scale = cameraData.isSceneViewCamera ? 1 : cameraData.renderScale;
            // cameraData.resolveFinalTarget = false;

            int width = (int) (pixelRect.width * scale);
            int height = (int) (pixelRect.height * scale);

            m_Descriptor = GetCompatibleDescriptor(width, height, RenderTextureFormat.DefaultHDR);

            #region SwapBuffer

            // Don't use these directly unless you have a good reason to, use GetSource() and GetDestination() instead
            bool tempTargetUsed = false;
            bool tempTarget2Used = false;

            RenderTargetIdentifier source;
            RenderTargetIdentifier destination;

            if (m_UseSwapBuffer)
            {
                source      = renderer.cameraColorTarget; // presumed back buffer
                destination = renderer.GetCameraColorFrontBuffer(cmd);
            }
            else
            {
                source      = m_Source;
                destination = -1;
            }

            RenderTargetIdentifier GetSource() => source;

            RenderTargetIdentifier GetDestination()
            {
                if (m_UseSwapBuffer)
                    return destination;

                if (destination == -1) // destination is cameraTarget
                {
                    cmd.GetTemporaryRT(ShaderConstants.TempRT1, width, height, 0, FilterMode.Bilinear);
                    destination    = new RenderTargetIdentifier(ShaderConstants.TempRT1);
                    tempTargetUsed = true;
                }
                else if (destination == m_Source && m_Descriptor.msaaSamples > 1)
                {
                    // Avoid using m_Source.id as new destination, it may come with a depth buffer that we don't want, may have MSAA that we don't want etc
                    cmd.GetTemporaryRT(ShaderConstants.TempRT2, width, height, 0, FilterMode.Bilinear);
                    destination     = new RenderTargetIdentifier(ShaderConstants.TempRT2);
                    tempTarget2Used = true;
                }

                return destination;
            }

            bool isFinalVolume = false;

            void Swap(ref ScriptableRenderer r)
            {
                if (m_UseSwapBuffer)
                {
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

            // Setup projection matrix for cmd.DrawMesh()
            cmd.SetGlobalMatrix(ShaderConstants._FullscreenProjMat, GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));

            #region Custom post-processing stack

            // Loop through custom effect stack
            for (var i = 0; i < _activeVolumeList.Count; i++)
            {
                var currentEffect = _activeVolumeList[i];
                isFinalVolume = i == _activeVolumeList.Count - 1;

                using (new ProfilingScope(cmd, new ProfilingSampler(currentEffect.displayName)))
                {
                    if (currentEffect is Streak streak)
                    {
                        streak.SetCameraData(cameraData);
                    }

                    // destination is front buffer or tempRT
                    cmd.SetRenderTarget(GetDestination());

                    currentEffect.Render(cmd, GetSource(), GetDestination());

                    Swap(ref renderer);
                }
            }

            #endregion

            // if (source == renderer.GetCameraColorFrontBuffer(cmd))
            // {
            //     source = renderer.cameraColorTarget;
            // }

            // Done with effects for this pass, blit it
            cmd.SetGlobalTexture(ShaderConstants.SourceTexture, GetSource());

            PostProcessBlit(cmd, BlitDstDiscardContent(cmd, destination), m_BlitMaterial);
            // CoreUtils.SetRenderTarget(cmd, renderer.GetCameraColorBackBuffer(cmd));
            // CoreUtils.DrawFullScreen(cmd, m_BlitMaterial);
            
            /*
            RenderBufferLoadAction colorLoadAction = RenderBufferLoadAction.DontCare;
            if (DestinationIsCameraTarget() && !cameraData.isDefaultViewport)
            {
                colorLoadAction = RenderBufferLoadAction.Load;
            }

            RenderTargetIdentifier targetDestination = m_UseSwapBuffer ? destination : m_Destination.id;

            // Note: We rendering to "camera target" we need to get the cameraData.targetTexture as this will get the targetTexture of the camera stack.
            // Overlay cameras need to output to the target described in the base camera while doing camera stack.
            RenderTargetIdentifier cameraTargetID = (BuiltinRenderTextureType.CameraTarget);

            RenderTargetIdentifier cameraTarget =
                cameraData.targetTexture != null
                    ? new RenderTargetIdentifier(cameraData.targetTexture) // if not null, use RenderTexture assigned in inspector
                    : cameraTargetID;                                      // else use CameraTarget

            // With camera stacking we not always resolve post to final screen as we might run post-processing in the middle of the stack.
            if (m_UseSwapBuffer)
            {
                cameraTarget = targetDestination;
            }
            else
            {
                cameraTarget = (DestinationIsCameraTarget()) ? cameraTarget : m_Destination.Identifier();
                // m_ResolveToScreen = cameraData.resolveFinalTarget || m_Destination.Identifier() == cameraTargetId;
            }

            if (cameraTarget == renderer.GetCameraColorBackBuffer(cmd))
            {
                cameraTarget = cameraTargetID;
            }

            // Based on end of UberPost in PostProcessPass
            cmd.SetRenderTarget(cameraTarget, colorLoadAction, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            ConfigureTarget(cameraTarget);
            cameraData.renderer.ConfigureCameraTarget(cameraTarget, cameraTarget);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            if ((DestinationIsCameraTarget() && !m_UseSwapBuffer) || (m_ResolveToScreen && m_UseSwapBuffer))
                cmd.SetViewport(cameraData.camera.pixelRect);

            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_BlitMaterial);


            // TODO: Implement swapbuffer in 2DRenderer so we can remove this
            // For now, when render post-processing in the middle of the camera stack (not resolving to screen)
            // we do an extra blit to ping pong results back to color texture. In future we should allow a Swap of the current active color texture
            // in the pipeline to avoid this extra blit.
            if (!m_UseSwapBuffer && !m_ResolveToScreen)
            {
                cmd.SetGlobalTexture(ShaderConstants.SourceTexture, cameraTarget);
                cmd.SetRenderTarget(m_Source, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_BlitMaterial);
            }

            cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
            ConfigureTarget(cameraTarget);
            // cameraData.renderer.ConfigureCameraTarget(cameraTarget, cameraTarget);

            if (m_UseSwapBuffer && !m_ResolveToScreen)
            {
                // Swap(ref renderer);
                renderer.SwapColorBuffer(cmd);
            }*/

            if (tempTargetUsed)
            {
                cmd.ReleaseTemporaryRT(ShaderConstants.TempRT1);
            }

            if (tempTarget2Used)
            {
                cmd.ReleaseTemporaryRT(ShaderConstants.TempRT2);
            }
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_volumeTypeList.Count == 0)
                return;

            if (renderingData.cameraData.postProcessEnabled == false)
                return;

            bool isSceneViewCamera = renderingData.cameraData.isSceneViewCamera;
            GetActivePPVolumes(isSceneViewCamera);

            if (_activeVolumeList.Count <= 0)
                return;

            var cameraData = renderingData.cameraData;
            var pixelRect = cameraData.camera.pixelRect;
            float scale = cameraData.isSceneViewCamera ? 1 : cameraData.renderScale;
            int width = (int) (pixelRect.width * scale);
            int height = (int) (pixelRect.height * scale);
            cmd.GetTemporaryRT(ShaderConstants.TempRT1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            cmd.GetTemporaryRT(ShaderConstants.TempRT2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            var target = ShaderConstants.TempRT1;
            var source = ShaderConstants.TempRT2;

            for (int i = 0; i < _activeVolumeList.Count; i++)
            {
                var volumeComp = _activeVolumeList[i];
                if (volumeComp is Streak streak)
                {
                    streak.SetCameraData(cameraData);
                }

                if (i == 0)
                {
                    cmd.Blit(BuiltinRenderTextureType.CurrentActive, source);
                }
                else
                {
                    CoreUtils.Swap(ref target, ref source);
                }

                RenderTargetIdentifier renderTarget;
                bool isFinalVolume = i == _activeVolumeList.Count - 1;
                if (isFinalVolume)
                {
                    bool renderToDefaultColorTexture =
                        renderPassEvent == RenderPassEvent.BeforeRenderingPostProcessing
                        || renderPassEvent == RenderPassEvent.BeforeRenderingTransparents;

                    if (renderToDefaultColorTexture)
                    {
                        ref ScriptableRenderer renderer = ref cameraData.renderer;
                        renderTarget = renderer.cameraColorTarget;
                    }
                    else
                    {
                        renderTarget = BuiltinRenderTextureType.CameraTarget;
                    }
                }
                else
                {
                    renderTarget = target;
                }

                cmd.SetRenderTarget(renderTarget);

                cmd.BeginSample(volumeComp.displayName);
                volumeComp.Render(cmd, source, renderTarget);
                cmd.EndSample(volumeComp.displayName);
            }

            cmd.ReleaseTemporaryRT(source);
            cmd.ReleaseTemporaryRT(target);
        }

        #region Internal utilities

        private static class ShaderConstants
        {
            internal static readonly int MainTex = Shader.PropertyToID("_MainTex");
            internal static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");
            private const string TempRT1Name = "tempRT_1";
            private const string TempRT2Name = "tempRT_2";

            public static readonly int TempRT1 = Shader.PropertyToID(TempRT1Name);
            public static readonly int TempRT2 = Shader.PropertyToID(TempRT2Name);
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");

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

            // Retrieve the material for a given PostProcessVolumeComponent
            public Material GetMaterialForComponent(Type componentType)
            {
                if (materialMap.TryGetValue(componentType, out Material material))
                {
                    return material;
                }

                Debug.LogError($"Could not find material for component of type {componentType.Name}");
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