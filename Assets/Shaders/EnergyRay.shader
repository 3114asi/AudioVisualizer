Shader "AudioVisualizer/EnergyRay"
{
    Properties
    {
        _Color ("Color", Color) = (0.3, 0.5, 1.0, 1.0)
        _Intensity ("Intensity", Float) = 3.0
        _Softness ("Softness", Float) = 3.0
        _Length ("Length", Float) = 1.0
        _Width ("Width", Float) = 0.03
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+2" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _Color;
            float _Intensity, _Softness, _Length, _Width;

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 hcs : SV_POSITION; float2 uv : TEXCOORD0; };

            V vert(A input)
            {
                V o;
                o.hcs = UnityObjectToClipPos(input.pos);
                o.uv = input.uv;
                return o;
            }

            half4 frag(V input) : SV_Target
            {
                float2 uv = input.uv;
                float centerDist = abs(uv.x - 0.5) * 2.0;
                float needle = exp(-centerDist * centerDist * _Softness * 5.5);
                float softWing = exp(-centerDist * centerDist * _Softness * 0.95) * 0.25;

                float rootFade = smoothstep(0.00, 0.10, uv.y);
                float tipFade = pow(saturate(1.0 - uv.y), 1.45);
                float broken = 0.72 + 0.28 * sin(uv.y * 28.0 + _Time.y * 7.0);

                float rootFlare = exp(-uv.y * 14.0) * exp(-centerDist * centerDist * 3.5) * 0.75;
                float base = (needle + softWing) * rootFade * tipFade * broken + rootFlare;
                float3 col = _Color.rgb * _Intensity * base;

                return half4(col, 0);
            }
            ENDCG
        }
    }
}
