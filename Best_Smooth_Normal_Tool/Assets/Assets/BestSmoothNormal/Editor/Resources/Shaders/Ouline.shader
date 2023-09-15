Shader "TA/Ouline"
{
    Properties
    {
        _OutlineWidth("Outline Width", Range(0.01, 1)) = 0.24
        _OutLineColor("OutLine Color", Color) = (0.5,0.5,0.5,1)
        [KeywordEnum(Tangent, Vertex, uv2, uv3, uv4, uv5, uv6, uv7, uv8)]_OutLineMode("OutLine Mode", float) = 0
        [Toggle(IS_MAP01_ON)]_IsMapO1("Is Mapping to [O, 1]", int) = 0
        [Toggle(IS_OCT_ON)]_IsUseOctahedron("Is Use Octahedron", int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        pass
        {
            Tags {"LightMode" = "ForwardBase"}

            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 vert(appdata_base v) : SV_POSITION
            {
                return UnityObjectToClipPos(v.vertex);
            }

            half4 frag() : SV_TARGET
            {
                return half4(1,1,1,1);
            }
            ENDCG
        }
        Pass
        {
            Tags {"LightMode" = "ForwardBase"}

            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _OUTLINEMODE_TANGENT _OUTLINEMODE_VERTEX _OUTLINEMODE_UV2 _OUTLINEMODE_UV3 _OUTLINEMODE_UV4 _OUTLINEMODE_UV5 _OUTLINEMODE_UV6 _OUTLINEMODE_UV7 _OUTLINEMODE_UV8
            #pragma multi_compile IS_MAP01_ON _
            #pragma multi_compile IS_OCT_ON _
            #include "UnityCG.cginc"

            half _OutlineWidth;
            half4 _OutLineColor;

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float4 tangent : TANGENT;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                float4 uv4 : TEXCOORD4;
                float4 uv5 : TEXCOORD5;
                float4 uv6 : TEXCOORD6;
                float4 uv7 : TEXCOORD7;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float3 OctahedronToUnitVector(float2 oct)
            {
                float3 unitVec = float3(oct, 1 - dot(float2(1, 1), abs(oct)));
                if (unitVec.z < 0)
                {
                    unitVec.xy = (1 - abs(unitVec.yx)) * float2(unitVec.x >= 0 ? 1 : -1, unitVec.y >= 0 ? 1 : -1);
                }
                return normalize(unitVec);
            }

            v2f vert(a2v v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                // smooth normal 
                float3 smoothNormal = float3(0.0, 0.0, 0.0);
                // 保存在切线中
                #if _OUTLINEMODE_TANGENT
                {
                    #if IS_MAP01_ON
                    smoothNormal = 2 * v.tangent.xyz - 1;
                    #else
                    smoothNormal = v.tangent.xyz;
                    #endif
                }
                // 保存在顶点色中
                #elif _OUTLINEMODE_VERTEX
                {
                    #if IS_MAP01_ON
                    smoothNormal = 2 * v.color.rgb - 1;
                    #else
                    smoothNormal = v.color.rgb;
                    #endif
                }
                #else
                {
                    float4 uv = float4(0.0, 0.0, 0.0, 0.0);
                    // 保存在 uv2 中
                    #if _OUTLINEMODE_UV2
                    uv = v.uv1;
                    #elif _OUTLINEMODE_UV3
                    uv = v.uv2;
                    #elif _OUTLINEMODE_UV4
                    uv = v.uv3;
                    #elif _OUTLINEMODE_UV5
                    uv = v.uv4;
                    #elif _OUTLINEMODE_UV6
                    uv = v.uv5;
                    #elif _OUTLINEMODE_UV7
                    uv = v.uv6;
                    #elif _OUTLINEMODE_UV8
                    uv = v.uv7;
                    #endif

                    #if IS_MAP01_ON
                    smoothNormal = uv.xyz * 2 - 1;
                    #else
                    smoothNormal = uv.xyz;
                    #endif

                    #if IS_OCT_ON
                    float2 oct = smoothNormal.xy;
                    smoothNormal = OctahedronToUnitVector(oct);
                    #endif

                    float3 T = normalize(v.tangent.xyz);
                    float3 N = normalize(v.normal);
                    float3 B = normalize(cross(N, T)) * v.tangent.w;
                    float3x3 TBN = float3x3(T, B, N);
                    smoothNormal = mul(smoothNormal, TBN);
                }
                #endif

                o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + smoothNormal * _OutlineWidth * 0.01 ,1));
                return o;
            }

            half4 frag(v2f i) : SV_TARGET
            {
                return _OutLineColor;
            }
            ENDCG
        }
    }
}