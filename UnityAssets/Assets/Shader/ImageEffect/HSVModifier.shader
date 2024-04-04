Shader "Hidden/HSVModifier"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HueShiftAmount ("Hue Shift Amount", float) = 0
        _SaturationBoost ("Saturation Boost", float) = 0
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

            struct hsvColor
            {
                float h;
                float s;
                float v;
            };

            hsvColor toHSV(fixed3 color)
            {
                float cMin = min(color.r, min(color.g, color.b));
                float cMax = max(color.r, max(color.g, color.b));

                float delta = cMax - cMin;

                hsvColor result;

                if (delta == 0)
                {
                    result.h = 0;
                }
                else if (cMax == color.r)
                {
                    result.h = 60 * (((color.g - color.b) / delta) % 6.0);
                }
                else if (cMax == color.g)
                {
                    result.h = 60 * (((color.b - color.r) / delta) + 2);
                }
                else if (cMax == color.b)
                {
                    result.h = 60 * (((color.r - color.g) / delta) + 4);
                }

                if (cMax == 0)
                {
                    result.s = 0;
                }
                else
                {
                    result.s = delta / cMax;
                }

                result.v = cMax;

                return result;
            }

            fixed3 toRGB(hsvColor color)
            {
                fixed3 result;

                float c = color.v * color.s;
                float x = c * (1 - abs(((color.h / 60.0) % 2) - 1));
                float m = color.v - c;

                if (color.h >= 0 && color.h < 60)
                {
                    result = fixed3(c, x, 0);
                }
                else if (color.h >= 60 && color.h < 120)
                {
                    result = fixed3(x, c, 0);
                }
                else if (color.h >= 120 && color.h < 180)
                {
                    result = fixed3(0, c, x);
                }
                else if (color.h >= 180 && color.h < 240)
                {
                    result = fixed3(0, x, c);
                }
                else if (color.h >= 240 && color.h < 300)
                {
                    result = fixed3(x, 0, c);
                }
                else if (color.h >= 300 && color.h < 360)
                {
                    result = fixed3(c, 0, x);
                }

                return result + m;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            float _HueShiftAmount;
            float _SaturationBoost;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                hsvColor colHSV = toHSV(col.rgb);

                colHSV.h = (colHSV.h + _HueShiftAmount) % 360.0;

                float saturation;
                if (_SaturationBoost <= 0)
                {
                    saturation = colHSV.s * (_SaturationBoost + 1);
                }
                else if (_SaturationBoost <= 1)
                {
                    saturation = colHSV.s + (_SaturationBoost * (1 - colHSV.s));
                }
                else
                {
                    saturation = _SaturationBoost;
                }

                colHSV.s = max(0, saturation);
                
                col.rgb = toRGB(colHSV);

                return col;
            }
            ENDCG
        }
    }
}
