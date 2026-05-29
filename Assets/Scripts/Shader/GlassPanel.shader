Shader "UI/GlassPanelAdvanced"
{
    Properties
    {
        // =========================
        // GLASS
        // =========================
        _BlurTex ("Blur Texture", 2D) = "white" {}
        _GlassOpacity ("Glass Opacity", Range(0,1)) = 0.6
        _GlassTintColor ("Glass Tint", Color) = (1,1,1,1)
        _GlassTintStrength ("Tint Strength", Range(0,1)) = 0.2

        // =========================
        // EDGE
        // =========================
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _EdgeThickness ("Edge Thickness", Range(0.001,0.5)) = 0.08
        _EdgeIntensity ("Edge Intensity", Range(0,5)) = 1.5
        _EdgeFade ("Edge Fade", Range(0,1)) = 1.0

        // =========================
        // SHADOW
        // =========================
        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
        _ShadowOpacity ("Shadow Opacity", Range(0,1)) = 0.4
        _ShadowSoftness ("Shadow Softness", Range(0.001,0.5)) = 0.15
        _ShadowOffset ("Shadow Offset", Vector) = (0,-0.05,0,0)

        // =========================
        // SHAPE
        // =========================
        _Radius ("Corner Radius", Range(0,0.5)) = 0.2
        _Size ("Panel Size", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // =========================
            // TEXTURES
            // =========================
            sampler2D _BlurTex;

            // =========================
            // GLASS
            // =========================
            float _GlassOpacity;
            float _GlassTintStrength;
            float4 _GlassTintColor;

            // =========================
            // EDGE
            // =========================
            float4 _EdgeColor;
            float _EdgeThickness;
            float _EdgeIntensity;
            float _EdgeFade;

            // =========================
            // SHADOW
            // =========================
            float4 _ShadowColor;
            float _ShadowOpacity;
            float _ShadowSoftness;
            float4 _ShadowOffset;

            // =========================
            // SHAPE
            // =========================
            float _Radius;
            float2 _Size;

            // =========================
            // VERT / FRAG DATA
            // =========================
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localPos : TEXCOORD1;
            };

            // =========================
            // ROUND RECT SDF
            // =========================
            float RoundedBoxSDF(float2 p, float2 b, float r)
            {
                float2 d = abs(p) - b + r;
                return length(max(d, 0.0)) - r;
            }

            // =========================
            // GLASS
            // =========================
            float3 ApplyGlass(float2 uv)
            {
                float3 blurred = tex2D(_BlurTex, uv).rgb;
                float3 tinted = lerp(blurred, _GlassTintColor.rgb, _GlassTintStrength);
                return tinted * _GlassOpacity;
            }

            // =========================
            // EDGE HIGHLIGHT
            // =========================
            float3 ApplyEdge(float2 p, float2 size, float3 col)
            {
                float d = RoundedBoxSDF(p, size, _Radius);

                float edge = smoothstep(_EdgeThickness, 0.0, abs(d));
                edge = pow(edge, 1.5);

                float3 highlight = _EdgeColor.rgb * edge * _EdgeIntensity;

                return col + highlight * _EdgeFade;
            }

            // =========================
            // SHADOW
            // =========================
            float4 ApplyShadow(float2 p, float2 size)
            {
                float2 sp = p - _ShadowOffset.xy;

                float d = RoundedBoxSDF(sp, size, _Radius);

                float shadow = smoothstep(_ShadowSoftness, 0.0, d);

                float a = shadow * _ShadowOpacity;

                return float4(_ShadowColor.rgb, a);
            }

            // =========================
            // VERTEX
            // =========================
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                // center UV to -1..1 space for SDF
                o.localPos = (v.uv - 0.5) * 2.0;

                return o;
            }

            // =========================
            // FRAGMENT
            // =========================
            float4 frag(v2f i) : SV_Target
            {
                float2 size = _Size;

                // -------- glass base --------
                float3 col = ApplyGlass(i.uv);

                // -------- edge --------
                col = ApplyEdge(i.localPos, size, col);

                // -------- shadow --------
                float4 shadow = ApplyShadow(i.localPos, size);

                // composite shadow behind
                col = lerp(col, shadow.rgb, shadow.a);

                // -------- final alpha --------
                float alpha = _GlassOpacity;

                return float4(col, alpha);
            }

            ENDHLSL
        }
    }
}