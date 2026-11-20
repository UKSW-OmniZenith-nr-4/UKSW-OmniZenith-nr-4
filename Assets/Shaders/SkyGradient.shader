Shader "OmniZenith/SkyGradient"
{
    Properties
    {
        _Zenith ("Zenith (top)", Color) = (0.005, 0.01, 0.03, 1)
        _Mid ("Middle", Color) = (0.03, 0.05, 0.12, 1)
        _Horizon ("Horizon (bottom)", Color) = (0.12, 0.16, 0.28, 1)
        _Ground ("Ground", Color) = (0.01, 0.01, 0.015, 1)
        _Power ("Gradient power", Float) = 0.5
        _MidPos ("Middle position (0..1)", Range(0.05, 0.95)) = 0.4
        _GroundBlur ("Ground edge softness", Range(0.001, 0.3)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Cull Front
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith;
                half4 _Mid;
                half4 _Horizon;
                half4 _Ground;
                float _Power;
                float _MidPos;
                float _GroundBlur;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float h = normalize(i.dir).y;
                float t = pow(saturate(h), _Power);

                half3 lower = lerp(_Horizon.rgb, _Mid.rgb, saturate(t / _MidPos));
                half3 upper = lerp(_Mid.rgb, _Zenith.rgb, saturate((t - _MidPos) / (1.0 - _MidPos)));
                half3 sky = lerp(lower, upper, step(_MidPos, t));

                float g = smoothstep(-_GroundBlur, _GroundBlur, -h);
                half3 col = lerp(sky, _Ground.rgb, g);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
