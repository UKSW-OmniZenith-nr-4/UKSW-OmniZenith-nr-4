Shader "OmniZenith/Star"
{
    Properties
    {
        _CoreSize ("Core radius (about 1 / Halo Scale)", Range(0.05, 0.8)) = 0.25
        _HaloPower ("Halo falloff", Range(1, 8)) = 4
        _HaloStrength ("Halo strength", Range(0, 2)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _CoreSize;
                float _HaloPower;
                float _HaloStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0;
                float aa = max(fwidth(d), 0.0001);
                float core = 1.0 - smoothstep(_CoreSize - aa, _CoreSize + aa, d);

                float b = max(i.color.r, max(i.color.g, i.color.b));
                float halo = pow(saturate(1.0 - d), _HaloPower) * _HaloStrength * b * b;

                half3 coreCol = lerp(i.color.rgb, half3(b, b, b), 0.12);
                half3 col = coreCol * core + i.color.rgb * halo;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
