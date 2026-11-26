Shader "OmniZenith/Star"
{
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

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
<<<<<<< Updated upstream
                float d = length(IN.uv - 0.5) * 2.0;
                float a = saturate(1.0 - d);
                a = a * a;
                return half4(IN.color.rgb * a, 1);
=======
                float d = length(i.uv - 0.5) * 2.0;
                float aa = max(fwidth(d), 0.0001);
                float core = 1.0 - smoothstep(_CoreSize - aa, _CoreSize + aa, d);

                float b = max(i.color.r, max(i.color.g, i.color.b));
                float halo = pow(saturate(1.0 - d), _HaloPower) * _HaloStrength * b * b;

                half3 coreCol = lerp(i.color.rgb, half3(b, b, b), 0.12);
                half3 col = coreCol * core + i.color.rgb * halo;
                return half4(col, 1);
>>>>>>> Stashed changes
            }
            ENDHLSL
        }
    }
}