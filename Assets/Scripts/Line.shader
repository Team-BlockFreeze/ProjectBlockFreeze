Shader "Custom/TransparentLine"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base (RGB)", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Lighting Off
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            SetTexture[_MainTex]
            {
                combine primary
            }
        }
    }
    FallBack "Diffuse"
}
