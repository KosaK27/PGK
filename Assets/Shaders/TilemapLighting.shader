Shader "Custom/TilemapLighting"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _LightMap ("Light Map", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 worldUV : TEXCOORD1; float4 color : COLOR; };

            sampler2D _MainTex;
            sampler2D _LightMap;
            float4 _LightMap_ST;

            float _WorldMinX;
            float _WorldMinY;
            float _WorldWidth;
            float _WorldHeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldUV = float2(
                    (worldPos.x - _WorldMinX) / _WorldWidth,
                    (worldPos.y - _WorldMinY) / _WorldHeight
                );
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                fixed4 light = tex2D(_LightMap, i.worldUV);
                col.rgb *= light.rgb;
                return col;
            }
            ENDCG
        }
    }
}