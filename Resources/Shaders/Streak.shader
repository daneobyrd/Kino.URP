Shader "Hidden/Kino/PostProcess/Streak"
{
    HLSLINCLUDE
    #include "Includes/KinoCommon.hlsl"

    // CBUFFER_START(UnityPerMaterial)
    TEXTURE2D_X(_SourceTexture);
    TEXTURE2D(_InputTexture);
    TEXTURE2D(_SourceTexLowMip);

    float4 _InputTexture_TexelSize;

    float _Threshold;
    float _Stretch;
    float _StreakIntensity;
    float3 _StreakColor;
    // CBUFFER_END

    // Prefilter: Shrink horizontally and apply threshold.
    half4 FragmentPrefilter(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        uint2 ss = KinoUV * _ScreenSize.xy - float2(0, 0.5);
        half3 c0 = LOAD_TEXTURE2D_X(_SourceTexture, ss).rgb;
        half3 c1 = LOAD_TEXTURE2D_X(_SourceTexture, ss + uint2(0, 1)).rgb;
        half3 c = (c0 + c1) / 2;

        half br = max(c.r, max(c.g, c.b));
        c *= max(0, br - _Threshold) / max(br, 1e-5);
        
        return EncodeHDR(half4(c, 1));
    }

    // Downsampler
    half4 FragmentDownsample(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = KinoUV;
        const float dx = _InputTexture_TexelSize.x;

        // 6-tap?
        float u0 = uv.x - dx * 5;
        float u1 = uv.x - dx * 3;
        float u2 = uv.x - dx * 1;
        float u3 = uv.x + dx * 1;
        float u4 = uv.x + dx * 3;
        float u5 = uv.x + dx * 5;

        half3 c0 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u0, uv.y)).rgb;
        half3 c1 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u1, uv.y)).rgb;
        half3 c2 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u2, uv.y)).rgb;
        half3 c3 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u3, uv.y)).rgb;
        half3 c4 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u4, uv.y)).rgb;
        half3 c5 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u5, uv.y)).rgb;

        half4 c = half4((c0 + c1 * 2 + c2 * 3 + c3 * 3 + c4 * 2 + c5) / 12, 1);

        return c;
    }

    // Upsampler
    half4 FragmentUpsample(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = KinoUV;
        const float dx = _InputTexture_TexelSize.x * 1.5;

        float u0 = uv.x - dx;
        float u1 = uv.x;
        float u2 = uv.x + dx;

        half3 c0 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u0, uv.y)).rgb;
        half3 c1 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u1, uv.y)).rgb;
        half3 c2 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u2, uv.y)).rgb;
        half3 c3 = SAMPLE_TEXTURE2D(_SourceTexLowMip, sampler_LinearClamp, uv).rgb;

        half4 c = float4(lerp(c3, (c0 / 4) + (c1 / 2) + (c2 / 4), _Stretch), 1);

        return c;
    }

    // Final composition
    half4 FragmentComposition(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = KinoUV;
        uint2 positionSS = uv * _ScreenSize.xy;
        const float dx = _InputTexture_TexelSize.x * 1.5;

        float u0 = uv.x - dx;
        float u1 = uv.x;
        float u2 = uv.x + dx;

        half3 c0 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u0, uv.y)).rgb;
        half3 c1 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u1, uv.y)).rgb;
        half3 c2 = SAMPLE_TEXTURE2D(_InputTexture, sampler_LinearClamp, float2(u2, uv.y)).rgb;
        half3 c3 = LOAD_TEXTURE2D_X(_SourceTexture, positionSS).rgb;
        half3 cf = (c0 / 4 + c1 / 2 + c2 / 4) * _StreakColor * _StreakIntensity * 5;

        half4 c = half4(cf + c3, 1);

        return c;
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentPrefilter
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentDownsample
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentUpsample
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentComposition
            ENDHLSL
        }
    }
    Fallback Off
}