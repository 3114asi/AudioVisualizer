Shader "AudioVisualizer/Neon Portal Ring Additive"
{
    Properties
    {
        _ColorA ("Cyan Emission", Color) = (0.0, 3.0, 8.0, 1)
        _ColorB ("Magenta Emission", Color) = (9.0, 0.0, 8.0, 1)
        _Intensity ("Intensity", Float) = 3
        _Pulse ("Pulse", Float) = 1
        _FlowOffset ("Flow Offset", Float) = 0
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.25
        _SegmentContrast ("Segment Contrast", Range(0, 6)) = 2.5
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
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float3 posOS : TEXCOORD1; };

            half4 _ColorA;
            half4 _ColorB;
            half _Intensity;
            half _Pulse;
            half _FlowOffset;
            half _NoiseStrength;
            half _SegmentContrast;

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float angleNoise = sin((input.uv.x + _FlowOffset) * 54.0) * sin((input.uv.x - _FlowOffset * 0.7) * 31.0);
                float edge = abs(input.uv.y - 0.5) * 2.0;
                input.positionOS.xy += normalize(input.positionOS.xy + 0.0001) * angleNoise * edge * _NoiseStrength * 0.09;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                output.posOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float angle = input.uv.x;
                float verticalBias = saturate(1.0 - input.uv.y);
                half4 color = lerp(_ColorA, _ColorB, smoothstep(0.10, 0.92, angle) * 0.9 + verticalBias * 0.22);
                float core = 1.0 - abs(input.uv.y - 0.5) * 2.0;
                float fresnel = pow(saturate(1.0 - core), 2.5) + pow(saturate(core), 7.0) * 2.0;
                float segments = pow(saturate(sin((angle + _FlowOffset) * 37.699) * 0.5 + 0.5), _SegmentContrast);
                float sparks = step(0.955, hash(float2(floor((angle + _FlowOffset) * 170.0), floor(_Time.y * 18.0))));
                float alpha = saturate(0.25 + fresnel + segments * 1.2 + sparks * 3.0);
                return color * (_Intensity * _Pulse * alpha);
            }
            ENDCG
        }
    }
}
