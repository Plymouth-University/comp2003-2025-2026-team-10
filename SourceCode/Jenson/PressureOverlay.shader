Shader "Custom/PressureOverlay"
{
    Properties
    {
        _PressureMapA ("Pressure Map A", 2D) = "black" {}
        _PressureMapB ("Pressure Map B", 2D) = "black" {}
        _Blend ("Blend", Range(0,1)) = 0
        _Alpha ("Alpha", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_PressureMapA);
            TEXTURE2D(_PressureMapB);
            SAMPLER(sampler_PressureMapA);
            SAMPLER(sampler_PressureMapB);

            CBUFFER_START(UnityPerMaterial)
                float4 _PressureMapA_ST;
                float  _Blend;
                float  _Alpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pA = SAMPLE_TEXTURE2D(_PressureMapA, sampler_PressureMapA, IN.uv).r;
                float pB = SAMPLE_TEXTURE2D(_PressureMapB, sampler_PressureMapB, IN.uv).r;
                float p  = lerp(pA, pB, _Blend);

                // Blue as low, going up to red as high pressure.
                float3 col;
                if (p < 0.25)
                    col = lerp(float3(0,0,1), float3(0,1,1), p * 4);
                else if (p < 0.5)
                    col = lerp(float3(0,1,1), float3(0,1,0), (p - 0.25) * 4);
                else if (p < 0.75)
                    col = lerp(float3(0,1,0), float3(1,1,0), (p - 0.5) * 4);
                else
                    col = lerp(float3(1,1,0), float3(1,0,0), (p - 0.75) * 4);

                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }
}