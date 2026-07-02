Shader "AudioVisualizer/InnerNCSSpectrum"
{
    // Unity port of the NCS Spectrum GLava idea: particle/depth-style density and
    // circular spectral motion, implemented as a procedural transparent shader.
    // The effect is intentionally edge-biased: dark centre, soft energy membrane,
    // thin spectral ribbons, fine drifting dust, and subdued additive emission.
    Properties
    {
        _RingCenterX ("Center X", Float) = 0.0
        _RingCenterY ("Center Y", Float) = 0.0
        _InnerRadius ("Inner Radius", Float) = 2.92

        [Header(Zone)]
        _CoreDark ("Dark Core Radius", Range(0,1)) = 0.60
        _ShellWidth ("Active Shell Width", Range(0.05,0.7)) = 0.45
        _EdgeFeather ("Edge Feather", Range(0.001,0.5)) = 0.055
        _EdgeBias ("Edge Concentration", Range(0.5,7)) = 2.75
        _CoreAbsorb ("Core Absorption", Range(0,1)) = 0.35
        _AbsorbRadius ("Absorption Radius", Range(0,1)) = 0.68
        _FresnelPower ("Fresnel Power", Range(0.5,6)) = 2.3

        [Header(InnerEnergyMembrane)]
        _MembraneFill ("Membrane Fill", Float) = 0.060
        _MembraneIntensity ("Membrane Intensity", Float) = 0.48

        [Header(SpectrumRibbonLayer)]
        _BandIntensity ("Band Intensity", Float) = 0.40
        _BandSharpness ("Band Sharpness (higher=thinner)", Float) = 155.0
        _WaveAmp ("Wave Amplitude", Float) = 0.040
        _Ribbon1R ("Ribbon 1 Radius", Range(0,1)) = 0.954
        _Ribbon2R ("Ribbon 2 Radius", Range(0,1)) = 0.884
        _Ribbon3R ("Ribbon 3 Radius", Range(0,1)) = 0.812
        _Freq1 ("Ribbon 1 Freq", Float) = 7.0
        _Freq2 ("Ribbon 2 Freq", Float) = 11.0
        _Freq3 ("Ribbon 3 Freq", Float) = 5.0
        _Speed1 ("Ribbon 1 Speed", Float) = 0.30
        _Speed2 ("Ribbon 2 Speed", Float) = -0.23
        _Speed3 ("Ribbon 3 Speed", Float) = 0.16
        _FineThreadIntensity ("Fine Thread Intensity", Float) = 0.035

        [Header(Spectral Spokes)]
        _SpokeIntensity ("Spoke Intensity", Float) = 0.035
        _SpokeCount ("Spoke Count", Float) = 112.0
        _SpokeSpeed ("Spoke Speed", Float) = 0.045

        [Header(InnerParticleField)]
        _DotIntensity ("Dot Intensity", Float) = 1.10
        _DotDensity ("Dot Density", Float) = 165.0
        _DotThreshold ("Dot Threshold (higher=fewer)", Range(0.7,0.99)) = 0.860
        _DotCenterFill ("Centre Particle Fill", Range(0,0.25)) = 0.0
        _ParticleDrift ("Particle Drift", Float) = 0.055

        [Header(Colour)]
        _CoolColor ("Cool Color", Color) = (0.04, 0.48, 1.20, 1)
        _WarmColor ("Warm Color", Color) = (0.92, 0.08, 0.98, 1)
        _WarmAngle ("Warm Angle", Float) = 0.16
        _WarmSharpness ("Warm Sharpness", Float) = 1.65

        [Header(ProceduralNoiseFlow)]
        _EffectTime ("Effect Time", Float) = 0.0
        _NoiseAmp ("Noise Amplitude", Float) = 0.085
        _NoiseFreq ("Noise Frequency", Float) = 4.9
        _NoiseSpeed ("Noise Speed", Float) = 0.075
        _PulseSpeed ("Pulse Speed", Float) = 0.24
        _PulseAmp ("Pulse Amplitude", Range(0,0.5)) = 0.055

        [Header(Global)]
        _Exposure ("Exposure", Float) = 0.90
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct A { float4 pos : POSITION; };
            struct V { float4 hcs : SV_POSITION; float3 wp : TEXCOORD0; };

            float _RingCenterX, _RingCenterY, _InnerRadius;
            float _EdgeFeather, _CoreAbsorb, _AbsorbRadius;

            V vert(A input)
            {
                V o;
                o.hcs = UnityObjectToClipPos(input.pos);
                o.wp = mul(unity_ObjectToWorld, float4(input.pos.xyz, 1.0)).xyz;
                return o;
            }

            half4 frag(V input) : SV_Target
            {
                float2 p = input.wp.xy - float2(_RingCenterX, _RingCenterY);
                float nr = length(p) / max(_InnerRadius, 1e-5);
                float disk = 1.0 - smoothstep(1.0 - _EdgeFeather, 1.0, nr);
                float centerDark = 1.0 - smoothstep(_AbsorbRadius, 0.98, nr);
                float edgeKeep = smoothstep(0.82, 1.0, nr);
                float alpha = _CoreAbsorb * disk * saturate(centerDark) * (1.0 - edgeKeep * 0.65);
                return half4(0.0, 0.0, 0.0, alpha);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define TAU 6.28318530718

            struct A { float4 pos : POSITION; };
            struct V { float4 hcs : SV_POSITION; float3 wp : TEXCOORD0; };

            float _RingCenterX, _RingCenterY, _InnerRadius;
            float _CoreDark, _ShellWidth, _EdgeFeather, _EdgeBias, _FresnelPower;
            float _MembraneFill, _MembraneIntensity;
            float _BandIntensity, _BandSharpness, _WaveAmp;
            float _Ribbon1R, _Ribbon2R, _Ribbon3R;
            float _Freq1, _Freq2, _Freq3, _Speed1, _Speed2, _Speed3;
            float _FineThreadIntensity;
            float _SpokeIntensity, _SpokeCount, _SpokeSpeed;
            float _DotIntensity, _DotDensity, _DotThreshold, _DotCenterFill, _ParticleDrift;
            half4 _CoolColor, _WarmColor;
            float _WarmAngle, _WarmSharpness;
            float _EffectTime, _NoiseAmp, _NoiseFreq, _NoiseSpeed, _PulseSpeed, _PulseAmp;
            float _Exposure;

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
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float2 rotate2(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            float seamSafeHarmonic(float freq)
            {
                float mag = max(1.0, floor(abs(freq) + 0.5));
                return freq < 0.0 ? -mag : mag;
            }

            float ribbon(float nr, float a, float baseR, float freq, float speed, float t, float warp)
            {
                float f1 = seamSafeHarmonic(freq);
                float f2 = seamSafeHarmonic(freq * 0.47 + 1.3);
                float audioLike = 0.62 + 0.38 * sin(t * 0.74 + freq * 0.39);
                float dynR = baseR
                    + _WaveAmp * audioLike * sin(a * f1 + t * speed * TAU + warp * 4.2)
                    + _WaveAmp * 0.42 * sin(a * f2 - t * speed * 3.5 + warp * 2.1)
                    + warp * 0.42;

                return exp(-abs(nr - dynR) * _BandSharpness);
            }

            float particleField(float2 q, float density, float threshold, float seed, float t)
            {
                float2 uv = q * density + seed;
                float2 cell = floor(uv);
                float2 local = frac(uv) - 0.5;
                float gate = step(threshold, hash(cell));
                float dotFade = exp(-dot(local, local) * 96.0);
                float twinkle = 0.42 + 0.58 * sin(t * 1.15 + hash(cell + seed) * TAU);
                return gate * dotFade * saturate(twinkle);
            }

            half4 frag(V input) : SV_Target
            {
                float t = _EffectTime;
                float2 center = float2(_RingCenterX, _RingCenterY);
                float2 p = input.wp.xy - center;
                float r = length(p);
                float nr = r / max(_InnerRadius, 1e-5);
                float a = atan2(p.y, p.x);
                float2 dir = p / max(r, 1e-5);
                float2 circularUV = dir;

                float disk = 1.0 - smoothstep(1.0 - _EdgeFeather, 1.0, nr);
                float centerFade = smoothstep(_CoreDark, 1.0, nr);
                float shellStart = saturate(1.0 - _ShellWidth);
                float shell = smoothstep(shellStart, 0.98, nr) * disk;
                float edgeShell = pow(saturate(shell), _EdgeBias);
                float softShell = smoothstep(shellStart * 0.78, 0.98, nr) * disk * centerFade;

                float flowA = noise2(dir * _NoiseFreq + float2(t * _NoiseSpeed, -t * _NoiseSpeed * 0.73));
                float flowB = noise2(float2(
                    circularUV.x * 2.65 + nr * 0.85 + t * 0.045,
                    circularUV.y * 2.65 + nr * 3.60 - t * 0.052));
                float warp = ((flowA * 0.68 + flowB * 0.32) - 0.5) * 2.0 * _NoiseAmp;

                float b1 = ribbon(nr, a, _Ribbon1R, _Freq1, _Speed1, t, warp);
                float b2 = ribbon(nr, a, _Ribbon2R, _Freq2, _Speed2, t, warp);
                float b3 = ribbon(nr, a, _Ribbon3R, _Freq3, _Speed3, t, warp);
                float arcGate = 0.52 + 0.48 * noise2(float2(
                    circularUV.x * 3.15 + nr * 0.55 + t * 0.052,
                    circularUV.y * 3.15 + nr * 2.80 + warp * 2.0));
                float bands = (b1 * 0.88 + b2 * 0.52 + b3 * 0.30) * softShell * arcGate;

                float threadA = pow(0.5 + 0.5 * sin(a * 47.0 + nr * 61.0 - t * 0.58 + warp * 8.0), 9.0);
                float threadB = pow(0.5 + 0.5 * sin(a * 83.0 - nr * 44.0 + t * 0.43 - warp * 5.5), 12.0);
                float fineThreads = (threadA + threadB * 0.55) * edgeShell * _FineThreadIntensity * (0.25 + saturate(bands * 4.0));

                float spokeCount = seamSafeHarmonic(_SpokeCount);
                float spoke = pow(0.5 + 0.5 * sin(a * spokeCount + t * _SpokeSpeed * TAU + warp * 3.0), 22.0);
                float spokes = spoke * edgeShell * smoothstep(0.72, 0.965, nr) * _SpokeIntensity;

                float2 q = p / max(_InnerRadius, 1e-5);
                float driftAngle = t * _ParticleDrift;
                float2 driftNoise = float2(
                    noise2(q * 2.7 + float2(t * 0.033, 2.1)),
                    noise2(q * 2.7 + float2(-1.7, -t * 0.028))) - 0.5;
                float2 q1 = rotate2(q + driftNoise * 0.020, driftAngle);
                float2 q2 = rotate2(q * 1.17 - driftNoise * 0.014, -driftAngle * 0.72);
                float dotsFine = particleField(q1, _DotDensity, _DotThreshold, 101.0, t);
                float dotsDeep = particleField(q2, _DotDensity * 0.64, min(_DotThreshold + 0.040, 0.985), 307.0, t) * 0.62;
                float dotMask = disk * centerFade * lerp(_DotCenterFill, 1.0, edgeShell);
                float dots = (dotsFine + dotsDeep) * dotMask;

                float membraneNoise = noise2(dir * 2.0 + float2(t * 0.025, -t * 0.018));
                float membrane = softShell * _MembraneFill * (0.22 + 0.78 * membraneNoise);
                float fresnel = pow(saturate(nr), _FresnelPower) * disk * centerFade * 0.018;

                float pulse = 1.0
                    + _PulseAmp * sin(t * _PulseSpeed * TAU)
                    + _PulseAmp * 0.35 * sin(t * _PulseSpeed * 1.83 * TAU + 1.2);

                float warm = pow(saturate((cos(a - _WarmAngle) + 1.0) * 0.5), _WarmSharpness);
                warm = saturate(warm + smoothstep(0.18, 1.0, sin(a)) * 0.10);
                half3 tint = lerp(_CoolColor.rgb, _WarmColor.rgb, warm);
                half3 dustTint = lerp(half3(0.82, 0.94, 1.10), tint, 0.64);

                half3 col = 0;
                col += tint * bands * _BandIntensity;
                col += tint * (fineThreads + spokes);
                col += dustTint * dots * _DotIntensity;
                col += tint * (membrane + fresnel) * _MembraneIntensity;

                col *= pulse * _Exposure;
                col.g *= lerp(0.82, 0.52, warm);

                return half4(max(col, 0.0), 1.0);
            }
            ENDCG
        }
    }
}
