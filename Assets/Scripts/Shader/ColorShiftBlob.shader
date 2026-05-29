Shader "UI/ColorShiftBlob"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // --- Set A (Player 1) — set by script ---
        [HideInInspector] _ColorA1 ("", Color) = (0.3, 0.6, 1.0, 1)
        [HideInInspector] _ColorA2 ("", Color) = (0.1, 0.8, 0.6, 1)
        [HideInInspector] _ColorA3 ("", Color) = (0.0, 0.0, 0.0, 0)
        [HideInInspector] _ColorA4 ("", Color) = (0.0, 0.0, 0.0, 0)

        // --- Set B (Player 2 / NPC) — set by script ---
        [HideInInspector] _ColorB1 ("", Color) = (0.9, 0.3, 0.3, 1)
        [HideInInspector] _ColorB2 ("", Color) = (0.9, 0.7, 0.1, 1)
        [HideInInspector] _ColorB3 ("", Color) = (0.0, 0.0, 0.0, 0)
        [HideInInspector] _ColorB4 ("", Color) = (0.0, 0.0, 0.0, 0)

        // --- Animation ---
        _Speed ("Speed", Float) = 0.2
        _BlobScale ("Blob Scale", Float) = 4.0
        _Transition ("Transition", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClipRect;

            // Set A colors
            float4 _ColorA1, _ColorA2, _ColorA3, _ColorA4;
            // Set B colors
            float4 _ColorB1, _ColorB2, _ColorB3, _ColorB4;

            float _Speed;
            float _BlobScale;
            float _Transition;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float t = _Time.y * _Speed;

                // 4 blob centres drift in Lissajous-like paths
                float2 c[4];
                c[0] = 0.5 + 0.38 * float2(sin(t * 0.70 + 0.0), cos(t * 0.50 + 1.3));
                c[1] = 0.5 + 0.38 * float2(sin(t * 0.40 + 2.1), cos(t * 0.60 + 0.5));
                c[2] = 0.5 + 0.38 * float2(sin(t * 0.30 + 4.7), cos(t * 0.80 + 3.2));
                c[3] = 0.5 + 0.38 * float2(sin(t * 0.55 + 5.9), cos(t * 0.35 + 7.1));

                float3 colsA[4] = { _ColorA1.rgb, _ColorA2.rgb, _ColorA3.rgb, _ColorA4.rgb };
                float3 colsB[4] = { _ColorB1.rgb, _ColorB2.rgb, _ColorB3.rgb, _ColorB4.rgb };
                float  actA[4]  = { _ColorA1.a,  _ColorA2.a,  _ColorA3.a,  _ColorA4.a };
                float  actB[4]  = { _ColorB1.a,  _ColorB2.a,  _ColorB3.a,  _ColorB4.a };

                float3 blendA = 0, blendB = 0;
                float  wSumA = 0, wSumB = 0;

                for (int i = 0; i < 4; i++)
                {
                    float2 d = uv - c[i];
                    float dsq = dot(d, d);
                    float w = exp(-dsq * _BlobScale);

                    blendA += colsA[i] * w * actA[i];
                    wSumA  += w * actA[i];

                    blendB += colsB[i] * w * actB[i];
                    wSumB  += w * actB[i];
                }

                // Normalise so total intensity stays ~1 regardless of active count
                float3 col;
                if (wSumA < 0.001 && wSumB < 0.001)
                    col = 0;
                else
                {
                    float3 normA = wSumA > 0.001 ? blendA / wSumA : 0;
                    float3 normB = wSumB > 0.001 ? blendB / wSumB : 0;
                    col = lerp(normA, normB, _Transition);
                }

                // Multiply by vertex colour (default white → identity)
                col *= i.color.rgb;

                // UI clip rect support
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                // Alpha clip support
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
