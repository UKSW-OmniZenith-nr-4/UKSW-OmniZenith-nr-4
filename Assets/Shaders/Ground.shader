Shader "OmniZenith/Ground"
{
    Properties
    {
        [NoScaleOffset] _BaseMap ("Grass texture", 2D) = "white" {}
        _Tint ("Near tint", Color) = (0.3, 0.45, 0.3, 1)
        _FarColor ("Far color", Color) = (0.01, 0.01, 0.015, 1)
        _Tiling ("Tiling", Float) = 80
        _FadeStart ("Fade start (m)", Float) = 3
        _FadeEnd ("Fade end (m)", Float) = 85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _FarColor;
                float _Tiling;
                float _FadeStart;
                float _FadeEnd;
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
                float d = distance(i.posWS.xz, _WorldSpaceCameraPos.xz);
                float f = saturate((d - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));
                f = smoothstep(0.0, 1.0, f);

                half3 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * _Tiling).rgb;
                half3 near = tex * _Tint.rgb;
                half3 col = lerp(near, _FarColor.rgb, f);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}