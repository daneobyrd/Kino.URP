Shader "Hidden/Kino/PostProcess/Overlay"
{
    SubShader
    {
        HLSLINCLUDE
        #include "Includes/Overlay.hlsl"
        // #pragma multi_compile_local _ _LINEAR_TO_SRGB_CONVERSION
        ENDHLSL
        Cull Off ZWrite Off ZTest Always

        // Normal mode (alpha blending)

        Pass // Texture
        {
            Name "Texture: Normal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentTexture
            #define OVERLAY_BLEND_NORMAL
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Normal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_NORMAL
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
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Screen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_SCREEN
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
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Overlay"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_OVERLAY
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
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Multiply"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_MULTIPLY
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
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Soft Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_SOFTLIGHT
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
            ENDHLSL
        }

        Pass // 3 keys gradient
        {
            Name "3 Keys Gradient: Hard Light"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentGradient
            #define OVERLAY_BLEND_HARDLIGHT
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
            ENDHLSL
        }
    }
    Fallback Off
}