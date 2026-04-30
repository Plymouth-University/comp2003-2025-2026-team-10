Shader "Water/WaterFlow"
{
    Properties
    {
        _HeightmapA  ("Frame A", 2D) = "gray" {}
        _HeightmapB  ("Frame B", 2D) = "gray" {}
        _FlowMap     ("Flow Map", 2D) = "black" {}
        _Blend       ("Blend", Range(0,1)) = 0
        _FlowScale   ("Flow Scale", Float) = 0.01
        _NormalStrength ("Normal Strength", Float) = 2.0
        _Smoothness  ("Smoothness", Range(0,1)) = 0.9
        _Metallic    ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_HeightmapA);   SAMPLER(sampler_HeightmapA);
            TEXTURE2D(_HeightmapB);   SAMPLER(sampler_HeightmapB);
            TEXTURE2D(_FlowMap);      SAMPLER(sampler_FlowMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _HeightmapA_ST;
                float4 _HeightmapB_ST;
                float4 _FlowMap_ST;
                float  _Blend;
                float  _FlowScale;
                float  _NormalStrength;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            float SampleHeight(float2 uv)
            {
                float2 flow = SAMPLE_TEXTURE2D_LOD(_FlowMap, sampler_FlowMap, uv, 0).rg * _FlowScale;
                float2 uvA = uv + flow * _Blend;
                float2 uvB = uv - flow * (1.0 - _Blend);
                float hA = SAMPLE_TEXTURE2D_LOD(_HeightmapA, sampler_HeightmapA, uvA, 0).r;
                float hB = SAMPLE_TEXTURE2D_LOD(_HeightmapB, sampler_HeightmapB, uvB, 0).r;
                return lerp(hA, hB, _Blend);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 flow = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, uv).rg * _FlowScale;

                float2 uvA = uv + flow * _Blend;
                float2 uvB = uv - flow * (1.0 - _Blend);

                float heightA = SAMPLE_TEXTURE2D(_HeightmapA, sampler_HeightmapA, uvA).r;
                float heightB = SAMPLE_TEXTURE2D(_HeightmapB, sampler_HeightmapB, uvB).r;
                float height  = lerp(heightA, heightB, _Blend);

                // Derive normals from neighbouring height samples
                float texelSize = 0.002;
                float hR = SampleHeight(uv + float2(texelSize, 0));
                float hL = SampleHeight(uv - float2(texelSize, 0));
                float hU = SampleHeight(uv + float2(0, texelSize));
                float hD = SampleHeight(uv - float2(0, texelSize));
                float3 normal = normalize(float3(
                    (hL - hR) * _NormalStrength,
                    1.0,
                    (hD - hU) * _NormalStrength
                ));
                normal = normalize(TransformObjectToWorldNormal(normal));

                // Water colours - deep and rich
                half3 deepColour    = half3(0.0,  0.08, 0.25);
                half3 shallowColour = half3(0.05, 0.6,  0.8);
                half3 foamColour    = half3(0.9,  0.95, 1.0);

                half3 waterColour = lerp(deepColour, shallowColour, saturate(height * 2.0));
                waterColour = lerp(waterColour, foamColour, saturate((height - 0.6) * 4.0));

                // Transparency - deep areas much more opaque for depth illusion
                float alpha = lerp(0.4, 0.85, saturate(height * 2.0));

                // Lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = IN.positionWS;
                lightingInput.normalWS   = normal;
                lightingInput.viewDirectionWS = normalize(GetCameraPositionWS() - IN.positionWS);
                lightingInput.shadowCoord = float4(0,0,0,0);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = waterColour;
                surfaceData.smoothness  = _Smoothness;
                surfaceData.metallic    = _Metallic;
                surfaceData.alpha       = alpha;

                half4 colour = UniversalFragmentPBR(lightingInput, surfaceData);
                colour.a = alpha;
                return colour;
            }
            ENDHLSL
        }
    }
}
