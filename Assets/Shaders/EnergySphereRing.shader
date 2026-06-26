Shader "AudioVisualizer/EnergySphereRing"
{
    Properties
    {
        _RingCenterX ("Center X", Float) = 0.0
        _RingCenterY ("Center Y", Float) = 0.0
        _RingRadius  ("Radius",  Float) = 2.0

        [Header(Shape)]
        _Thickness ("Visible Thickness", Float) = 0.020
        _CoreWidth ("HDR Core Width", Float) = 0.0065
        _GlowWidth ("Color Glow Width", Float) = 0.066
        _OuterHaloWidth ("Outer Halo Width", Float) = 0.270
        _AtmosWidth ("Atmosphere Width", Float) = 0.900

        [Header(Intensity)]
        _CoreIntensity ("Core Intensity", Float) = 15.5
        _GlowIntensity ("Glow Intensity", Float) = 3.7
        _HaloIntensity ("Halo Intensity", Float) = 0.72
        _AtmosIntensity ("Atmosphere Intensity", Float) = 0.10
        _Exposure ("Exposure", Float) = 1.0

        [Header(Color Gradient)]
        _CoolColor ("Cool Cyan/Blue", Color) = (0.0, 0.85, 1.55, 1)
        _BlueColor ("Deep Blue", Color) = (0.02, 0.05, 1.45, 1)
        _VioletColor ("Violet Bridge", Color) = (0.45, 0.06, 1.20, 1)
        _WarmColor ("Warm Magenta/Pink", Color) = (1.65, 0.10, 0.85, 1)
        _CoreColor ("White Core", Color) = (1.0, 0.94, 1.0, 1)
        _WarmAngle ("Warm Angle", Float) = 0.28
        _WarmSharpness ("Warm Sharpness", Float) = 1.65
        _AngleStrength ("Angle Strength", Range(0,1)) = 0.85

        [Header(Motion)]
        _EffectTime ("Effect Time", Float) = 0.0
        _PulseAmp ("Pulse Amp", Range(0,0.20)) = 0.026
        _PulseSpeed ("Pulse Speed", Float) = 1.15
        _NoiseAmp ("Noise Amp", Float) = 0.018
        _NoiseFreq ("Noise Freq", Float) = 5.0
        _NoiseSpeed ("Noise Speed", Float) = 0.32

        [Header(Hotspots)]
        _HotspotWidth ("Hotspot Angular Width", Float) = 0.090
        _HotspotIntensity ("Hotspot Intensity", Float) = 8.0
        _HotspotAngle0 ("Hotspot Angle 0", Float) = 0.0
        _HotspotAngle1 ("Hotspot Angle 1", Float) = 0.7
        _HotspotAngle2 ("Hotspot Angle 2", Float) = -1.55
        _HotspotAngle3 ("Hotspot Angle 3", Float) = -2.35
        _HotspotAngle4 ("Hotspot Angle 4", Float) = 1.35
        _HotspotAngle5 ("Hotspot Angle 5", Float) = 3.05
        _HotspotPower0 ("Hotspot Power 0", Float) = 0.0
        _HotspotPower1 ("Hotspot Power 1", Float) = 0.0
        _HotspotPower2 ("Hotspot Power 2", Float) = 0.0
        _HotspotPower3 ("Hotspot Power 3", Float) = 0.0
        _HotspotPower4 ("Hotspot Power 4", Float) = 0.0
        _HotspotPower5 ("Hotspot Power 5", Float) = 0.0

        [Header(Falloff)]
        _InnerDimRadius ("Inner Dim Radius", Float) = 0.72
        _AlphaFalloff ("Alpha Falloff", Float) = 5.8
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

            struct A
            {
                float4 pos : POSITION;
            };

            struct V
            {
                float4 hcs : SV_POSITION;
                float3 wp : TEXCOORD0;
            };

            float _RingCenterX, _RingCenterY, _RingRadius;
            float _Thickness, _CoreWidth, _GlowWidth, _OuterHaloWidth, _AtmosWidth;
            float _CoreIntensity, _GlowIntensity, _HaloIntensity, _AtmosIntensity, _Exposure;
            half4 _CoolColor, _BlueColor, _VioletColor, _WarmColor, _CoreColor;
            float _WarmAngle, _WarmSharpness, _AngleStrength;
            float _EffectTime, _PulseAmp, _PulseSpeed, _NoiseAmp, _NoiseFreq, _NoiseSpeed;
            float _HotspotWidth, _HotspotIntensity;
            float _HotspotAngle0, _HotspotAngle1, _HotspotAngle2, _HotspotAngle3, _HotspotAngle4, _HotspotAngle5;
            float _HotspotPower0, _HotspotPower1, _HotspotPower2, _HotspotPower3, _HotspotPower4, _HotspotPower5;
            float _InnerDimRadius, _AlphaFalloff;

            V vert(A input)
            {
                V o;
                o.hcs = UnityObjectToClipPos(input.pos);
                o.wp = mul(unity_ObjectToWorld, float4(input.pos.xyz, 1.0)).xyz;
                return o;
            }

            float hash(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453123);
            }

            float noise1(float p)
            {
                float i = floor(p);
                float f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(hash(i), hash(i + 1.0), f);
            }

            float angularDistance(float a, float b)
            {
                return abs(atan2(sin(a - b), cos(a - b)));
            }

            float hotspot(float angle, float hAngle, float power, float width)
            {
                float d = angularDistance(angle, hAngle);
                return power * exp(-(d * d) / max(width * width, 1e-5));
            }

            float ringLayer(float dist, float width)
            {
                return exp(-dist / max(width, 1e-5));
            }

            half4 frag(V input) : SV_Target
            {
                float t = _EffectTime + _Time.y;
                float2 center = float2(_RingCenterX, _RingCenterY);
                float2 toPixel = input.wp.xy - center;
                float distFromCenter = length(toPixel);
                float angle = atan2(toPixel.y, toPixel.x);

                float lowWave = sin(angle * 3.0 - t * 0.55) * 0.50
                              + sin(angle * 5.0 + t * 0.37) * 0.30
                              + sin(angle * 9.0 - t * 0.73) * 0.12;
                float steppedNoise = noise1(angle * _NoiseFreq + t * _NoiseSpeed)
                                   + noise1(angle * (_NoiseFreq * 1.9) - t * (_NoiseSpeed * 0.65)) * 0.45;
                float irregular = (lowWave * 0.45 + steppedNoise - 0.70);

                float pulse = 1.0
                    + _PulseAmp * sin(t * _PulseSpeed)
                    + _PulseAmp * 0.45 * sin(t * _PulseSpeed * 2.13 + 1.7);

                float radius = _RingRadius * pulse + irregular * _NoiseAmp;
                float ringDist = abs(distFromCenter - radius);

                float rightWarm = pow(saturate((cos(angle - _WarmAngle) + 1.0) * 0.5), max(_WarmSharpness, 0.05));
                float topWarm = pow(saturate((sin(angle) + 1.0) * 0.5), 2.2) * 0.40;
                float warm = saturate(max(rightWarm, topWarm) * _AngleStrength);

                float leftCool = pow(saturate((cos(angle - 3.14159) + 1.0) * 0.5), 1.15);
                float bottomCool = pow(saturate((-sin(angle) + 1.0) * 0.5), 1.85) * 0.85;
                float cool = saturate(max(leftCool, bottomCool));

                half3 coolCol = lerp(_BlueColor.rgb, _CoolColor.rgb, bottomCool * 0.65 + leftCool * 0.35);
                half3 chroma = lerp(coolCol, _WarmColor.rgb, warm);
                chroma = lerp(chroma, _VioletColor.rgb, saturate((warm * cool) * 0.55));

                float h = 0.0;
                h += hotspot(angle, _HotspotAngle0, _HotspotPower0, _HotspotWidth);
                h += hotspot(angle, _HotspotAngle1, _HotspotPower1, _HotspotWidth);
                h += hotspot(angle, _HotspotAngle2, _HotspotPower2, _HotspotWidth);
                h += hotspot(angle, _HotspotAngle3, _HotspotPower3, _HotspotWidth);
                h += hotspot(angle, _HotspotAngle4, _HotspotPower4, _HotspotWidth);
                h += hotspot(angle, _HotspotAngle5, _HotspotPower5, _HotspotWidth);

                float filament = smoothstep(_Thickness * 2.2, _Thickness * 0.35, ringDist);
                float core = ringLayer(ringDist, _CoreWidth);
                float glow = ringLayer(ringDist, _GlowWidth);
                float halo = ringLayer(ringDist, _OuterHaloWidth);
                float atmos = ringLayer(ringDist, _AtmosWidth);

                float innerMask = smoothstep(_RingRadius * _InnerDimRadius, _RingRadius, distFromCenter);
                float outsideMask = 1.0 - smoothstep(_RingRadius + _AtmosWidth * _AlphaFalloff, _RingRadius + _AtmosWidth * (_AlphaFalloff + 0.7), distFromCenter);

                half3 coreTint = normalize(_CoreColor.rgb + chroma * 0.35 + 0.001);
                half3 col = 0;
                col += coreTint * (_CoreIntensity * core * (1.0 + h * 1.25));
                col += chroma * (_GlowIntensity * glow * (0.85 + filament * 0.45 + h * 0.75));
                col += chroma * (_HaloIntensity * halo * (0.65 + cool * 0.35 + warm * 0.30));
                col += lerp(_BlueColor.rgb, _VioletColor.rgb, warm * 0.6) * (_AtmosIntensity * atmos);

                float rimRipple = 0.5 + 0.5 * sin(angle * 34.0 + t * 4.3 + noise1(angle * 11.0) * 6.0);
                col += chroma * filament * rimRipple * 1.2;
                col += _CoreColor.rgb * h * _HotspotIntensity * ringLayer(ringDist, _CoreWidth * 3.0);

                float sparkCell = floor((angle + 3.14159) * 36.0);
                float sparkClock = floor(t * 7.0);
                float sparkRand = hash(sparkCell + sparkClock * 19.37);
                float sparkGate = step(0.885, sparkRand);
                float sparkAge = frac(t * 2.9 + hash(sparkCell + 5.1));
                float sparkEnv = sin(sparkAge * 3.14159) * step(sparkAge, 0.76);
                float sparkBand = exp(-ringDist / 0.018) * (0.65 + 0.35 * h);
                col += chroma * sparkGate * sparkEnv * sparkBand * 2.8;

                col *= innerMask * outsideMask * _Exposure;
                col.g *= lerp(0.72, 0.18, warm);

                return half4(col, 1.0);
            }
            ENDCG
        }
    }
}
