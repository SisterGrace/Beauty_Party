Shader "Custom/AlphaRadialFillShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // 主纹理
        [Enum(Horizontal, 0, Vertical, 1,  Centre, 2, OverAnim, 3)]
        _StartPoint("StartPoint", Float) = 2
        _flip("flip", Float) = 0
        [HideInInSpector]
        _AltasPosition("AltasPosition", Vector) = (-1.0,0.0,1.0,1.0)
        _FillAmount ("Fill Amount", Range(0,1)) = 0.5 // 填充量
        _FillOffset ("Fill Offset", Range(0,1)) = 0.0 // 填充偏移
    }
    SubShader
    {
       LOD 0

		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

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
                float4 color    : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _FillAmount;
            float _FillOffset;
            fixed4 _Color;
            float _StartPoint;
            float _flip;
            float4 _AltasPosition;

            float2 GetNormalizedUV(float2 uv){

                if (_AltasPosition.x == -1.0) return uv;
	            //归一化的uv
	            float2 normalizedUV;
	            // (uv.x - 图片在图集的初始uv位置)/(图片在图集的宽的占比)
	            normalizedUV.x = ((uv.x - (_AltasPosition.x * _MainTex_TexelSize.x))/(_AltasPosition.z * _MainTex_TexelSize.x));
	            // (uv.y - 图片在图集的初始uv位置)/(图片在图集的高的占比)
	            normalizedUV.y = ((uv.y - (_AltasPosition.y * _MainTex_TexelSize.y))/(_AltasPosition.w * _MainTex_TexelSize.y));
	            
	            return normalizedUV;
            }
            
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 获取纹理颜色，包括Alpha通道
                fixed4 texColor = tex2D(_MainTex, i.uv) * i.color;
                float2 uv = GetNormalizedUV(i.uv);
                // 水平
                if (_StartPoint == 0)
                {
                    texColor.a = texColor.a * (_flip == 0 ? step(uv.x, _FillAmount) : step((1 - _FillAmount), uv.x));
                }
                else if(_StartPoint == 1)
                {
                    texColor.a = texColor.a * (_flip == 0 ? step(uv.y, _FillAmount) : step((1 - _FillAmount), uv.y));
                }
                else if (_StartPoint == 3)
                {
                    float gradient = _flip == 0 ? 1 - (i.uv.x + ( 1 - i.uv.y)) / 2.0 :  (i.uv.x + 1 -i.uv.y) / 2.0;
                    texColor.a = texColor.a * step(gradient, _FillAmount);
                }
                else
                {
                    // 将UV坐标从[0,1]转换到[-1,1]
                    float2 uv = i.uv * 2.0 - 1.0;

                    // 计算当前像素相对于圆心的角度（以弧度表示）
                    float angle = atan2(uv.y, uv.x) / (2.0 * UNITY_PI) + 0.5 + _FillOffset;
                    angle = frac(angle); // 将角度保持在[0,1]范围内

                    // 仅当角度小于填充量时，绘制该像素
                    if (angle >_FillAmount)
                    {
                         return fixed4(0, 0, 0, 0); // 不在填充范围内，保持透明
                    }
                }
                texColor.a *= i.color.a;
                texColor.rgb *= texColor.a;
                return texColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent"
}