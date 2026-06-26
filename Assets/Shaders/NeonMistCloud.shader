Shader "AudioVisualizer/Neon Mist Cloud Additive"
{
    Properties
    {
        _Color ("Emission", Color) = (1, 0, 3, 0.35)
        _Intensity ("Intensity", Float) = 1
        _FlowOffset ("Flow Offset", Float) = 0
        _Softness ("Softness", Range(0.1, 6)) = 2
        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPos : TEXCOORD1; };

            half4 _Color;
            half _Intensity;
            half _FlowOffset;
            half _Softness;
            half _NoiseScale;
            half _NoiseStrength;

            // Simple 2D hash-based value noise
            float hash2D(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // 2D value noise with smooth interpolation
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                return lerp(
                    lerp(hash2D(i), hash2D(i + float2(1, 0)), f.x),
                    lerp(hash2D(i + float2(0, 1)), hash2D(i + float2(1, 1)), f.x),
                    f.y
                );
            }

            // 2-octave fbm for more organic look
            float fbm2D(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.6;
                float frequency = 1.0;
                for (int i = 0; i < 2; i++)
                {
                    value += amplitude * noise2D(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Gentle vertex displacement for organic edges
                float2 noiseInput = input.uv * 5.0 + _FlowOffset;
                float displace = (noise2D(noiseInput) - 0.5) * 0.06;
                input.positionOS.xy += float2(displace, displace * 0.7);
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                output.worldPos = mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1)).xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 c = input.uv - 0.5;

                // Base radial gradient
                float radial = pow(saturate(1.0 - dot(c, c) * 4.0), _Softness);

                // Organic noise from world-space position (non-repeating per quad)
                float2 noiseCoord = input.worldPos.xy * _NoiseScale + _FlowOffset * 0.3;
                float noise = fbm2D(noiseCoord);

                // Mix: radial shape modulated by organic noise
                float shape = radial * lerp(0.25, 1.0, noise * _NoiseStrength);

                return _Color * _Intensity * shape;
            }
            ENDCG
        }
    }
}
