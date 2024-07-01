Shader "Hidden/Kino/PostProcess/Recolor"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 4 keys, fixed gradient
        Pass
        {
            Name "Recolor Edge Color: 4 Keys Fixed Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_COLOR
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Depth: 4 Keys Fixed Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_DEPTH
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Normal: 4 Keys Fixed Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_NORMAL
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }

        // 8 keys, fixed gradient
        Pass
        {
            Name "Recolor Edge Color: 8 Keys Fixed Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_COLOR
            #define RECOLOR_GRADIENT_EXT
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Depth: 8 Keys Fixed Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_DEPTH
            #define RECOLOR_GRADIENT_EXT
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Normal: 8 Keys Fixed Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_NORMAL
            #define RECOLOR_GRADIENT_EXT
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }

        // 4 keys, blend gradient
        Pass
        {
            Name "Recolor Edge Color: 4 Keys Blend Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_COLOR
            #define RECOLOR_GRADIENT_LERP
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Depth: 4 Keys Blend Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_DEPTH
            #define RECOLOR_GRADIENT_LERP
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Normal: 4 Keys Blend Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_NORMAL
            #define RECOLOR_GRADIENT_LERP
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }

        // 8 keys, blend gradient
        Pass
        {
            Name "Recolor Edge Color: 8 Keys Blend Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_COLOR
            #define RECOLOR_GRADIENT_EXT
            #define RECOLOR_GRADIENT_LERP
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Depth: 8 Keys Blend Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_DEPTH
            #define RECOLOR_GRADIENT_EXT
            #define RECOLOR_GRADIENT_LERP
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Recolor Edge Normal: 8 Keys Blend Gradient"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #define RECOLOR_EDGE_NORMAL
            #define RECOLOR_GRADIENT_EXT
            #define RECOLOR_GRADIENT_LERP
            #include "Includes/Recolor.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
