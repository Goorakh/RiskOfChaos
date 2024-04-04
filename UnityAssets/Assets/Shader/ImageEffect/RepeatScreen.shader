Shader "Hidden/RepeatScreen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RepeatCount ("Repeat Factor", float) = 1
        _CenterOffset ("Center Offset", Vector) = (0.5, 0.5, 0, 0)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _RepeatCount;
            float4 _CenterOffset;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centerOffset = _CenterOffset.xy;
                i.uv = (((i.uv - centerOffset) * _RepeatCount) + centerOffset) % 1.0;

                if (i.uv.x < 0)
                {
                    i.uv.x += 1;
                }

                if (i.uv.y < 0)
                {
                    i.uv.y += 1;
                }

                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
