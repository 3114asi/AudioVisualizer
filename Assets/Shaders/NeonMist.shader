Shader "AudioVisualizer/Neon Mist Additive"
{
    Properties
    {
        _Color ("Emission", Color) = (1, 0, 3, 0.35)
        _Intensity ("Intensity", Float) = 1
        _FlowOffset ("Flow Offset", Float) = 0
        _Softness ("Softness", Range(0.1, 6)) = 2
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
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            half4 _Color;
            half _Intensity;
            half _FlowOffset;
            half _Softness;
            Varyings vert(Attributes input)
            {
                Varyings output;
                input.positionOS.xy += float2(sin((input.uv.y + _FlowOffset) * 8.0), cos((input.uv.x - _FlowOffset) * 7.0)) * 0.04;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 c = input.uv - 0.5;
                float radial = pow(saturate(1.0 - dot(c, c) * 4.0), _Softness);
                float noise = sin((input.uv.x + _FlowOffset) * 20.0) * sin((input.uv.y - _FlowOffset * 0.7) * 17.0) * 0.5 + 0.5;
                return _Color * _Intensity * radial * (0.35 + noise * 0.65);
            }
            ENDCG
        }
    }
}
