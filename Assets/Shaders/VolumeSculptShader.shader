Shader "Custom/VolumeSculptShader"
{
    Properties
    {
        _FrontTex ("Front (XY-R)", 2D) = "black" {}
        _BackTex ("Back (XY-R)", 2D) = "black" {}
        _LeftTex ("Left (YZ-G)", 2D) = "black" {}
        _RightTex ("Right (YZ-G)", 2D) = "black" {}
        _TopTex ("Top (XZ-B)", 2D) = "black" {}
        _BottomTex ("Bottom (XZ-B)", 2D) = "black" {}
        _CubeColor ("Solid Color", Color) = (0.1, 0.1, 0.1, 1)
        _StepSize ("Step Size", Float) = 0.02
    }
SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            sampler2D _FrontTex, _BackTex, _LeftTex, _RightTex, _TopTex, _BottomTex;
            float4 _CubeColor;
            float _StepSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz; // Range -0.5 to 0.5
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldViewDir = normalize(worldPos - _WorldSpaceCameraPos);
                o.viewDir = mul((float3x3)unity_WorldToObject, worldViewDir);
                return o;
            }

            bool IsSolid(float3 p) {
                float3 uvw = p + 0.5;
                
                // Allow waterfall to show at the top
                if (uvw.y >= 0.98) return false;

                float front = tex2Dlod(_FrontTex, float4(uvw.xy, 0, 0)).r;
                float back = tex2Dlod(_BackTex, float4(1-uvw.x, uvw.y, 0, 0)).r;
                if (uvw.z > (1.0 - front)) return false;
                if (uvw.z < back) return false;

                float right = tex2Dlod(_RightTex, float4(1-uvw.z, uvw.y, 0, 0)).g;
                float left = tex2Dlod(_LeftTex, float4(uvw.z, uvw.y, 0, 0)).g;
                if (uvw.x > (1.0 - right)) return false;
                if (uvw.x < left) return false;

                float top = tex2Dlod(_TopTex, float4(uvw.x, 1-uvw.z, 0, 0)).b;
                float bottom = tex2Dlod(_BottomTex, float4(uvw.x, uvw.z, 0, 0)).b;
                if (uvw.y > (1.0 - top)) return false;
                if (uvw.y < bottom) return false;

                return true;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 rayPos = i.localPos;
                float3 rayDir = normalize(i.viewDir);
                
                float4 finalCol = float4(0,0,0,0);

                for(int s = 0; s < 80; s++) {
                    bool solid = IsSolid(rayPos);
                    
                    if (solid) {
                        float depth = distance(i.localPos, rayPos);
                        float light = 1.0 - depth;
                        float4 solidCol = fixed4(_CubeColor.rgb * light, 1.0);
                        finalCol.rgb = finalCol.rgb + (1.0 - finalCol.a) * solidCol.rgb;
                        finalCol.a = 1.0;
                        break;
                    }

                    rayPos += rayDir * _StepSize;
                    if (any(abs(rayPos) > 0.501)) break;
                    if (finalCol.a >= 0.98) break;
                }

                return finalCol;
            }
            ENDCG
        }
    }
}
