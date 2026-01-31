Shader "Hidden/CRTEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _ScreenSize; // xy: 像素化分辨率, zw: 原始分辨率
            
            // CRT效果参数
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _ChromaticAberration;
            float _ChromaticAngle;
            float _Curvature;
            float _Vignette;
            float _Brightness;
            float _CRTTime; // 使用自定义时间变量名，避免与Unity内置_Time冲突

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // CRT屏幕弯曲函数
            float2 CRTCurve(float2 uv)
            {
                uv = uv * 2.0 - 1.0; // 转换到[-1, 1]
                float2 offset = abs(uv.yx) / _Curvature;
                uv = uv + uv * offset * offset;
                uv = uv * 0.5 + 0.5; // 转回[0, 1]
                return uv;
            }

            // 扫描线效果
            float Scanline(float2 uv)
            {
                // 使用原始屏幕分辨率计算扫描线，确保像素级精度
                float scanline = sin((uv.y * _ScreenSize.w * _ScanlineCount) + (_CRTTime * _ScanlineSpeed * 10.0));
                scanline = scanline * 0.5 + 0.5; // 归一化到[0, 1]
                return lerp(1.0, scanline, _ScanlineIntensity);
            }

            // 色差效果 - 分离RGB通道
            float4 ChromaticAberration(sampler2D tex, float2 uv)
            {
                if (_ChromaticAberration <= 0.0)
                {
                    return tex2D(tex, uv);
                }

                // 计算偏移方向
                float2 direction = float2(cos(_ChromaticAngle), sin(_ChromaticAngle));
                
                // 像素偏移（基于原始分辨率）
                float2 offset = direction * (_ChromaticAberration / _ScreenSize.zw);

                // 分别采样RGB通道
                float r = tex2D(tex, uv + offset).r;
                float g = tex2D(tex, uv).g;
                float b = tex2D(tex, uv - offset).b;

                return float4(r, g, b, 1.0);
            }

            // 暗角效果
            float Vignette(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float vignette = 1.0 - dot(uv, uv) * _Vignette;
                return saturate(vignette);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 1. 应用CRT曲面
                if (_Curvature > 0.0)
                {
                    uv = CRTCurve(uv);
                    
                    // 超出屏幕范围显示黑色
                    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    {
                        return fixed4(0, 0, 0, 1);
                    }
                }

                // 2. 应用色差效果
                fixed4 col = ChromaticAberration(_MainTex, uv);

                // 3. 应用扫描线
                if (_ScanlineIntensity > 0.0)
                {
                    float scanline = Scanline(uv);
                    col.rgb *= scanline;
                }

                // 4. 应用暗角
                if (_Vignette > 0.0)
                {
                    float vignette = Vignette(uv);
                    col.rgb *= vignette;
                }

                // 5. 亮度调整
                col.rgb *= _Brightness;

                // 6. 添加轻微的噪点（可选，增强CRT感觉）
                float noise = frac(sin(dot(uv + _CRTTime * 0.001, float2(12.9898, 78.233))) * 43758.5453);
                col.rgb += (noise - 0.5) * 0.02;

                return col;
            }
            ENDCG
        }
    }
    
    FallBack off
}
