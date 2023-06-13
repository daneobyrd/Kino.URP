#include "KinoCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

CBUFFER_START(UnityPerMaterial)
TEXTURE2D_X(_InputTexture);

float4 _EdgeColor;
float2 _EdgeThresholds;
float _FillOpacity;

float4 _ColorKey0;
float4 _ColorKey1;
float4 _ColorKey2;
float4 _ColorKey3;
float4 _ColorKey4;
float4 _ColorKey5;
float4 _ColorKey6;
float4 _ColorKey7;

TEXTURE2D(_DitherTexture);
float _DitherStrength;
CBUFFER_END

float4 Fragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    uint2 positionSS = KinoUV * _ScreenSize.xy;

    // Source color
    float4 c0 = LOAD_TEXTURE2D_X(_InputTexture, positionSS);

    // Four sample points of the roberts cross operator
    // TL / BR / TR / BL
    uint2 uv0 = positionSS;
    uint2 uv1 = min(positionSS + uint2(1, 1), _ScreenSize.xy - 1);
    uint2 uv2 = uint2(uv1.x, uv0.y);
    uint2 uv3 = uint2(uv0.x, uv1.y);

#ifdef RECOLOR_EDGE_COLOR

    // Color samples
    float3 c1 = LOAD_TEXTURE2D_X(_InputTexture, uv1).rgb;
    float3 c2 = LOAD_TEXTURE2D_X(_InputTexture, uv2).rgb;
    float3 c3 = LOAD_TEXTURE2D_X(_InputTexture, uv3).rgb;

    // Roberts cross operator
    float3 g1 = c1 - c0.rgb;
    float3 g2 = c3 - c2;
    float g = sqrt(dot(g1, g1) + dot(g2, g2)) * 10;

#endif

#if defined(RECOLOR_EDGE_DEPTH) || defined(RECOLOR_EDGE_NORMAL)

    // Depth samples
    float d0 = LoadSceneDepth(uv0);
    float d1 = LoadSceneDepth(uv1);
    float d2 = LoadSceneDepth(uv2);
    float d3 = LoadSceneDepth(uv3);

#endif

#ifdef RECOLOR_EDGE_DEPTH

    // Roberts cross operator
    float g = length(float2(d1 - d0, d3 - d2)) * 100;

#endif

#ifdef RECOLOR_EDGE_NORMAL

    // Normal samples
    float3 n0 = LoadSceneNormals(uv0);
    float3 n1 = LoadSceneNormals(uv1);
    float3 n2 = LoadSceneNormals(uv2);
    float3 n3 = LoadSceneNormals(uv3);

    // Background removal
#if UNITY_REVERSED_Z
    n0 *= d0 > 0; n1 *= d1 > 0; n2 *= d2 > 0; n3 *= d3 > 0;
#else
    n0 *= d0 < 0; n1 *= d1 < 1; n2 *= d2 < 1; n3 *= d3 < 1;
#endif

    // Roberts cross operator
    float3 g1 = n1 - n0;
    float3 g2 = n3 - n2;
    float g = sqrt(dot(g1, g1) + dot(g2, g2));

#endif

    // Dithering
    uint tw, th;
    _DitherTexture.GetDimensions(tw, th);
    float dither = LOAD_TEXTURE2D(_DitherTexture, positionSS % uint2(tw, th)).x;
    dither = (dither - 0.5) * _DitherStrength;

    // Apply fill gradient.
    float3 fill = _ColorKey0.rgb;
    float lum = Luminance(c0.rgb) + dither;

#ifdef RECOLOR_GRADIENT_LERP
    fill = lerp(fill, _ColorKey1.rgb, saturate((lum - _ColorKey0.w) / (_ColorKey1.w - _ColorKey0.w)));
    fill = lerp(fill, _ColorKey2.rgb, saturate((lum - _ColorKey1.w) / (_ColorKey2.w - _ColorKey1.w)));
    fill = lerp(fill, _ColorKey3.rgb, saturate((lum - _ColorKey2.w) / (_ColorKey3.w - _ColorKey2.w)));
    #ifdef RECOLOR_GRADIENT_EXT
    fill = lerp(fill, _ColorKey4.rgb, saturate((lum - _ColorKey3.w) / (_ColorKey4.w - _ColorKey3.w)));
    fill = lerp(fill, _ColorKey5.rgb, saturate((lum - _ColorKey4.w) / (_ColorKey5.w - _ColorKey4.w)));
    fill = lerp(fill, _ColorKey6.rgb, saturate((lum - _ColorKey5.w) / (_ColorKey6.w - _ColorKey5.w)));
    fill = lerp(fill, _ColorKey7.rgb, saturate((lum - _ColorKey6.w) / (_ColorKey7.w - _ColorKey6.w)));
    #endif
#else
    fill = lum > _ColorKey0.w ? _ColorKey1.rgb : fill;
    fill = lum > _ColorKey1.w ? _ColorKey2.rgb : fill;
    fill = lum > _ColorKey2.w ? _ColorKey3.rgb : fill;
    #ifdef RECOLOR_GRADIENT_EXT
    fill = lum > _ColorKey3.w ? _ColorKey4.rgb : fill;
    fill = lum > _ColorKey4.w ? _ColorKey5.rgb : fill;
    fill = lum > _ColorKey5.w ? _ColorKey6.rgb : fill;
    fill = lum > _ColorKey6.w ? _ColorKey7.rgb : fill;
    #endif
#endif

    float edge = smoothstep(_EdgeThresholds.x, _EdgeThresholds.y, g);
    float3 cb = lerp(c0.rgb, fill, _FillOpacity);
    float3 co = lerp(cb, _EdgeColor.rgb, edge * _EdgeColor.a);
    float4 finalColor = float4(co, c0.a);
    

    
    return finalColor;
}
