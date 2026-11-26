Shader "OmniZenith/BodySprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Point colour", Color) = (1,1,1,1)
        _Detail ("Detail 0..1", Range(0,1)) = 1
        _PointRel ("Point size / quad size", Float) = 1
        _TexScale ("Quad size / texture size", Float) = 1
        _UsePhase ("Use phase", Float) = 0
        _Phase ("Phase", Range(0,1)) = 0.5
        _DiscR ("Disc radius", Float) = 0.9
        _CoreSize ("Point core radius", Range(0.05, 0.8)) = 0.25
        _HaloPower ("Halo falloff", Range(1, 8)) = 4
        _HaloStrength ("Halo strength", Range(0, 2)) = 0.35
        _MaxLod ("Blur at detail 0 (mip level)", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float _Detail;
                float _PointRel;
                float _TexScale;
                float _UsePhase;
                float _Phase;
                float _DiscR;
                float _CoreSize;
                float _HaloPower;
                float _HaloStrength;
                float _MaxLod;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;

                // point look (like a star): core + glow
                float ds = length(p) / max(_PointRel, 0.0001);
                float aa = max(fwidth(ds), 0.0001);
                float core = 1.0 - smoothstep(_CoreSize - aa, _CoreSize + aa, ds);
                float b = max(_Tint.r, max(_Tint.g, _Tint.b));
                float halo = pow(saturate(1.0 - ds), _HaloPower) * _HaloStrength * b * b;
                float3 coreCol = lerp(_Tint.rgb, float3(b, b, b), 0.12);
                float3 star = coreCol * core + _Tint.rgb * halo;

                // picture look (gets sharper as we zoom in)
                float2 pT = p * _TexScale;
                float2 uvT = pT * 0.5 + 0.5;
                float inside = step(0.0, uvT.x) * step(uvT.x, 1.0) * step(0.0, uvT.y) * step(uvT.y, 1.0);
                float lod = (1.0 - _Detail) * _MaxLod;
                float4 t = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uvT, lod);
                float texA = t.a * inside;
                float3 rgb = t.rgb;

                if (_UsePhase > 0.5)
                {
                    float rr = length(pT) / _DiscR;
                    if (rr < 1.0)
                    {
                        float3 n = float3(pT / _DiscR, sqrt(saturate(1.0 - rr * rr)));
                        float ang = _Phase * 6.2831853;
                        float3 l = normalize(float3(sin(ang), 0.0, -cos(ang)));
                        float lit = smoothstep(-0.02, 0.15, dot(n, l));
                        rgb *= lit + 0.04;
                    }
                }

                float a = texA * _Detail;
                float3 col = star * (1.0 - _Detail) + rgb * a;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
