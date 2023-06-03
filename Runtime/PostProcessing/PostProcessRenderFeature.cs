using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Kino.PostProcessing
{
    /// <summary>
    /// CustomPassRendererFeature is a renderer feature used to change screen appearance such as post processing effect.
    /// This implementation lets it's user create an effect with minimal code involvement.
    /// </summary>
    [Serializable, DisallowMultipleRendererFeature(nameof(PostProcessRenderFeature))]
    public class PostProcessRenderFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// Material the Renderer Feature uses to render the effect.
        /// </summary>
        private static Material m_BlitMaterial => CoreUtils.CreateEngineMaterial(Shader.Find("ColorBlit"));

        /// <summary>
        /// An index that tells renderer feature which pass to use if passMaterial contains more than one. Default is 0.
        /// We draw custom pass index entry with the custom dropdown inside FullScreenPassRendererFeatureEditor that sets this value.
        /// Setting it directly will be overridden by the editor class.
        /// </summary>
        [HideInInspector] public int passIndex = 0;

        private KinoPostProcessData postProcessData;
        public PostProcessOrderConfig config;

        public PostProcessRenderPass customPass_BeforeTransparents;
        public PostProcessRenderPass customPass_BeforePostProcess;
        public PostProcessRenderPass customPass_AfterPostProcess;

        /// <inheritdoc/>
        public override void Create()
        {
#if UNITY_EDITOR
            if (config is null)
                return;
            config.OnDataChange = Create;
#endif
            postProcessData ??= KinoPostProcessData.GetDefaultUserPostProcessData();

            if (config.beforeTransparents.Count > 0)
            {
                customPass_BeforeTransparents = new PostProcessRenderPass(InjectionPoint.BeforeTransparents, postProcessData, config, m_BlitMaterial);
            }

            if (config.beforePostProcess.Count > 0)
            {
                customPass_BeforePostProcess = new PostProcessRenderPass(InjectionPoint.BeforePostProcess, postProcessData, config, m_BlitMaterial);
            }

            if (config.afterPostProcess.Count > 0)
            {
                customPass_AfterPostProcess = new PostProcessRenderPass(InjectionPoint.AfterPostProcess, postProcessData, config, m_BlitMaterial);
            }
        }

#if UNITY_2022_1_OR_NEWER
        private override void SetupRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            RTHandle colorTarget = new RTHandle(renderingData.cameraColorTargetHandle);
            RTHandle depthTarget = new RTHandle(renderingData.cameraDepthTargetHandle);
            
            customPass_BeforePostProcess.Setup(cameraTargetDescriptor, depthTarget, enableSRGBConversion: false);
            customPass_BeforePostProcess.Setup(cameraTargetDescriptor, depthTarget, enableSRGBConversion: false);
            customPass_AfterPostProcess.Setup(cameraTargetDescriptor, depthTarget, enableSRGBConversion: false);
        }
#else
        private void SetupRenderPasses(ScriptableRenderer unused, ref RenderingData renderingData)
        {
            ref var cameraTargetDescriptor = ref renderingData.cameraData.cameraTargetDescriptor;

            customPass_BeforePostProcess.Setup(cameraTargetDescriptor, false);
            customPass_BeforePostProcess.Setup(cameraTargetDescriptor, false);
            customPass_AfterPostProcess.Setup(cameraTargetDescriptor, false);
        }
#endif

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!m_BlitMaterial)
            {
                Debug.LogWarningFormat
                (
                    "Missing Post Processing effect Material." +
                    "{0} Fullscreen pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name
                );
                return;
            }

#if !UNITY_2022_1_OR_NEWER // in 2022+ this is an overridden function and does not need to be called here.
            SetupRenderPasses(renderer, ref renderingData);
#endif

            if (customPass_BeforeTransparents != null)
            {
                renderer.EnqueuePass(customPass_BeforeTransparents);
            }

            if (customPass_BeforePostProcess != null)
            {
                renderer.EnqueuePass(customPass_BeforePostProcess);
            }

            if (customPass_AfterPostProcess != null)
            {
                renderer.EnqueuePass(customPass_AfterPostProcess);
            }
        }
    }
}