Shader "Hidden/Kino/PostProcess/Utility"
{
    HLSLINCLUDE

    #include "Includes/KinoCommon.hlsl"
    
    CBUFFER_START(UnityPerMaterial)
    float4 _FadeColor;
    float _HueShift;
    float _Invert;
    float _Saturation;
    TEXTURE2D_X(_InputTexture);
    CBUFFER_END

    float4 Fragment(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        uint2 positionSS = KinoUV * _ScreenSize.xy;
        float4 c = LOAD_TEXTURE2D_X(_InputTexture, positionSS);
        float3 rgb = c.rgb;

        // Saturation
        rgb = max(0, lerp(Luminance(rgb), rgb, _Saturation));

        // Linear -> sRGB
        rgb = LinearToSRGB(rgb);

        // Hue shift
        float3 hsv = RgbToHsv(rgb);
        hsv.x = frac(hsv.x + _HueShift);
        rgb = HsvToRgb(hsv);

        // Invert
        rgb = lerp(rgb, 1 - rgb, _Invert);

        // Fade
        rgb = lerp(rgb, _FadeColor.rgb, _FadeColor.a);

        // sRGB -> Linear
        c.rgb = SRGBToLinear(rgb);

        return c;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            ENDHLSL
        }
    }
    Fallback Off
}
