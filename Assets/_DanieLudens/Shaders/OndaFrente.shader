// Frente de la onda del amuleto (el anillo de luz que cambia el mundo).
// Es el mismo anillo del video: 3 píxeles de arte de grosor, borde blanco,
// luego azul y un tramado (ajedrez) hacia adentro, más un resplandor suave detrás.
//
// Se calcula en píxeles de arte (_PixelSize unidades por píxel), así el anillo
// queda nítido a cualquier radio sin necesitar una imagen por tamaño.
// Lo controla WorldWave.cs; no hace falta tocar nada aquí.
Shader "Mohan/OndaFrente"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _ColorBorde ("Borde", Color) = (1, 1, 1, 1)
        _ColorBrillo ("Brillo", Color) = (0.737, 0.824, 1, 1)
        _Centro ("Centro (mundo)", Vector) = (0, 0, 0, 0)
        _Radio ("Radio (unidades)", Float) = 0
        _PixelSize ("Tamaño del píxel de arte", Float) = 0.03125
        _Intensidad ("Intensidad", Range(0, 1)) = 1
        _AnchoResplandor ("Ancho del resplandor (píxeles de arte)", Float) = 40
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
                float4 _ColorBorde;
                float4 _ColorBrillo;
                float4 _Centro;
                float _Radio;
                float _PixelSize;
                float _Intensidad;
                float _AnchoResplandor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Todo en píxeles de arte: así cae en la rejilla del pixel art
                float2 p = floor(i.positionWS.xy / _PixelSize);
                float2 c = floor(_Centro.xy / _PixelSize);
                float r = round(_Radio / _PixelSize);
                float2 dxy = p - c;
                float d = r - length(dxy); // 0 en el borde de afuera, crece hacia adentro

                // Resplandor: transparente a _AnchoResplandor px adentro, más fuerte en el borde
                half4 color = 0;
                if (d >= 0)
                {
                    float t = saturate(1 - d / _AnchoResplandor);
                    color = half4(_ColorBrillo.rgb, 0.35 * _Intensidad * t);
                }

                // Anillo de 3 píxeles: blanco, azul y tramado
                bool ajedrez = frac((p.x + p.y) * 0.5) < 0.25;
                if (d >= -0.5 && d < 3)
                {
                    if (d < 1) color = half4(_ColorBorde.rgb, _Intensidad);
                    else if (d < 2 || ajedrez) color = half4(_ColorBrillo.rgb, _Intensidad);
                }
                return color;
            }
            ENDHLSL
        }
    }
}
