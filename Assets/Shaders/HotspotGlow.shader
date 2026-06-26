Shader "AudioVisualizer/HotspotGlow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 1, 1)
        _Intensity ("Intensity", Float) = 4.0
        _Falloff ("Falloff", Float) = 2.5
        _Size ("Size", Float) = 1.0
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
            float _Intensity, _Falloff, _Size;

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
                float2 c = input.uv - 0.5;
                float dist = length(c) * 2.0;
                float glow = exp(-dist * dist * _Falloff);
                float horizontal = exp(-abs(c.x) * 18.0) * exp(-abs(c.y) * 3.5);
                float vertical = exp(-abs(c.y) * 18.0) * exp(-abs(c.x) * 3.5);
                float spark = glow + (horizontal + vertical) * 0.18;
                float3 col = _Color.rgb * _Intensity * spark;
                return half4(col, 0);
            }
            ENDCG
        }
    }
}
