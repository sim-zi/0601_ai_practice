Shader "Hidden/BrushShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrushPos ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Float) = 0.1
        _BrushStrength ("Brush Strength", Float) = 0.1
        _BrushColor ("Brush Color", Color) = (1, 0, 0, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        // Blend is not needed here if we do the math in frag
        Blend Off 

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
            float4 _BrushPos;
            float _BrushSize;
            float _BrushStrength;
            float4 _BrushColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the existing texture (tempRT passed as _MainTex)
                float4 existingCol = tex2D(_MainTex, i.uv);
                
                float d = distance(i.uv, _BrushPos.xy);
                float mask = smoothstep(_BrushSize, _BrushSize * 0.3, d);
                
                // Additive painting: New = Old + (Brush * Strength)
                float4 brushStroke = _BrushColor * mask * _BrushStrength;
                
                return saturate(existingCol + brushStroke);
            }
            ENDCG
        }
    }
}
