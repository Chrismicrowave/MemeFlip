Shader "Custom/SingleTextureRing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _Threshold ("Alpha Threshold", Range(0,1)) = 0.5

        _Thickness ("Ring Thickness", Range(0.001,0.2)) = 0.05

        _Color ("Ring Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Threshold;
            float _Thickness;
            float4 _Color;

            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float sampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            fixed4 frag (Interpolators i) : SV_Target
            {
                float a = sampleAlpha(i.uv);

                // edge detection via local contrast
                float2 offset = float2(_Thickness, 0);

                float aR = sampleAlpha(i.uv + offset);
                float aL = sampleAlpha(i.uv - offset);
                float aU = sampleAlpha(i.uv + offset.yx);
                float aD = sampleAlpha(i.uv - offset.yx);

                float edge = abs(a - aR) +
                             abs(a - aL) +
                             abs(a - aU) +
                             abs(a - aD);

                edge = saturate(edge);

                fixed4 col = _Color;
                col.a *= edge;

                return col;
            }
            ENDCG
        }
    }
}