Shader "ColorBlit"
{
    HLSLINCLUDE
    #pragma exclude_renderers gles
    #pragma multi_compile_local_fragment _ _LINEAR_TO_SRGB_CONVERSION
    // #pragma multi_compile _ _USE_DRAW_PROCEDURAL

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    ENDHLSL


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            struct Attributes
            {
                float4 positionHCS : POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Note: The pass is setup with a mesh already in clip
                // space, that's why, it's enough to just output vertex
                // positions
                output.positionCS = float4(input.positionHCS.xyz, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                output.positionCS.y *= -1;
                #endif

                output.uv = input.uv;
                return output;
            }

            TEXTURE2D_X(_SourceTexture);
            SAMPLER(sampler_SourceTexture);

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D_X(_SourceTexture, sampler_SourceTexture, input.uv);

                #ifdef _LINEAR_TO_SRGB_CONVERSION
                color = LinearToSRGB(color);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}