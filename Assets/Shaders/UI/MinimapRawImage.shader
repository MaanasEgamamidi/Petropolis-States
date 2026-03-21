// Smooth circular / square minimap with optional anti-aliased border (shader-drawn).
Shader "Custom/UI/MinimapRawImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shape)]
        _UseCircularClip ("Use circular clip", Range(0,1)) = 1
        _EdgeAA ("Edge anti-alias scale", Range(0.5, 4)) = 2

        [Header(Border)]
        _UseBorder ("Use border", Range(0,1)) = 0
        _BorderWidthUV ("Border width (UV)", Range(0.005, 0.15)) = 0.03
        _BorderColor ("Border color", Color) = (0.08, 0.08, 0.12, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float _UseCircularClip;
            float _EdgeAA;
            float _UseBorder;
            float _BorderWidthUV;
            fixed4 _BorderColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 texCol = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                float aaScale = max(_EdgeAA, 0.5);

                if (_UseCircularClip > 0.5)
                {
                    float2 p = IN.texcoord - float2(0.5, 0.5);
                    float len = length(p);
                    const float R = 0.5;
                    float aalen = max(fwidth(len) * aaScale, 0.0008);

                    if (_UseBorder < 0.5 || _BorderWidthUV < 0.0001)
                    {
                        half4 c = texCol;
                        c.a *= 1.0 - smoothstep(R - aalen, R + aalen, len);
                        return c;
                    }

                    float mapR = R - _BorderWidthUV;
                    float innerMapMask = 1.0 - smoothstep(mapR - aalen, mapR + aalen, len);
                    float borderRing = saturate(
                        smoothstep(mapR - aalen, mapR + aalen, len) *
                        (1.0 - smoothstep(R - aalen, R + aalen, len)));
                    float outerMask = 1.0 - smoothstep(R - aalen, R + aalen, len);

                    float am = texCol.a * innerMapMask;
                    float ab = borderRing * _BorderColor.a;
                    float outA = am + ab * (1.0 - am);
                    half3 outRgb = texCol.rgb * am + _BorderColor.rgb * ab * (1.0 - am);
                    if (outA > 1e-5)
                        outRgb /= outA;
                    return half4(outRgb, outA * outerMask);
                }
                else
                {
                    if (_UseBorder < 0.5 || _BorderWidthUV < 0.0001)
                        return texCol;

                    float2 q = min(IN.texcoord.xy, 1.0 - IN.texcoord.xy);
                    float distEdge = min(q.x, q.y);
                    float aad = max(fwidth(distEdge) * aaScale, 0.0008);
                    float bw = _BorderWidthUV;

                    float innerMapMask = smoothstep(bw - aad, bw + aad, distEdge);
                    float borderRing = 1.0 - smoothstep(bw - aad, bw + aad, distEdge);

                    float am = texCol.a * innerMapMask;
                    float ab = borderRing * _BorderColor.a;
                    float outA = am + ab * (1.0 - am);
                    half3 outRgb = texCol.rgb * am + _BorderColor.rgb * ab * (1.0 - am);
                    if (outA > 1e-5)
                        outRgb /= outA;
                    return half4(outRgb, outA);
                }
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
