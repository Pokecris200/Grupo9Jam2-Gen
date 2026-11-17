// Pixel art rain drawn on a quad that covers the screen.
// Each column of art pixels may hold falling drops; everything snaps to the art pixel grid,
// so the rain stays crisp like the rest of the game. PixelRain.cs drives it.
Shader "Mohan/PixelRain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Color", Color) = (0.8, 0.85, 0.95, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.5
        _PixelSize ("Art Pixel Size", Float) = 0.03125
        _Spacing ("Column Spacing (art pixels)", Float) = 4
        _Density ("Density", Range(0, 1)) = 0.4
        _Speed ("Speed (units per second)", Float) = 12
        _Length ("Drop Length (art pixels)", Float) = 6
        _Gap ("Gap Between Drops (art pixels)", Float) = 70
        _Slant ("Slant", Float) = 0.25
        _Parallax ("Parallax (1 = moves with the world)", Range(0, 1)) = 1
        _RainTime ("Rain Time", Float) = 0
        _Seed ("Seed", Float) = 0
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
                float4 _Color;
                float _Alpha;
                float _PixelSize;
                float _Spacing;
                float _Density;
                float _Speed;
                float _Length;
                float _Gap;
                float _Slant;
                float _Parallax;
                float _RainTime;
                float _Seed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };                                    // Vertex input
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; }; // Data for the fragment

            // Pass the world position to the fragment
            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz); // World position
                o.positionHCS = TransformWorldToHClip(o.positionWS);     // Screen position
                return o;
            }

            // Repeatable random number between 0 and 1 for a column
            float Hash (float n)
            {
                return frac(sin(n * 12.9898 + _Seed * 78.233) * 43758.5453);
            }

            // Draw the drop pixels of this column
            half4 frag (Varyings i) : SV_Target
            {
                float2 q = i.positionWS.xy - _WorldSpaceCameraPos.xy * (1 - _Parallax); // Far layers follow the camera a bit
                q.x += q.y * _Slant;                                                       // Diagonal fall
                float2 p = floor(q / _PixelSize);                                          // This pixel on the art grid

                if (fmod(abs(p.x), _Spacing) >= 1) return 0; // Only one pixel wide columns
                float column = floor(p.x / _Spacing);         // Column index
                float h = Hash(column);                       // Random value of this column
                if (h > _Density) return 0;                   // Empty column

                float speed = _Speed / _PixelSize * (0.8 + 0.4 * Hash(column + 17)); // Pixels per second, a bit different per column
                float period = _Gap + _Length;                                       // One drop and its gap
                float y = p.y + _RainTime * speed + h * 997;                         // Moves down over time
                float m = frac(y / period) * period;                                 // Position inside the drop cycle
                if (m >= _Length) return 0;                                          // Gap between drops

                float fade = lerp(1, 0.35, m / _Length);          // Bright head, faint tail
                return half4(_Color.rgb, _Alpha * _Color.a * fade); // Drop pixel
            }
            ENDHLSL
        }
    }
}
