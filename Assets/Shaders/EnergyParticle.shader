Shader "AudioVisualizer/EnergyParticle"
{
    Properties
    {
        _EmissionGain ("Emission Gain", Float) = 1.0
        _Softness ("Softness", Float) = 3.8
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+3" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _EmissionGain, _Softness;

            struct A
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct V
            {
                float4 hcs : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            V vert(A input)
            {
                V o;
                o.hcs = UnityObjectToClipPos(input.vertex);
                o.color = input.color;
                o.uv = input.uv;
                return o;
            }

            half4 frag(V input) : SV_Target
            {
                float2 p = input.uv - 0.5;
                float r2 = dot(p, p) * 4.0;
                float core = exp(-r2 * _Softness);
                float halo = exp(-r2 * (_Softness * 0.32)) * 0.25;
                float a = input.color.a * (core + halo);
                return half4(input.color.rgb * a * _EmissionGain, 0.0);
            }
            ENDCG
        }
    }
}
