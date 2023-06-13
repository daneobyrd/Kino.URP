#ifndef KINO_COMMON_INCLUDED
#define KINO_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

half4 EncodeHDR(half3 color)
{
    #if _USE_RGBM
    half4 outColor = EncodeRGBM(color);
    #else
    half4 outColor = half4(color, 1.0);
    #endif

    #if UNITY_COLORSPACE_GAMMA
return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
    #else
    return outColor;
    #endif
}

half3 DecodeHDR(half4 color)
{
    #if UNITY_COLORSPACE_GAMMA
    color.xyz *= color.xyz; // γ to linear
    #endif

    #if _USE_RGBM
    return DecodeRGBM(color);
    #else
    return color.xyz;
    #endif
}

/*#define LOAD_HDR_TEXTURE2D(texture, uv)     DecodeHDR(LOAD_TEXTURE2D(texture, uv))
#define LOAD_HDR_TEXTURE2D_X(texture, uv)   DecodeHDR(LOAD_TEXTURE2D_X(texture, uv))
#define SAMPLE_HDR_TEXTURE2D(texture, sampler, uv)   DecodeHDR(SAMPLE_TEXTURE2D(texture, sampler, uv))
#define SAMPLE_HDR_TEXTURE2D_X(texture, sampler, uv) DecodeHDR(SAMPLE_TEXTURE2D_X(texture, sampler, uv))*/

// Set in KinoCore.cs
#if USE_BLITTER_API // URP 13.1 (UNITY 2022.1)

    // UNITY_CORE_BLIT_INCLUDED
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #define KinoUV          input.texcoord

#else               // #if VERSION_LOWER(13, 1)

// UNIVERSAL_FULLSCREEN_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
#define KinoUV          input.uv

#endif

#endif
