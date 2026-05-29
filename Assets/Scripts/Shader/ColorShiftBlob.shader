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
        _ColorA1 ("Set A – Colour 1", Color) = (0.3, 0.6, 1.0, 1)
        _ColorA2 ("Set A – Colour 2", Color) = (0.1, 0.8, 0.6, 1)
        _ColorA3 ("Set A – Colour 3", Color) = (0.0, 0.0, 0.0, 0)
        _ColorA4 ("Set A – Colour 4", Color) = (0.0, 0.0, 0.0, 0)

        // --- Set B (Player 2 / NPC) — set by script ---
        _ColorB1 ("Set B – Colour 1", Color) = (0.9, 0.3, 0.3, 1)
        _ColorB2 ("Set B – Colour 2", Color) = (0.9, 0.7, 0.1, 1)
        _ColorB3 ("Set B – Colour 3", Color) = (0.0, 0.0, 0.0, 0)
        _ColorB4 ("Set B – Colour 4", Color) = (0.0, 0.0, 0.0, 0)

        // --- Animation ---
        _Speed ("Speed", Float) = 0.2
        _BlobScale ("Blob Scale", Float) = 4.0
        _Transition ("Transition (0=A, 1=B)", Range(0,1)) = 0
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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

            float4 _ColorA1, _ColorA2, _ColorA3, _ColorA4;
            float4 _ColorB1, _ColorB2, _ColorB3, _ColorB4;
            float _Speed, _BlobScale, _Transition;

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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float t = _Time.y * _Speed;

                // 4 blob centres — each drifts on its own Lissajous path
                float2 c[4] =
                {
                    0.5 + 0.38 * float2(sin(t * 0.70 + 0.0), cos(t * 0.50 + 1.3)),
                    0.5 + 0.38 * float2(sin(t * 0.40 + 2.1), cos(t * 0.60 + 0.5)),
                    0.5 + 0.38 * float2(sin(t * 0.30 + 4.7), cos(t * 0.80 + 3.2)),
                    0.5 + 0.38 * float2(sin(t * 0.55 + 5.9), cos(t * 0.35 + 7.1))
                };

                float4 cols[4] = { _ColorA1, _ColorA2, _ColorA3, _ColorA4 };
                float4 colb[4] = { _ColorB1, _ColorB2, _ColorB3, _ColorB4 };

                float3 blendA = 0, blendB = 0;
                float  wA = 0, wB = 0;

                for (int i = 0; i < 4; i++)
                {
                    float2 d = uv - c[i];
                    float w = exp(-dot(d, d) * _BlobScale);

                    blendA += cols[i].rgb * w * cols[i].a;
                    wA     += w * cols[i].a;

                    blendB += colb[i].rgb * w * colb[i].a;
                    wB     += w * colb[i].a;
                }

                float3 blended;
                if (wA < 0.001 && wB < 0.001)
                    blended = 0;
                else
                {
                    float3 normA = wA > 0.001 ? blendA / wA : 0;
                    float3 normB = wB > 0.001 ? blendB / wB : 0;
                    blended = lerp(normA, normB, _Transition);
                }

                blended *= i.color.rgb;
                float4 col = float4(blended, 1);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
