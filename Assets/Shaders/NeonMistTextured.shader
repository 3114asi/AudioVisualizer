Shader "AudioVisualizer/Neon Mist Textured Additive"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {}
        _Color ("Emission", Color) = (0.3, 0, 1, 0.4)
        _Intensity ("Intensity", Float) = 0.26
        _Softness ("Edge Softness", Range(0.1, 3)) = 1.0
        _DistortStrength ("UV Distortion", Range(0, 0.1)) = 0.03
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            half _Intensity;
            half _Softness;
            half _DistortStrength;

            // Simple hash for UV distortion
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Subtle vertex displacement for organic edges
                float displace = (hash(input.uv * 7.3 + _Time.x * 0.05) - 0.5) * 0.04;
                input.positionOS.xy += displace;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Slight UV distortion for wispy edges
                float2 distortedUV = input.uv;
                distortedUV.x += (hash(input.uv * 5.0 + 0.1) - 0.5) * _DistortStrength;
                distortedUV.y += (hash(input.uv * 5.0 + 0.6) - 0.5) * _DistortStrength;

                // Sample cloud texture
                float cloudDensity = tex2D(_MainTex, distortedUV).a;

                // Soft threshold to create distinct cloud shapes
                float shape = pow(saturate(cloudDensity), _Softness);

                return _Color * _Intensity * shape;
            }
            ENDCG
        }
    }
}
