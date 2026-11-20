Shader "OmniZenith/SilhouetteImage"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Silhouette (alpha)", 2D) = "white" {}
        _TopColor ("Top color", Color) = (0.02, 0.03, 0.05, 1)
        _BottomColor ("Bottom (haze) color", Color) = (0.08, 0.11, 0.2, 1)
        _GradHeight ("Gradient height (tan)", Float) = 0.06
        _TexMix ("Use texture color", Range(0, 1)) = 0
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _BottomColor;
                float _GradHeight;
                float _TexMix;
                float _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 posWS : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(tex.a - _Cutoff);

                float dist = max(length(i.posWS.xz), 0.001);
                float t = saturate(i.posWS.y / (_GradHeight * dist));
                half3 grad = lerp(_BottomColor.rgb, _TopColor.rgb, t);
                half3 col = grad * lerp(half3(1, 1, 1), tex.rgb, _TexMix);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
