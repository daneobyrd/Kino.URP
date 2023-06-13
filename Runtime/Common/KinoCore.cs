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
        public static readonly string packagePath = "Packages/jp.keijiro.kino.post-processing";

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
            bool useSRPBlitter = false;
#if UNITY_2022_1_OR_NEWER
            // UNITY_CORE_BLIT_INCLUDED and RTHandle support
            useSRPBlitter = true;
            // #else
            // UNIVERSAL_FULLSCREEN_INCLUDED
#endif
            CoreUtils.SetKeyword(cmd, "USE_BLITTER_API", useSRPBlitter);
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
    }

    #region Accessing Internal Methods and QoL Extensions

    static class ScriptableRendererInternal
    {
        private static void GetUniversalRendererMethodInternal(ScriptableRenderer renderer, string methodName, out MethodInfo rendererMethod)
        {
            UniversalRenderer universalRenderer = renderer as UniversalRenderer;
            rendererMethod = universalRenderer?.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererMethod == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to get {methodName} via System.Reflection");
#endif
            }
        }

        public static void EnableSwapBufferMSAA(this ScriptableRenderer renderer, bool enable)
        {
            const string methodName = "EnableSwapBufferMSAA";
            GetUniversalRendererMethodInternal(renderer, methodName, out MethodInfo enableSwapBufferMSAAMethod);
            enableSwapBufferMSAAMethod.Invoke(renderer, new object[] {enable});
        }

        public static void SwapColorBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            const string methodName = "SwapColorBuffer";
            GetUniversalRendererMethodInternal(renderer, methodName, out MethodInfo swapColorBufferMethod);
            swapColorBufferMethod.Invoke(renderer, new object[] {cmd});
        }

        private static bool TryGetColorBufferSystem(ScriptableRenderer renderer, out FieldInfo colorBufferSystemField)
        {
            UniversalRenderer universalRenderer = renderer as UniversalRenderer;

            const string fieldName = "m_ColorBufferSystem";
            colorBufferSystemField = universalRenderer?.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (colorBufferSystemField != null) return true;
#if UNITY_EDITOR
            Debug.LogError($"Unable to get {fieldName} via System.Reflection.");
#endif
            return false;
        }

        private static RenderTargetIdentifier GetCameraColorBufferInternal(ScriptableRenderer renderer, CommandBuffer cmd, in string methodName)
        {
            RenderTargetIdentifier fallbackCameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

            bool fetchColorBufferSystem = TryGetColorBufferSystem(renderer, out FieldInfo colorBufferSystemField);
            if (fetchColorBufferSystem == false)
            {
                return fallbackCameraTarget;
            }

            object colorBufferSystemObject = colorBufferSystemField.GetValue(renderer);
            if (colorBufferSystemObject == null)
            {
                return fallbackCameraTarget;
            }

            Type colorBufferSystemType = colorBufferSystemObject.GetType();
            MethodInfo methodInfo = colorBufferSystemType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            object bufferObject = methodInfo?.Invoke(colorBufferSystemObject, new object[] {cmd});

            if (methodInfo == null || bufferObject == null)
            {
#if UNITY_EDITOR
                #if !UNITY_2022_1_OR_NEWER // 2021
                {
                    // GetBackBuffer method was added in 2022
                    if (methodName != "GetBackBuffer")
                    {
                        Debug.LogError($"Unable to access m_ColorBufferSystem.{methodName}() via System.Reflection." +
                                         "Check the provided method name and/or whether the method exists.");
                    }
                }
                #endif
#endif
                return fallbackCameraTarget;
            }

            // TODO: Replace usage of RenderTargetHandle with RTHandle for ver. 2022+
            RenderTargetHandle colorBufferHandle = (RenderTargetHandle) bufferObject;
            return colorBufferHandle.id;
        }

        public static RenderTargetIdentifier GetCameraColorBackBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            return GetCameraColorBufferInternal(renderer, cmd, "GetBackBuffer");
        }

        public static RenderTargetIdentifier GetCameraColorFrontBuffer(this ScriptableRenderer renderer, CommandBuffer cmd)
        {
            GetUniversalRendererMethodInternal(renderer, "GetCameraColorFrontBuffer", out var rendererMethod);
            return (RenderTargetIdentifier) rendererMethod.Invoke(renderer, new object[] {cmd});
            // return GetCameraColorBufferInternal(renderer, cmd, "GetFrontBuffer");
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

        public static void FinalBlit(this PostProcessRenderPass pass,
                                     CommandBuffer cmd, RenderTargetIdentifier source, ref RenderingData renderingData, Material _material, int passIndex = 0)
        {
            cmd.SetPostProcessInputTexture(source);
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