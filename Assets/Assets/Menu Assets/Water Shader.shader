Shader "UI/WarpingSplash"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.1
        _TimeSpeed ("Warp Speed", Range(0, 10)) = 2
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _NoiseStrength;
            float _TimeSpeed;

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                float timeWarp = _Time.y * _TimeSpeed;
                
                float distortionX = sin(v.vertex.y * 8.0 + timeWarp) * _NoiseStrength;
                float distortionY = cos(v.vertex.x * 8.0 + timeWarp) * _NoiseStrength;

                v.vertex.x += distortionX;
                v.vertex.y += distortionY;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
