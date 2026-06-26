Shader "AudioVisualizer/Top Dark Gradient"
{
    Properties
    {
        _Color ("Color", Color) = (0.005, 0.003, 0.015, 1)
        _GradientStart ("Gradient Start (UV.y)", Range(0, 1)) = 0.35
        _GradientPower ("Gradient Power", Range(0.5, 5)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+80" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _GradientStart;
            float _GradientPower;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // Soft vertical gradient: opaque at top, transparent at bottom
                float t = saturate((input.uv.y - (1.0 - _GradientStart)) * _GradientPower);
                return fixed4(_Color.rgb, _Color.a * t);
            }
            ENDCG
        }
    }
}
