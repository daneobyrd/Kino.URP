Shader "Hidden/Kino/PostProcess/Glitch"
{
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert            
            #pragma fragment Fragment
            #include "Includes/Glitch.hlsl"
            ENDHLSL
        }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #define GLITCH_BASIC
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Includes/Glitch.hlsl"
            ENDHLSL
        }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #define GLITCH_BLOCK
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Includes/Glitch.hlsl"
            ENDHLSL
        }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #define GLITCH_BASIC
            #define GLITCH_BLOCK
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Includes/Glitch.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
