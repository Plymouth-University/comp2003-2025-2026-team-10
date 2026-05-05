// URP shader that renders a transparent vorticity colour overlay on a mesh.
// Samples two greyscale vorticity maps and interpolates between them each frame.
// Maps normalised vorticity magnitude (0-1) to a black (low) to white (high)
// colour gradient — high vorticity areas appear bright, calm areas appear dark.
// Place this file in Assets/Shaders/ — Unity will compile it automatically.
Shader "Custom/VorticityOverlay"
{
    Properties
    {
        // These are set at runtime by VorticityVisualiser.cs — no need to assign in Inspector
        _VorticityMapA ("Vorticity Map A", 2D) = "black" {}
        _VorticityMapB ("Vorticity Map B", 2D) = "black" {}
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

            TEXTURE2D(_VorticityMapA);
            TEXTURE2D(_VorticityMapB);
            SAMPLER(sampler_VorticityMapA);
            SAMPLER(sampler_VorticityMapB);

            CBUFFER_START(UnityPerMaterial)
                float4 _VorticityMapA_ST;
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
                // Sample both vorticity maps and interpolate between them
                float vA = SAMPLE_TEXTURE2D(_VorticityMapA, sampler_VorticityMapA, IN.uv).r;
                float vB = SAMPLE_TEXTURE2D(_VorticityMapB, sampler_VorticityMapB, IN.uv).r;
                float v  = lerp(vA, vB, _Blend);

                // Map normalised vorticity magnitude to a purple (low) to white (high) gradient
                // Low vorticity = calm fluid (dark purple), high vorticity = rotation (bright white)
                float3 col;
                if (v < 0.5)
                    col = lerp(float3(0.1, 0.0, 0.2), float3(0.6, 0.0, 0.8), v * 2);
                else
                    col = lerp(float3(0.6, 0.0, 0.8), float3(1.0, 1.0, 1.0), (v - 0.5) * 2);

                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }
}
