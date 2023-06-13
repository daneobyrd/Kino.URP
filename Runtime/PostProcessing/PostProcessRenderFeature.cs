using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
        private const string UnityBlit = "Hidden/Universal Render Pipeline/Blit";
        private const string UnityBlitCopy = "Hidden/BlitCopy";
        private const string ColorBlit = "ColorBlit";

        /// <summary>
        /// Material the Renderer Feature uses to render the final result.
        /// </summary>
        private static Material m_BlitMaterial => CoreUtils.CreateEngineMaterial(Shader.Find(UnityBlit));

        /// <summary>
        /// An index that tells renderer feature which pass to use if passMaterial contains more than one. Default is 0.
        /// We draw custom pass index entry with the custom dropdown inside FullScreenPassRendererFeatureEditor that sets this value.
        /// Setting it directly will be overridden by the editor class.
        /// </summary>
        [HideInInspector] public int passIndex = 0;

        public KinoPostProcessData postProcessData;
        public PostProcessOrderConfig config;

        public PostProcessRenderPass customPass_BeforeTransparents;
        public PostProcessRenderPass customPass_BeforePostProcess;
        public PostProcessRenderPass customPass_AfterPostProcess;

        private bool hasBeforeTransparents = false;
        private bool hasBeforePostProcess = false;
        private bool hasAfterPostProcess = false;

        private bool m_UseDrawProcedural;

        // Use Fast conversions between SRGB and Linear
        private bool m_UseFastSRGBLinearConversion;

        private bool m_UseRGBM;

        private GraphicsFormat m_DefaultHDRFormat;

        private bool RequireSRGBConversionBlitToBackBuffer(ref CameraData cameraData) { return cameraData.requireSrgbConversion(); }

        public void SetDefaultHDRFormat()
        {
            // Texture format pre-lookup
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            var usage = FormatUsage.Linear | FormatUsage.Render;
            if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32, usage))
            {
                m_DefaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                m_UseRGBM          = false;
            }
            else
            {
                m_DefaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
                m_UseRGBM = true;
            }
        }

        private void SetMaterialKeywords(ref RenderingData renderingData)
        {
            // Reset keywords
            m_BlitMaterial.shaderKeywords = null;

            m_BlitMaterial.SetKeyword(ShaderKeywordStrings.UseDrawProcedural, m_UseDrawProcedural);

            var needsSrgbConversion = RequireSRGBConversionBlitToBackBuffer(ref renderingData.cameraData);
            m_BlitMaterial.SetKeyword(ShaderKeywordStrings.LinearToSRGBConversion, needsSrgbConversion);

            // m_UseFastSRGBLinearConversion = renderingData.postProcessingData.useFastSRGBLinearConversion;
            // const string useFastSrgbLinearConversion = "_USE_FAST_SRGB_LINEAR_CONVERSION";
            // m_BlitMaterial.SetKeyword(useFastSrgbLinearConversion, m_UseFastSRGBLinearConversion);
        }

        private void OnEnable()
        {
            m_UseDrawProcedural = SystemInfo.graphicsShaderLevel < 30;
            SetDefaultHDRFormat();
            Create();
        }

        /// <inheritdoc/>
        public override void Create()
        {
#if UNITY_EDITOR
            if (config is null)
            {
                // Debug.LogWarningFormat("{0} is null.", config);
                return;
            }

            config.OnDataChange = Create;
#endif
            postProcessData ??= KinoPostProcessData.GetDefaultUserPostProcessData();

            if (config.beforeTransparents.Count > 0)
            {
                customPass_BeforeTransparents = new PostProcessRenderPass(InjectionPoint.BeforeTransparents, postProcessData, config, m_BlitMaterial);
                hasBeforeTransparents         = true;
            }

            if (config.beforePostProcess.Count > 0)
            {
                customPass_BeforePostProcess = new PostProcessRenderPass(InjectionPoint.BeforePostProcess, postProcessData, config, m_BlitMaterial);
                hasBeforePostProcess         = true;
            }

            if (config.afterPostProcess.Count > 0)
            {
                customPass_AfterPostProcess = new PostProcessRenderPass(InjectionPoint.AfterPostProcess, postProcessData, config, m_BlitMaterial);
                hasAfterPostProcess         = true;
            }
        }

#if UNITY_2022_1_OR_NEWER
        private override void SetupRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            RTHandle colorTarget = new RTHandle(renderer.cameraColorTargetHandle);
            RTHandle depthTarget = new RTHandle(renderer.cameraDepthTargetHandle);
#else
        private void SetupRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#endif
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            SetDefaultHDRFormat();
            cameraTargetDescriptor.graphicsFormat = m_DefaultHDRFormat;

            SetMaterialKeywords(ref renderingData);

            customPass_BeforeTransparents?.Setup(cameraTargetDescriptor);
            customPass_BeforePostProcess?.Setup(cameraTargetDescriptor);
            customPass_AfterPostProcess?.Setup(cameraTargetDescriptor);
        }


        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!m_BlitMaterial)
            {
                Debug.LogWarningFormat
                (
                    "Missing Post Processing effect Material. {0} Fullscreen pass will not execute.", GetType().Name
                );
                return;
            }

#if !UNITY_2022_1_OR_NEWER // in 2022+ this is an overridden function and does not need to be called here.
            SetupRenderPasses(renderer, ref renderingData);
#endif

            if (hasBeforeTransparents)
            {
                renderer.EnqueuePass(customPass_BeforeTransparents);
            }

            if (hasBeforePostProcess)
            {
                renderer.EnqueuePass(customPass_BeforePostProcess);
            }

            if (hasAfterPostProcess)
            {
                renderer.EnqueuePass(customPass_AfterPostProcess);
            }
        }
    }
}