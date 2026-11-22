// Fresnel bubble used for shields, auras and the invisibility ghost.
Shader "BalloonToHeaven/Shield"
{
    Properties
    {
        _Color ("Color", Color) = (0.3,0.9,1,1)
        _Power ("Fresnel Power", Range(0.5,6)) = 2.2
        _Alpha ("Alpha", Range(0,1)) = 0.8
        _Pulse ("Pulse Speed", Float) = 3
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            half _Power;
            half _Alpha;
            half _Pulse;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.normal = UnityObjectToWorldNormal(v.normal); return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 view = normalize(UNITY_MATRIX_V[2].xyz);
                half f = pow(1.0 - saturate(abs(dot(normalize(i.normal), view))), _Power);
                half pulse = 0.8 + 0.2 * sin(_Time.y * _Pulse);
                return fixed4(_Color.rgb, saturate((f + 0.08) * _Alpha * pulse));
            }
            ENDCG
        }
    }
    Fallback Off
}
