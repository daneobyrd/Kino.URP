Shader "Hidden/Kino/PostProcess/Slice"
{
    HLSLINCLUDE

    #include "Includes/KinoCommon.hlsl"
    #include "Includes/ClampUV.hlsl"

    CBUFFER_START(UnityPerMaterial)
    TEXTURE2D_X(_InputTexture);

    float2 _SliceDirection;
    float _Displacement;
    float _Rows;
    uint _SliceSeed;
    CBUFFER_END
    
    float4 Fragment(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        const float aspect = (float)_ScreenSize.x / _ScreenSize.y;
        const float inv_aspect = (float)_ScreenSize.y / _ScreenSize.x;

        const float2 axis1 = _SliceDirection;
        const float2 axis2 = float2(-axis1.y, axis1.x);

        float2 uv = KinoUV;
        float param = dot(uv - 0.5, axis2 * float2(aspect, 1));
        uint seed = _SliceSeed + (uint)((param + 10) * _Rows + 0.5);
        float delta = Hash(seed) - 0.5;

        uv += axis1 * delta * _Displacement * float2(inv_aspect, 1);

        uv = ClampAndScaleUVForBilinear(uv);

        float4 finalColor = SAMPLE_TEXTURE2D_X(_InputTexture, sampler_LinearClamp, uv);
        
        return finalColor;
    }

    ENDHLSL
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            ENDHLSL
        }
    }
    Fallback Off
}