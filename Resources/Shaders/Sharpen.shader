Shader "Hidden/Kino/PostProcess/Sharpen"
{
    HLSLINCLUDE

    #include "Includes/KinoCommon.hlsl"

    CBUFFER_START(UnityPerMaterial)
    TEXTURE2D_X(_InputTexture);
    float _SharpenIntensity;
    CBUFFER_END
    
    float4 SampleInput(int2 coord)
    {
        coord = min(max(0, coord), _ScreenSize.xy - 1);
        return LOAD_TEXTURE2D_X(_InputTexture, coord);
    }

    float4 Fragment(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        int2 positionSS = KinoUV * _ScreenSize.xy;

        float4 c0 = SampleInput(positionSS + int2(-1, -1));
        float4 c1 = SampleInput(positionSS + int2( 0, -1));
        float4 c2 = SampleInput(positionSS + int2(+1, -1));

        float4 c3 = SampleInput(positionSS + int2(-1, 0));
        float4 c4 = SampleInput(positionSS + int2( 0, 0));
        float4 c5 = SampleInput(positionSS + int2(+1, 0));

        float4 c6 = SampleInput(positionSS + int2(-1, +1));
        float4 c7 = SampleInput(positionSS + int2( 0, +1));
        float4 c8 = SampleInput(positionSS + int2(+1, +1));

        float4 finalColor = c4 - (c0 + c1 + c2 + c3 - 8 * c4 + c5 + c6 + c7 + c8) * _SharpenIntensity;
        
        return finalColor;
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
