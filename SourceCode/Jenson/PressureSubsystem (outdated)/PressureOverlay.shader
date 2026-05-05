//For updated version implementation instructions:

// URP shader (like other subsystems) that renders a transparent pressure colour overlay on a mesh.

// Samples two greyscale pressure maps and interpolates between them each frame.

// Maps normalised pressure (0-1) to a blue (low) to red (high) colour gradient.

// Place this in Assets/Shaders/ — Unity will compile it automatically.

Shader "Custom/PressureOverlay"
{
    Properties
    {
        // These are set at runtime by PressureMapVisualiser.cs — no need to assign in Inspector
        _PressureMapA ("Pressure Map A", 2D) = "black" {}
        _PressureMapB ("Pressure Map B", 2D) = "black" {}
        _Blend ("Blend", Range(0,1)) = 0
        // Controls how transparent the overlay is — 0.6 is a good starting point
        _Alpha ("Alpha", Range(0,1)) = 0.6
    }

    SubShader
    {
        // Transparent queue so the overlay renders on top of the water surface
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
                // Sample both pressure maps and interpolate between them
                float pA = SAMPLE_TEXTURE2D(_PressureMapA, sampler_PressureMapA, IN.uv).r;
                float pB = SAMPLE_TEXTURE2D(_PressureMapB, sampler_PressureMapB, IN.uv).r;
                float p  = lerp(pA, pB, _Blend);

                // Map normalised pressure to a standard CFD colour gradient:
                // Blue (0) → Cyan (0.25) → Green (0.5) → Yellow (0.75) → Red (1)
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
