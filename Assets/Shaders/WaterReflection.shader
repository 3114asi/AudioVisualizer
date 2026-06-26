Shader "AudioVisualizer/Water Reflection Additive"
{
    Properties
    {
        _ColorA ("Cyan", Color) = (0, 2, 6, 1)
        _ColorB ("Magenta", Color) = (7, 0, 8, 1)
        _Intensity ("Intensity", Float) = 1
        _Ripple ("Ripple", Float) = 0
        _Width ("Width", Range(0.02, 1.0)) = 0.22
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
            half4 _ColorA;
            half4 _ColorB;
            half _Intensity;
            half _Ripple;
            half _Width;
            Varyings vert(Attributes input)
            {
                float wobble = sin(input.uv.y * 42.0 + _Ripple * 6.283) * 0.08 + sin(input.uv.y * 91.0 - _Ripple * 9.0) * 0.025;
                input.positionOS.x += wobble * (1.0 - input.uv.y);
                Varyings output;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float center = exp(-pow(abs(input.uv.x - 0.5) / _Width, 2.0));
                float fade = pow(saturate(1.0 - input.uv.y), 1.8) * smoothstep(0.02, 0.18, input.uv.y);
                float stripes = sin(input.uv.y * 90.0 + _Ripple * 12.0) * 0.5 + 0.5;
                return lerp(_ColorB, _ColorA, input.uv.y) * _Intensity * center * fade * (0.55 + stripes * 0.45);
            }
            ENDCG
        }
    }
}
