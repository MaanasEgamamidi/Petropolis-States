Shader "Petropolis/UI/UIBackgroundBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 0.92)
        _Blur ("Blur", Range(0, 10)) = 1.25

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            Name "Default"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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
                float4 screenPos : TEXCOORD0;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Blur;
            CBUFFER_END

            float4 _ClipRect;

            float4 ClipVertex(float3 posOS)
            {
                #if UNITY_UV_STARTS_AT_TOP
                return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(posOS, 1.0)));
                #else
                return TransformObjectToHClip(posOS);
                #endif
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                float4 clipPos = ClipVertex(v.vertex.xyz);
                o.vertex = clipPos;
                o.screenPos = ComputeScreenPos(clipPos);
                o.texcoord = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
                o.color = v.color * _Color;
                return o;
            }

            // Single 3x3 Gaussian; used at multiple radii so large blurs don't leave huge gaps (undersampling).
            float3 GaussianBlur9(float2 uv, float2 texelBlur)
            {
                float3 c =
                    SampleSceneColor(uv + texelBlur * float2(-1, -1)) * 0.0625 +
                    SampleSceneColor(uv + texelBlur * float2(0, -1)) * 0.125 +
                    SampleSceneColor(uv + texelBlur * float2(1, -1)) * 0.0625 +
                    SampleSceneColor(uv + texelBlur * float2(-1, 0)) * 0.125 +
                    SampleSceneColor(uv) * 0.25 +
                    SampleSceneColor(uv + texelBlur * float2(1, 0)) * 0.125 +
                    SampleSceneColor(uv + texelBlur * float2(-1, 1)) * 0.0625 +
                    SampleSceneColor(uv + texelBlur * float2(0, 1)) * 0.125 +
                    SampleSceneColor(uv + texelBlur * float2(1, 1)) * 0.0625;
                return c;
            }

            float3 BlurMultiScale(float2 uv, float2 texelStep)
            {
                float3 a = GaussianBlur9(uv, texelStep * 0.38);
                float3 b = GaussianBlur9(uv, texelStep * 0.72);
                float3 c = GaussianBlur9(uv, texelStep * 1.08);
                return a * 0.28 + b * 0.36 + c * 0.36;
            }

            half4 frag(v2f IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                // texelStep scales with blur strength; multi-scale fills in between sparse taps at high _Blur.
                float2 texelStep = _CameraOpaqueTexture_TexelSize.xy * _Blur * 1.35;
                float3 blurred = BlurMultiScale(uv, texelStep);

                float2 clipMask = step(_ClipRect.xy, IN.worldPosition.xy) * step(IN.worldPosition.xy, _ClipRect.zw);
                float mask = clipMask.x * clipMask.y;

                half4 c = half4(blurred, 1.0) * IN.color;
                c.a *= mask;
                return c;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
