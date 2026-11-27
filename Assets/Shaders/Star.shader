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

                // alpha = relative brightness 0..1, rgb = pure hue at full strength
                float br = i.color.a;
                half3 hue = i.color.rgb;

                // Core: ALWAYS powerful (small and sharp), nearly white
                half3 coreCol = lerp(hue, half3(1, 1, 1), 0.03 + 0.3 * br * br) * (0.18 + 0.82 * br);

                // Halo: faint stars still glow clearly, bright ones glow much more
                float f = saturate(1.0 - d);
                float halo = pow(f, _HaloPower) * _HaloStrength * (0.05 + 0.95 * br * br);
                float glow = pow(f, 1.6) * _HaloStrength * 2.0 * br * br * br * br;

                half3 col = coreCol * core + hue * (halo + glow);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
