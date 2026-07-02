Shader "AudioVisualizer/Reference Backdrop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        ZWrite On
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return tex2D(_MainTex, input.uv) * _Color;
            }
            ENDCG
        }
    }
}
