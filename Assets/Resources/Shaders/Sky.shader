// Vertical gradient for the sky backdrop, with an optional soft horizon glow.
Shader "BalloonToHeaven/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0.3,0.6,1,1)
        _Bottom ("Bottom", Color) = (0.8,0.9,1,1)
        _Glow ("Glow", Color) = (1,1,1,0)
        _GlowHeight ("Glow Height", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top;
            fixed4 _Bottom;
            fixed4 _Glow;
            half _GlowHeight;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                half t = smoothstep(0, 1, i.uv.y);
                fixed3 col = lerp(_Bottom.rgb, _Top.rgb, t);
                half g = exp(-abs(i.uv.y - _GlowHeight) * 6.0) * _Glow.a;
                col += _Glow.rgb * g;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
