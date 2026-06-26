Shader "AudioVisualizer/InnerEnergySphere"
{
    Properties
    {
        _RingCenterX ("Center X", Float) = 0.0
        _RingCenterY ("Center Y", Float) = 0.0
        _InnerRadius ("Inner Radius", Float) = 1.93

        [Header(Base)]
        _BaseColor ("Base Color", Color) = (0.02, 0.02, 0.10, 1)
        _BaseIntensity ("Base Intensity", Float) = 0.10
        _EdgeFade ("Edge Fade", Float) = 0.18

        [Header(Energy Layers)]
        _Layer1Color ("Layer 1 Color", Color) = (0.0, 0.55, 1.40, 1)
        _Layer1Speed ("Layer 1 Speed", Float) = 0.28
        _Layer1Freq  ("Layer 1 Freq", Float) = 3.0
        _Layer1Intensity ("Layer 1 Intensity", Float) = 0.18

        _Layer2Color ("Layer 2 Color", Color) = (0.65, 0.04, 1.20, 1)
        _Layer2Speed ("Layer 2 Speed", Float) = -0.22
        _Layer2Freq  ("Layer 2 Freq", Float) = 5.5
        _Layer2Intensity ("Layer 2 Intensity", Float) = 0.12

        _Layer3Color ("Layer 3 Color", Color) = (1.10, 0.08, 0.75, 1)
        _Layer3Speed ("Layer 3 Speed", Float) = 0.17
        _Layer3Freq  ("Layer 3 Freq", Float) = 8.0
        _Layer3Intensity ("Layer 3 Intensity", Float) = 0.08

        [Header(Grid)]
        _GridColor ("Grid Color", Color) = (0.05, 0.58, 1.25, 1)
        _GridFreq ("Grid Frequency", Float) = 16.0
        _GridIntensity ("Grid Intensity", Float) = 0.075
        _DotIntensity ("Dot Intensity", Float) = 0.78

        [Header(Motion)]
        _EffectTime ("Effect Time", Float) = 0.0
        _NoiseAmp ("Noise Amplitude", Float) = 0.14
        _NoiseFreq ("Noise Frequency", Float) = 5.5
        _NoiseSpeed ("Noise Speed", Float) = 0.24
        _PulseSpeed ("Pulse Speed", Float) = 0.8
        _PulseAmp ("Pulse Amplitude", Range(0, 0.5)) = 0.12

        [Header(Global)]
        _Exposure ("Exposure", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct A
            {
                float4 pos : POSITION;
            };

            struct V
            {
                float4 hcs : SV_POSITION;
                float3 wp : TEXCOORD0;
            };

            float _RingCenterX, _RingCenterY, _InnerRadius, _EdgeFade;
            half4 _BaseColor; float _BaseIntensity;
            half4 _Layer1Color; float _Layer1Speed, _Layer1Freq, _Layer1Intensity;
            half4 _Layer2Color; float _Layer2Speed, _Layer2Freq, _Layer2Intensity;
            half4 _Layer3Color; float _Layer3Speed, _Layer3Freq, _Layer3Intensity;
            half4 _GridColor; float _GridFreq, _GridIntensity, _DotIntensity;
            float _EffectTime, _NoiseAmp, _NoiseFreq, _NoiseSpeed, _PulseSpeed, _PulseAmp, _Exposure;

            V vert(A input)
            {
                V o;
                o.hcs = UnityObjectToClipPos(input.pos);
                o.wp = mul(unity_ObjectToWorld, float4(input.pos.xyz, 1.0)).xyz;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float lineMask(float v, float width)
            {
                float f = abs(frac(v) - 0.5);
                return smoothstep(width, 0.0, f);
            }

            half4 frag(V input) : SV_Target
            {
                float t = _EffectTime + _Time.y;
                float2 center = float2(_RingCenterX, _RingCenterY);
                float2 p = input.wp.xy - center;
                float r = length(p);
                float nr = r / max(_InnerRadius, 1e-5);
                float a = atan2(p.y, p.x);

                float disk = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, nr);
                float edge = smoothstep(0.45, 1.0, nr);
                float centerDim = smoothstep(0.12, 0.42, nr);

                float n = noise2(p * _NoiseFreq + float2(t * _NoiseSpeed, -t * _NoiseSpeed * 0.7));
                float warp = (n - 0.5) * _NoiseAmp;

                float pulse = 1.0 + _PulseAmp * sin(t * _PulseSpeed)
                            + _PulseAmp * 0.45 * sin(t * _PulseSpeed * 1.9 + 1.1);

                float wave1 = sin(a * _Layer1Freq + nr * 13.0 - t * _Layer1Speed * 6.283 + warp * 5.0);
                float wave2 = sin(a * _Layer2Freq - nr * 18.0 - t * _Layer2Speed * 6.283 + warp * 4.0);
                float wave3 = sin(a * _Layer3Freq + nr * 23.0 - t * _Layer3Speed * 6.283 + warp * 6.0);

                float membrane1 = smoothstep(0.84, 1.0, wave1 * 0.5 + 0.5) * edge;
                float membrane2 = smoothstep(0.88, 1.0, wave2 * 0.5 + 0.5) * smoothstep(0.20, 0.92, nr);
                float membrane3 = smoothstep(0.91, 1.0, wave3 * 0.5 + 0.5) * smoothstep(0.55, 0.98, nr);

                float polarGrid = lineMask(a / 6.2831853 * _GridFreq + t * 0.015, 0.018)
                                + lineMask(nr * _GridFreq * 0.80 - t * 0.030, 0.022);
                polarGrid *= smoothstep(0.28, 0.95, nr) * (0.20 + 0.45 * n);

                float2 cell = floor((p / _InnerRadius) * 42.0 + 100.0);
                float2 local = frac((p / _InnerRadius) * 42.0 + 100.0) - 0.5;
                float dotGate = step(0.895, hash(cell));
                float dotFade = exp(-dot(local, local) * 185.0);
                float twinkle = 0.45 + 0.55 * sin(t * 3.7 + hash(cell + 19.0) * 6.283);
                float dots = dotGate * dotFade * twinkle * smoothstep(0.18, 0.92, nr) * (1.0 - smoothstep(0.97, 1.0, nr));

                float warm = saturate(max(pow(saturate((cos(a - 0.25) + 1.0) * 0.5), 1.8),
                                          pow(saturate((sin(a) + 1.0) * 0.5), 2.4) * 0.32));
                half3 coolCol = lerp(half3(0.0, 0.38, 1.1), half3(0.0, 0.88, 1.45), saturate((-sin(a) + 1.0) * 0.45));
                half3 warmCol = half3(1.15, 0.05, 0.78);

                half3 col = _BaseColor.rgb * _BaseIntensity;
                col += lerp(coolCol, warmCol, warm) * membrane1 * _Layer1Intensity;
                col += _Layer2Color.rgb * membrane2 * _Layer2Intensity;
                col += _Layer3Color.rgb * membrane3 * _Layer3Intensity;
                col += _GridColor.rgb * polarGrid * _GridIntensity;
                col += lerp(coolCol, warmCol, warm) * dots * _DotIntensity;

                float edgeGlow = pow(saturate(nr), 3.5) * (0.25 + 0.75 * (membrane1 + membrane2));
                col += lerp(coolCol, warmCol, warm) * edgeGlow * 0.08;

                col *= disk * centerDim * pulse * _Exposure;
                col.g *= lerp(0.85, 0.28, warm);

                return half4(col, 1.0);
            }
            ENDCG
        }
    }
}
