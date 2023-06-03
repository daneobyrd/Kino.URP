//
// Replicating Clamp functions as they worked in HDRP 7.1 (original version used by Kino).
//

/*
 *  If we look at the Blitter class (in Core/Runtime/Utilities/Blitter.cs)
 *  we can see that rtHandleScale is used every time "_BlitScaleBias" is set.
 *
 *  public static void BlitCameraTexture(...)
 *  {
 *      Vector2 viewportScale = new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y);
 *      // Will set the correct camera viewport as well.
 *      CoreUtils.SetRenderTarget(cmd, destination);
 *      BlitTexture(cmd, source, viewportScale, material, pass);
 *  }
 *
 * Thus we can *assume* that substituting "_RTHandleScale" with "_BlitScaleBias".
 */

    #if !defined(UNITY_DYNAMIC_SCALING_CLAMPING_INCLUDED) // if missing SRP Core functions

        #if !defined(USE_BLITTER_API) // if not URP 13.1+
        #define RTScaling float2(1, 1)
        #else
        #define RTScaling _RTHandleScale
        #endif

        // Functions to clamp UVs to use when RTHandle system is used.

        float2 ClampUV(float2 UV, float2 texelSize, float numberOfTexels, float2 scale)
        {
            float2 maxCoord = scale - numberOfTexels * texelSize;
            return min(UV, maxCoord);
        }

        float2 ClampUV(float2 UV, float2 texelSize, float numberOfTexels)
        {
            return ClampUV(UV, texelSize, numberOfTexels, RTScaling.xy);
        }

        float2 ClampAndScaleUV(float2 UV, float2 texelSize, float numberOfTexels, float2 scale)
        {
            float2 maxCoord = 1.0f - numberOfTexels * texelSize;
            return min(UV, maxCoord) * scale;
        }

        float2 ClampAndScaleUV(float2 UV, float2 texelSize, float numberOfTexels)
        {
            return ClampAndScaleUV(UV, texelSize, numberOfTexels, RTScaling.xy);
        }

        // This is assuming half a texel offset in the clamp.
        float2 ClampUVForBilinear(float2 UV, float2 texelSize)
        {
            return ClampUV(UV, texelSize, 0.5f);
        }

        float2 ClampUVForBilinear(float2 UV)
        {
            return ClampUV(UV, _ScreenSize.zw, 0.5f);
        }

        float2 ClampAndScaleUVForBilinear(float2 UV, float2 texelSize)
        {
            return ClampAndScaleUV(UV, texelSize, 0.5f);
        }

        // This is assuming full screen buffer and half a texel offset for the clamping.
        float2 ClampAndScaleUVForBilinear(float2 UV)
        {
            return ClampAndScaleUV(UV, _ScreenSize.zw, 0.5f);
        }

        float2 ClampAndScaleUVForPoint(float2 UV)
        {
            return min(UV, 1.0f) * RTScaling.xy;
        }
    #else
        // if defined, then these functions have been moved to SRP Core
        // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"
    #endif

/* 3/27/2023
 * These functions exist in HDRP but not in URP.
 * It seems this will change in the near future, as seen in this commit on the Unity/Graphics repo:
 *
 * // Move HDRP RTHandle clamping functions to SRP Core
 * https://github.com/Unity-Technologies/Graphics/commit/98bd020f9c427721d7f20b260ca61f00b0789e98
 *
 * Previous comments on my Unity Forum post about Blitting discuss how the SRP Blitter class is not "available"
 * prior on versions prior to Unity 2022.1 because of the lack of RTHandles support.
 * 
 * https://forum.unity.com/threads/how-to-blit-in-urp-documentation-unity-blog-post-on-every-blit-function.1211508/#post-7735527
 * https://forum.unity.com/threads/how-to-blit-in-urp-documentation-unity-blog-post-on-every-blit-function.1211508/#post-7740675
 *
 * The files for the RTHandle system are in SRP Core prior to Unity 2022.1,
 * but internal URP render passes did not transition to using RTHandles until 2022.1.
 */

