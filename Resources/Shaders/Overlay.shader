Shader "Hidden/Kino/PostProcess/Overlay"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        Cull Off ZWrite Off ZTest Always

        // Normal mode (alpha blending)

        Pass // Texture
        {
            Name "Texture: Normal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentTexture
            #define OVERLAY_BLEND_NORMAL
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Normal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_NORMAL
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 8 keys gradient
        {
            Name "8 Keys Gradient: Normal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_GRADIENT_EXT
            #define OVERLAY_BLEND_NORMAL
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        // Screen mode

        Pass // Texture
        {
            Name "Texture: Screen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentTexture
            #define OVERLAY_BLEND_SCREEN
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Screen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_SCREEN
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 8 keys gradient
        {
            Name "8 Keys Gradient: Screen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_GRADIENT_EXT
            #define OVERLAY_BLEND_SCREEN
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        // Overlay mode

        Pass // Texture
        {
            Name "Texture: Overlay"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentTexture
            #define OVERLAY_BLEND_OVERLAY
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Overlay"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_OVERLAY
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 8 keys gradient
        {
            Name "8 Keys Gradient: Overlay"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_GRADIENT_EXT
            #define OVERLAY_BLEND_OVERLAY
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        // Multiply mode

        Pass // Texture
        {
            Name "Texture: Multiply"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentTexture
            #define OVERLAY_BLEND_MULTIPLY
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Multiply"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_MULTIPLY
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 8 keys gradient
        {
            Name "8 Keys Gradient: Multiply"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_GRADIENT_EXT
            #define OVERLAY_BLEND_MULTIPLY
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        // Soft light mode

        Pass // Texture
        {
            Name "Texture: Soft Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentTexture
            #define OVERLAY_BLEND_SOFTLIGHT
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Soft Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_SOFTLIGHT
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 8 keys gradient
        {
            Name "8 Keys Gradient: Soft Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_GRADIENT_EXT
            #define OVERLAY_BLEND_SOFTLIGHT
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        // Hard light mode

        Pass // Texture
        {
            Name "Texture: Hard Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentTexture
            #define OVERLAY_BLEND_HARDLIGHT
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Hard Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_HARDLIGHT
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }

        Pass // 8 keys gradient
        {
            Name "8 Keys Gradient: Hard Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_GRADIENT_EXT
            #define OVERLAY_BLEND_HARDLIGHT
            #include "Includes/Overlay.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}