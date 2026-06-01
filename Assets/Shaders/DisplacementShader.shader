Shader "Custom/DisplacementShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _HeightMap ("Height Map", 2D) = "black" {}
        _HeightScale ("Height Scale", Float) = 1.0
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ExpansionDir ("Expansion Direction", Vector) = (0,1,0,0)
        _MinAlpha ("Min Alpha", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float height : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _HeightMap;
            float4 _MainTex_ST;
            float _HeightScale;
            float4 _BaseColor;
            float3 _ExpansionDir;
            float _MinAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                float height = tex2Dlod(_HeightMap, float4(v.uv, 0, 0)).r;
                
                // Displace along the expansion direction
                float3 displacedPos = v.vertex.xyz + _ExpansionDir * height * _HeightScale;
                
                o.vertex = UnityObjectToClipPos(float4(displacedPos, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.height = height;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _BaseColor;
                
                // Calculate alpha: parts with height are visible, background is transparent or faint
                float alpha = max(_MinAlpha, i.height);
                
                // Add some shading based on height
                col.rgb *= (0.5 + i.height * 0.5);
                
                return fixed4(col.rgb, alpha * _BaseColor.a);
            }
            ENDCG
        }
    }
}
