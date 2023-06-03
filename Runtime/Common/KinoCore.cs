using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Kino.PostProcessing.KinoCore;

namespace Kino.PostProcessing
{
    public static class KinoCore
    {
        public static string packagePath = "Packages/jp.keijiro.kino.post-processing";

        // Adapted from ScriptableRendererData
        /// <summary>
        /// Returns true if contains renderer feature with specified type.
        /// </summary>
        /// <typeparam name="T">Renderer Feature type.</typeparam>
        /// <returns></returns>
        public static bool TryGetRendererFeature<T>(in ScriptableRendererData rendererData, out T rendererFeature) where T : ScriptableRendererFeature
        {
            foreach (var target in rendererData.rendererFeatures)
            {
                if (target.GetType() == typeof(T))
                {
                    rendererFeature = target as T;
                    return true;
                }
            }

            rendererFeature = null;
            return false;
        }

        public static bool TryGetRendererFeature<T>(out T rendererFeature) where T : ScriptableRendererFeature
        {
            GetReflectedScriptableRendererData(out var rendererData);
            return TryGetRendererFeature<T>(rendererData, out rendererFeature);
        }

        private static void GetReflectedScriptableRendererData(out ScriptableRendererData rendererData)
        {
            if (UniversalRenderPipeline.asset is null)
            {
                rendererData = null;
            }

            // Get first entry in m_RendererDataList array from UniversalRendererData
            rendererData = ((ScriptableRendererData[])
                typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance)
                                                    ?.GetValue(UniversalRenderPipeline.asset))?[0];

            if (rendererData is null)
                throw new NullReferenceException(nameof(rendererData));
        }


        /// <summary>
        /// Meant to be used as a simpler way to determine which blit URP is using and if there is RTHandle support.
        /// </summary>
        public static void SetBlitterKeyword(CommandBuffer cmd)
        {
#if UNITY_2022_1_OR_NEWER
            // UNITY_CORE_BLIT_INCLUDED and RTHandle support
            CoreUtils.SetKeyword(cmd, "USE_BLITTER_API", true);
#else
            // UNIVERSAL_FULLSCREEN_INCLUDED
            CoreUtils.SetKeyword(cmd, "USE_BLITTER_API", false);
#endif
        }

        public enum KinoProfileId
        {
            Streak,  // BeforePostProcess
            Overlay, // AfterPostProcess ↓
            Recolor,
            Glitch,
            Sharpen,
            Utility, // Multipurpose: HueShift, Invert, Fade
            Slice,
            TestCard
        }
        
        public static class ShaderIDs
        {
            internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");

            internal static readonly int SourceColorTexture = Shader.PropertyToID("_CopyColorTexture");

            // Streak
            internal static readonly int StreakColor = Shader.PropertyToID("_StreakColor");
            internal static readonly int SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
            internal static readonly int StreakIntensity = Shader.PropertyToID("_StreakIntensity");
            internal static readonly int Stretch = Shader.PropertyToID("_Stretch");

            internal static readonly int Threshold = Shader.PropertyToID("_Threshold");

            // Overlay
            internal static readonly int OverlayColor = Shader.PropertyToID("_OverlayColor");
            internal static readonly int GradientDirection = Shader.PropertyToID("_GradientDirection");
            internal static readonly int OverlayOpacity = Shader.PropertyToID("_OverlayOpacity");
            internal static readonly int OverlayTexture = Shader.PropertyToID("_OverlayTexture");

            internal static readonly int UseTextureAlpha = Shader.PropertyToID("_UseTextureAlpha");

            // Glitch
            internal static readonly int GlitchSeed = Shader.PropertyToID("_GlitchSeed");
            internal static readonly int BlockSeed1 = Shader.PropertyToID("_BlockSeed1");
            internal static readonly int BlockSeed2 = Shader.PropertyToID("_BlockSeed2");
            internal static readonly int BlockStrength = Shader.PropertyToID("_BlockStrength");
            internal static readonly int BlockStride = Shader.PropertyToID("_BlockStride");
            internal static readonly int Drift = Shader.PropertyToID("_Drift");
            internal static readonly int Jitter = Shader.PropertyToID("_Jitter");
            internal static readonly int Jump = Shader.PropertyToID("_Jump");
            internal static readonly int Shake = Shader.PropertyToID("_Shake");

            // Slice
            internal static readonly int SliceDirection = Shader.PropertyToID("_SliceDirection");
            internal static readonly int Displacement = Shader.PropertyToID("_Displacement");
            internal static readonly int Rows = Shader.PropertyToID("_Rows");

            internal static readonly int SliceSeed = Shader.PropertyToID("_SliceSeed");

            // Sharpen
            internal static readonly int SharpenIntensity = Shader.PropertyToID("_SharpenIntensity");

            // Utility
            internal static readonly int FadeColor = Shader.PropertyToID("_FadeColor");
            internal static readonly int HueShift = Shader.PropertyToID("_HueShift");
            internal static readonly int Invert = Shader.PropertyToID("_Invert");

            internal static readonly int Saturation = Shader.PropertyToID("_Saturation");

            // TestCard
            internal static readonly int TestCardOpacity = Shader.PropertyToID("_TestCardOpacity");
        }
        
    }

    #region Accessing Internal Methods and QoL Extensions

    static class ScriptableRendererInternal
    {
        public static void EnableSwapBufferMSAA(this ScriptableRenderer renderer, bool enable)
        {
            UniversalRenderer universalRenderer = (UniversalRenderer) renderer;
            Type universalRendererType = universalRenderer?.GetType();
            MethodInfo enableSwapBufferMSAAMethod = universalRendererType?.GetMethod("EnableSwapBufferMSAA", BindingFlags.NonPublic | BindingFlags.Instance);
            enableSwapBufferMSAAMethod?.Invoke(renderer, new object[] {enable});
        }

        public static void SwapColorBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            // Debug.Log("Starting SwapColorBuffer()");
            MethodInfo swapColorBufferMethod = typeof(UniversalRenderer).GetMethod("SwapColorBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (swapColorBufferMethod == null)
            {
                Debug.LogError("Could not retrieve SwapColorBufferMethod via Reflection");
                return;
            }

            swapColorBufferMethod.Invoke(renderer, new object[] {cmd});
            // Debug.Log("Performed SwapColorBuffer()");
        }

        public static RenderTargetIdentifier GetCameraColorBackBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            UniversalRenderer universalRenderer = (UniversalRenderer) renderer;
            Type universalRendererType = universalRenderer?.GetType();
            FieldInfo colorBufferSystemField = universalRendererType?.GetField("m_ColorBufferSystem", BindingFlags.NonPublic | BindingFlags.Instance);
            if (colorBufferSystemField is null)
            {
                Debug.LogError("Unable to get m_ColorBufferSystem via System.Reflection.");
                return new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            }

            Type[] cmdBufferType = {typeof(CommandBuffer)};

            object colorBufferSystemValue = colorBufferSystemField.GetValue(renderer);
            Type colorBufferSystemType = colorBufferSystemValue?.GetType();
            MethodInfo methodInfo = colorBufferSystemType?.GetMethod("GetBackBuffer", BindingFlags.Public | BindingFlags.Instance, null, cmdBufferType, null);
            object backBufferObject = methodInfo?.Invoke(colorBufferSystemValue, new object[] {cmd});

            if (backBufferObject == null)
            {
                Debug.LogError("Unable to access m_ColorBufferSystem.GetBackBuffer() method via System.Reflection.");
                return new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            }

            RenderTargetHandle backBufferHandle = (RenderTargetHandle) backBufferObject;
            return backBufferHandle.id;
        }

        public static RenderTargetIdentifier GetCameraColorFrontBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            UniversalRenderer universalRenderer = renderer as UniversalRenderer;
            Type universalRendererType = universalRenderer?.GetType();
            FieldInfo colorBufferSystemField = universalRendererType?.GetField("m_ColorBufferSystem", BindingFlags.NonPublic | BindingFlags.Instance);
            if (colorBufferSystemField is null)
            {
                Debug.LogError("Unable to get m_ColorBufferSystem via System.Reflection.");
                return new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            }

            object colorBufferSystemValue = colorBufferSystemField.GetValue(renderer);
            Type colorBufferSystemType = colorBufferSystemValue?.GetType();
            MethodInfo methodInfo = colorBufferSystemType?.GetMethod("GetFrontBuffer", BindingFlags.Public | BindingFlags.Instance);
            object frontBufferObject = methodInfo?.Invoke(colorBufferSystemValue, new object[] {cmd});

            if (frontBufferObject == null)
            {
                Debug.LogError("Unable to access m_ColorBufferSystem.GetFrontBuffer() method via System.Reflection.");
                return new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            }

            RenderTargetHandle frontBufferHandle = (RenderTargetHandle) frontBufferObject;
            return frontBufferHandle.id;
        }
    }

    static class CustomPostProcessPassExtension
    {
        public static void DrawFullscreen(ref CommandBuffer commandBuffer, RenderTargetIdentifier colorBuffer, Material material, int shaderPassId = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffer);
            // commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1);
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, shaderPassId);
        }

        public static void FinalBlit(this CustomPostProcessPass pass,
                                     CommandBuffer cmd, RenderTargetIdentifier source, ref RenderingData renderingData, Material _material, int passIndex = 0)
        {
            cmd.SetPostProcessSourceTexture(source);
            pass.ConfigureTarget(source);
            pass.ConfigureClear(ClearFlag.All, Color.white);
            pass.Blit(cmd, ref renderingData, _material, passIndex);
        }
    }

    public static class RenderTargetHandleExtension
    {
        public static void Release(this RenderTargetHandle renderTargetHandle, CommandBuffer cmd) { cmd.ReleaseTemporaryRT(renderTargetHandle.id); }
    }

    public static class CameraDataInternal
    {
        public static bool requireSrgbConversion(this CameraData cameraData)
        {
            PropertyInfo getRequireSrgbConversionBool = cameraData.GetType().GetProperty("requireSrgbConversion", BindingFlags.NonPublic | BindingFlags.Instance)
                                                        ?? throw new ArgumentNullException(nameof(cameraData));
            return (bool) getRequireSrgbConversionBool.GetValue(cameraData);
        }
    }

    #endregion
}