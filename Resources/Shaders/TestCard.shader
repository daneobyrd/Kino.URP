Shader "Hidden/Kino/PostProcess/TestCard"
{
    HLSLINCLUDE

    #include "Includes/KinoCommon.hlsl"
    // CBUFFER_START(UnityPerMaterial)
    TEXTURE2D_X(_InputTexture);
    float _TestCardOpacity;
    // CBUFFER_END
    
    float3 TestPattern(float2 uv)
    {
        float scale = 27 / _ScreenSize.y;        // Grid scale
        float2 p0 = (uv - 0.5) * _ScreenSize.xy; // Position (pixel)
        float2 p1 = p0 * scale;                  // Position (half grid)
        float2 p2 = p1 / 2 - 0.5;                // Position (grid)

        // Size of inner area
        half aspect = (float)_ScreenSize.x / _ScreenSize.y;
        half2 area = half2(floor(6.5 * aspect) * 2 + 1, 13);

        // Crosshair and grid lines
        half2 ch = abs(p0);
        half2 grid = (1 - abs(frac(p2) - 0.5) * 2) / scale;
        half c1 = min(min(ch.x, ch.y), min(grid.x, grid.y)) < 1 ? 1 : 0.5;

        // Outer area checker
        half2 checker = frac(floor(p2) / 2) * 2;
        if (any(abs(p1) > area)) c1 = abs(checker.x - checker.y);

        half corner = sqrt(8) - length(abs(p1) - area + 4); // Corner circles
        half circle = 12 - length(p1);                      // Big center circle
        half mask = saturate(circle / scale);               // Center circls mask

        // Grayscale bars
        half bar1 = saturate(p1.y < 5 ? floor(p1.x / 4 + 3) / 5 : p1.x / 16 + 0.5);
        if (abs(5 - p1.y) < 4 * mask) c1 = bar1;

        // Basic color bars
        half3 bar2 = HsvToRgb(float3((p1.y > -5 ? floor(p1.x / 4) / 6 : p1.x / 16) + 0.5, 1, 1));
        float3 rgb = abs(-5 - p1.y) < 4 * mask ? bar2 : saturate(c1);

        // Circle lines
        rgb = lerp(rgb, 1, saturate(1.5 - abs(max(circle, corner)) / scale));

        // #if _LINEAR_TO_SRGB_CONVERSION
        // rgb = LinearToSRGB(rgb);
        // #endif
        
        return rgb;
    }

    float4 Fragment(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Source image
        uint2 positionSS = KinoUV * _ScreenSize.xy;
        float4 c = LOAD_TEXTURE2D_X(_InputTexture, positionSS);

        // Blend the test pattern in sRGB.
        c.rgb = LinearToSRGB(c.rgb);
        c.rgb = lerp(c.rgb, TestPattern(KinoUV), _TestCardOpacity);
        c.rgb = SRGBToLinear(c.rgb);

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
            #pragma fragment Fragment
            ENDHLSL
        }
    }

    Fallback Off
}
