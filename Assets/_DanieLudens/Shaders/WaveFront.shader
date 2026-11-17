// Ring of light of the amulet wave (the front that changes the world).
// Same ring as the intro video: 3 art pixels thick, white edge, then blue and a
// checkerboard dither inward, with a soft glow behind it.
// Everything is computed in art pixels (_PixelSize units per pixel), so the ring
// stays crisp at any radius without one image per size. WorldWave.cs drives it.
Shader "Mohan/WaveFront"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.737, 0.824, 1, 1)
        _Center ("Center (world)", Vector) = (0, 0, 0, 0)
        _Radius ("Radius (units)", Float) = 0
        _PixelSize ("Art Pixel Size", Float) = 0.03125
        _Intensity ("Intensity", Range(0, 1)) = 1
        _GlowWidth ("Glow Width (art pixels)", Float) = 40
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _EdgeColor;
                float4 _GlowColor;
                float4 _Center;
                float _Radius;
                float _PixelSize;
                float _Intensity;
                float _GlowWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };                       // Vertex input
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; }; // Data for the fragment

            // Pass the world position to the fragment
            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz); // World position
                o.positionHCS = TransformWorldToHClip(o.positionWS);     // Screen position
                return o;
            }

            // Draw the glow and the ring in art pixels
            half4 frag (Varyings i) : SV_Target
            {
                float2 p = floor(i.positionWS.xy / _PixelSize); // This pixel on the art grid
                float2 c = floor(_Center.xy / _PixelSize);      // Center on the art grid
                float r = round(_Radius / _PixelSize);          // Radius in art pixels
                float d = r - length(p - c);                    // 0 at the outer edge, grows inward

                half4 color = 0; // Transparent by default
                if (d >= 0)      // Inside the circle
                {
                    float t = saturate(1 - d / _GlowWidth);                  // Stronger near the edge
                    color = half4(_GlowColor.rgb, 0.35 * _Intensity * t);    // Soft glow
                }

                bool checker = frac((p.x + p.y) * 0.5) < 0.25; // Checkerboard dither
                if (d >= -0.5 && d < 3)                          // 3 pixel ring
                {
                    if (d < 1) color = half4(_EdgeColor.rgb, _Intensity);                    // White edge
                    else if (d < 2 || checker) color = half4(_GlowColor.rgb, _Intensity);    // Blue, then dithered
                }
                return color;
            }
            ENDHLSL
        }
    }
}
