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

        _ColorA1 ("Set A – Colour 1", Color) = (0.3, 0.6, 1.0, 1)
        _ColorA2 ("Set A – Colour 2", Color) = (0.1, 0.8, 0.6, 1)
        _ColorA3 ("Set A – Colour 3", Color) = (0.0, 0.0, 0.0, 0)
        _ColorA4 ("Set A – Colour 4", Color) = (0.0, 0.0, 0.0, 0)

        _ColorB1 ("Set B – Colour 1", Color) = (0.9, 0.3, 0.3, 1)
        _ColorB2 ("Set B – Colour 2", Color) = (0.9, 0.7, 0.1, 1)
        _ColorB3 ("Set B – Colour 3", Color) = (0.0, 0.0, 0.0, 0)
        _ColorB4 ("Set B – Colour 4", Color) = (0.0, 0.0, 0.0, 0)

        _BgColor ("Background", Color) = (1, 1, 1, 1)

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
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClipRect;

            float4 _ColorA1, _ColorA2, _ColorA3, _ColorA4;
            float4 _ColorB1, _ColorB2, _ColorB3, _ColorB4;
            float4 _BgColor;
            float _Speed, _BlobScale, _Transition;

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = 1;
                o.worldPos = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y * _Speed;

                // 4 blobs — fully unrolled, no arrays, no loops
                float2 c1 = 0.5 + 0.38 * float2(sin(t * 0.70 + 0.0), cos(t * 0.50 + 1.3));
                float2 c2 = 0.5 + 0.38 * float2(sin(t * 0.40 + 2.1), cos(t * 0.60 + 0.5));
                float2 c3 = 0.5 + 0.38 * float2(sin(t * 0.30 + 4.7), cos(t * 0.80 + 3.2));
                float2 c4 = 0.5 + 0.38 * float2(sin(t * 0.55 + 5.9), cos(t * 0.35 + 7.1));

                float3 blendA = 0, blendB = 0;
                float wA = 0, wB = 0;

                // Blob 1
                float w = exp(-dot(uv - c1, uv - c1) * _BlobScale);
                blendA += _ColorA1.rgb * w * _ColorA1.a;
                wA += w * _ColorA1.a;
                blendB += _ColorB1.rgb * w * _ColorB1.a;
                wB += w * _ColorB1.a;

                // Blob 2
                w = exp(-dot(uv - c2, uv - c2) * _BlobScale);
                blendA += _ColorA2.rgb * w * _ColorA2.a;
                wA += w * _ColorA2.a;
                blendB += _ColorB2.rgb * w * _ColorB2.a;
                wB += w * _ColorB2.a;

                // Blob 3
                w = exp(-dot(uv - c3, uv - c3) * _BlobScale);
                blendA += _ColorA3.rgb * w * _ColorA3.a;
                wA += w * _ColorA3.a;
                blendB += _ColorB3.rgb * w * _ColorB3.a;
                wB += w * _ColorB3.a;

                // Blob 4
                w = exp(-dot(uv - c4, uv - c4) * _BlobScale);
                blendA += _ColorA4.rgb * w * _ColorA4.a;
                wA += w * _ColorA4.a;
                blendB += _ColorB4.rgb * w * _ColorB4.a;
                wB += w * _ColorB4.a;

                float3 blobColor;
                if (wA < 0.001 && wB < 0.001)
                    blobColor = _BgColor.rgb;
                else
                {
                    float3 normA = wA > 0.001 ? blendA / wA : _BgColor.rgb;
                    float3 normB = wB > 0.001 ? blendB / wB : _BgColor.rgb;
                    blobColor = lerp(normA, normB, _Transition);
                }

                // Blend with background — edges fade smoothly into _BgColor
                float totalW = lerp(wA, wB, _Transition);
                float bgBlend = saturate(totalW * 2.5);
                float3 result = lerp(_BgColor.rgb, blobColor, bgBlend);

                result *= i.color.rgb;
                float4 col = float4(result, 1);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
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
