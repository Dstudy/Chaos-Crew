Shader "Custom/Disco2D"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Speed ("Hue Rotation Speed", float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _Speed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            float3 hsv_to_rgb(float h, float s, float v) {
                h = frac(h) * 6.0;
                float3 rgb = saturate(float3(
                    abs(h - 3.0) - 1.0,
                    2.0 - abs(h - 2.0),
                    2.0 - abs(h - 4.0)
                ));
                return ((rgb - 1.0) * s + 1.0) * v;
            }

            const float EPSILON = 1e-10;

            float3 rgb_to_hcv(in float3 rgb) {
                float4 p = (rgb.g < rgb.b) ? float4(rgb.bg, -1.0, 2.0/3.0) : float4(rgb.gb, 0.0, -1.0/3.0);
                float4 q = (rgb.r < p.x) ? float4(p.xyw, rgb.r) : float4(rgb.r, p.yzx);
                float c = q.x - min(q.w, q.y);
                float h = abs((q.w - q.y) / (6 * c + EPSILON) + q.z);
                return float3(h, c, q.x);
            }

            float3 rgb_to_hsv(float3 rgb) {
                float3 hcv = rgb_to_hcv(rgb);
                float s = hcv.y / (hcv.z + EPSILON);
                return float3(hcv.x, s, hcv.z);
            }

            v2f vert (appdata IN)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(IN.vertex);
                o.uv = IN.uv;
                o.color = IN.color * _Color;
                return o;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.uv) * IN.color;

                float3 hsv = rgb_to_hsv(c.rgb);

                float hueShift = hsv.x + _Time.x * _Speed;

                float3 rgb = hsv_to_rgb(hueShift, hsv.y, hsv.z);

                return fixed4(rgb, c.a);
            }

            ENDCG
        }
    }
}
