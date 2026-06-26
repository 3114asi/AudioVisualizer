Shader "AudioVisualizer/Neon Ring Multi-Layer"
{
    Properties
    {
        // ── Ring geometry (world-space) ──
        _RingCenterX ("Ring Center X", Float) = 0.12
        _RingCenterY ("Ring Center Y", Float) = -0.43
        _RingRadius  ("Ring Radius",  Float) = 3.05

        // ════════════════════════════════════════════════════════════════
        //  Seven INDEPENDENT additive light layers.
        //  Each contributes:  Color * Intensity * exp(-dist / Falloff)
        //  Near the ring the narrow bright White Core dominates (→ blow-out);
        //  moving outward each successively wider colored layer takes over:
        //  White → Hot Pink → Magenta → Purple → Electric Blue → Atmosphere.
        // ════════════════════════════════════════════════════════════════

        // L1 — Ultra White HDR Core (narrowest, brightest, overexposed)
        // The bright line takes its hue from the angular temperature: cool side
        // = blue-white, warm side = pink-white. At the peak it blows out to white.
        _CoreColor     ("Core Warm Color", Color) = (1.0, 0.78, 0.90, 1.0)
        _CoreCoolColor ("Core Cool Color", Color) = (0.55, 0.78, 1.0, 1.0)
        _CoreIntensity ("Core Intensity", Float) = 45.0
        _CoreFalloff   ("Core Falloff",   Float) = 0.006

        // L2 — Hot Pink Core
        _PinkColor     ("Pink Color", Color) = (1.0, 0.22, 0.62, 1.0)
        _PinkIntensity ("Pink Intensity", Float) = 13.0
        _PinkFalloff   ("Pink Falloff",   Float) = 0.026

        // L3 — Main Magenta Ring
        _MagentaColor     ("Magenta Color", Color) = (1.0, 0.04, 0.85, 1.0)
        _MagentaIntensity ("Magenta Intensity", Float) = 4.2
        _MagentaFalloff   ("Magenta Falloff",   Float) = 0.072

        // L4 — Purple / Violet Glow
        _PurpleColor     ("Purple Color", Color) = (0.50, 0.05, 1.0, 1.0)
        _PurpleIntensity ("Purple Intensity", Float) = 1.7
        _PurpleFalloff   ("Purple Falloff",   Float) = 0.18

        // L5 — Electric Blue Halo (wide cool band, the main missing layer)
        _BlueColor     ("Electric Blue Color", Color) = (0.10, 0.42, 1.0, 1.0)
        _BlueIntensity ("Blue Intensity", Float) = 0.95
        _BlueFalloff   ("Blue Falloff",   Float) = 0.55

        // L6 — HDR Bloom feeder layer (broad, feeds post bloom)
        _BloomColor     ("Bloom Color", Color) = (0.22, 0.30, 1.0, 1.0)
        _BloomIntensity ("Bloom Layer Intensity", Float) = 0.50
        _BloomFalloff   ("Bloom Layer Falloff",   Float) = 1.05

        // L7 — Large Atmospheric Glow (huge radius, barely visible volume)
        _AtmosColor     ("Atmos Color", Color) = (0.26, 0.16, 0.92, 1.0)
        _AtmosIntensity ("Atmos Intensity", Float) = 0.12
        _AtmosFalloff   ("Atmos Falloff",   Float) = 2.4

        // ── Global exposure (quick master brightness) ──
        _Exposure ("Exposure", Float) = 1.0

        // ── Angular temperature gradient ──
        // Warm (pink/magenta) direction vs cool (electric blue) direction.
        // _WarmAngle = direction (radians) of the warm pole.
        _WarmAngle    ("Warm Direction (rad)", Float) = -0.785   // bottom-right
        _AngleStrength("Angle Temp Strength", Range(0,1)) = 0.55
        // Concentrates the warm (pink) sector near the warm pole; >1 = blue dominates
        _WarmSharpness("Warm Sector Sharpness", Float) = 2.0
        // Final channel enforcement of temperature
        _CoolRedCut   ("Cool Side Red Cut",  Range(0,1)) = 0.22
        _WarmBlueCut  ("Warm Side Blue Cut", Range(0,1)) = 0.55

        // ── Energetic instability (smooth, not noise) ──
        // Brightens right + top sectors slightly for a living energy feel.
        _Instability ("Instability Amount", Range(0,1)) = 0.30
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
            struct Varyings   { float4 positionHCS : SV_POSITION; float3 worldPos : TEXCOORD0; };

            float _RingCenterX, _RingCenterY, _RingRadius;

            half4 _CoreColor, _CoreCoolColor;    float _CoreIntensity,    _CoreFalloff;
            half4 _PinkColor;    float _PinkIntensity,    _PinkFalloff;
            half4 _MagentaColor; float _MagentaIntensity, _MagentaFalloff;
            half4 _PurpleColor;  float _PurpleIntensity,  _PurpleFalloff;
            half4 _BlueColor;    float _BlueIntensity,    _BlueFalloff;
            half4 _BloomColor;   float _BloomIntensity,   _BloomFalloff;
            half4 _AtmosColor;   float _AtmosIntensity,   _AtmosFalloff;

            float _Exposure;
            float _WarmAngle, _AngleStrength, _WarmSharpness, _Instability;
            float _CoolRedCut, _WarmBlueCut;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = UnityObjectToClipPos(input.positionOS);
                o.worldPos    = mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1.0)).xyz;
                return o;
            }

            // One exponential-falloff layer.
            half3 layer(half4 col, float intensity, float falloff, float dist)
            {
                return col.rgb * (intensity * exp(-dist / max(falloff, 1e-6)));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 ringCenter   = float2(_RingCenterX, _RingCenterY);
                float2 d2           = input.worldPos.xy - ringCenter;
                float  distToCenter = length(d2);
                float  ringDist     = abs(distToCenter - _RingRadius);

                // ── Angular temperature: warm (pink) pole vs cool (blue) pole ──
                // The ring LINE itself must change hue around the circle: blue on
                // the cool side, pink/magenta on the warm side. So the warm layers
                // (pink, magenta) are weighted DOWN on the cool side and the blue
                // layer is weighted DOWN on the warm side — not just a final tint.
                float angle  = atan2(d2.y, d2.x);
                float warmth = (cos(angle - _WarmAngle) + 1.0) * 0.5;   // 1 warm, 0 cool
                warmth = pow(warmth, max(_WarmSharpness, 0.01));        // concentrate pink sector
                float s      = _AngleStrength;

                float pinkW    = lerp(1.0 - 0.92 * s, 1.0,            warmth); // pink almost gone on cool side
                float magentaW = lerp(1.0 - 0.80 * s, 1.0,            warmth); // magenta fades on cool side
                float blueW    = lerp(1.0,            1.0 - 0.45 * s, warmth); // blue fades on warm side

                // The bright line hue rotates: blue-white (cool) ↔ pink-white (warm)
                half4 coreCol = lerp(_CoreCoolColor, _CoreColor, warmth);

                // ── Sum of seven independent additive light layers ──
                half3 col = half3(0,0,0);
                col += layer(coreCol,       _CoreIntensity,              _CoreFalloff,    ringDist);
                col += layer(_PinkColor,    _PinkIntensity    * pinkW,   _PinkFalloff,    ringDist);
                col += layer(_MagentaColor, _MagentaIntensity * magentaW,_MagentaFalloff, ringDist);
                col += layer(_PurpleColor,  _PurpleIntensity,            _PurpleFalloff,  ringDist);
                col += layer(_BlueColor,    _BlueIntensity    * blueW,   _BlueFalloff,    ringDist);
                col += layer(_BloomColor,   _BloomIntensity,             _BloomFalloff,   ringDist);
                col += layer(_AtmosColor,   _AtmosIntensity,             _AtmosFalloff,   ringDist);

                // ── Energetic instability: smooth boost on right + top sectors ──
                float instab = 1.0
                    + _Instability * (0.55 * pow(saturate(cos(angle)),       2.0)    // right
                                    + 0.40 * pow(saturate(sin(angle)),       2.0)    // top
                                    + 0.18 * pow(saturate(sin(2.0 * angle)), 4.0));  // local nodes
                col *= instab;

                // ── Final angular temperature enforcement ──
                // Additive layers pile red into every channel and clamp toward
                // pink/white. To make the cool side genuinely BLUE, hard-cut the
                // red channel where it is cool, and trim blue where it is warm.
                col.r *= lerp(_CoolRedCut, 1.0,         warmth);   // cool = low red → blue
                col.g *= lerp(0.30,        0.55,        warmth);   // deep, near-zero green (ref is pure blue/magenta)
                col.b *= lerp(1.0,         _WarmBlueCut, warmth);  // warm = less blue → pink

                col *= _Exposure;

                // Soft clip of the quad's far edge so the atmospheric glow fades
                // smoothly to nothing instead of cutting at the quad boundary.
                col *= 1.0 - smoothstep(8.0, 9.5, distToCenter);

                return half4(col, 1.0);
            }
            ENDCG
        }
    }
}
