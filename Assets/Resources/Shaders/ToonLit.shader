// Mobile friendly stylised lighting. Uses global light values set per world by EnvironmentLighting
// so it does not depend on Unity's light objects or render pipeline features.
Shader "BalloonToHeaven/ToonLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Emission ("Emission", Color) = (0,0,0,0)
        _Rim ("Rim Strength", Range(0,2)) = 0.6
        _Gloss ("Gloss", Range(0,1)) = 0.25
        _Bands ("Light Bands", Range(1,8)) = 3
        _Flash ("Hit Flash", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _Emission;
            half _Rim;
            half _Gloss;
            half _Bands;
            half _Flash;
            float4 _BTH_LightDir;
            fixed4 _BTH_LightColor;
            fixed4 _BTH_Ambient;
            fixed4 _BTH_RimColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 l = normalize(_BTH_LightDir.xyz + float3(0.0001, 0, 0));
                float3 v = normalize(UNITY_MATRIX_V[2].xyz);
                half ndl = dot(n, l) * 0.5 + 0.5;
                half stepped = floor(ndl * _Bands + 0.5) / _Bands;
                half lightAmount = lerp(ndl, stepped, 0.55);
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                fixed3 col = albedo.rgb * (_BTH_Ambient.rgb + _BTH_LightColor.rgb * lightAmount);
                half rim = pow(1.0 - saturate(dot(n, v)), 3.0) * _Rim;
                half spec = pow(saturate(dot(reflect(-l, n), v)), 24.0) * _Gloss;
                col += _BTH_RimColor.rgb * rim + spec.xxx + _Emission.rgb;
                col = lerp(col, fixed3(1, 0.35, 0.35), _Flash);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
