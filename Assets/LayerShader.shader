Shader "LayerShader"
{
    Properties
    {
        // Layer Textures
        layerColor("Layer Color", 2D) = "red" {}
        [Normal] layerNormals("Layer Normals", 2D) = "black" {}
        layerHeightMap("Layer Height Map", 2D) = "black" {}
        layerDisplacementScale("Layer Displacement Scale", Float) = 1.0

        [HideInInspector] rasterDepthX("rasterDepthX", 2D) = "red" {}
        [HideInInspector] rasterDepthY("rasterDepthY", 2D) = "red" {}
        [HideInInspector] modelUV("modelUV", 2D) = "red" {}
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Prepare Pass"
            ZWrite On
            ZTest Less
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"

            struct appdata {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 screen_pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 view_pos : TEXCOORD1;
                half3 normal : TEXCOORD2;
            };

            sampler2D layerNormals;
            float4 layerNormals_ST;

            sampler2D layerHeightMap;
            float4 layerHeightMap_ST;

            float layerDisplacementScale;


            v2f vert (appdata v) {
                v2f o = (v2f)0;
                o.view_pos = UnityObjectToViewPos(v.vertex);
                o.screen_pos = UnityObjectToClipPos(v.vertex);
                o.normal = COMPUTE_VIEW_NORMAL;
                o.uv = TRANSFORM_TEX(v.uv, layerNormals);
                return o;
            }

            float packMask(bool2 mask) {
	            return float((mask.x ? 1 : 0) | (mask.y ? 2 : 0));
            }

            void frag(v2f input, out float4 modelUV: SV_Target0, out float4 raster: SV_Target1) {
                float3 view_pos = input.view_pos.xyz;

                // Get view-space normals
                float3 normal = input.normal;
                // normal = lerp(normal, normalize(UnpackNormal(tex2D(layerNormals, input.uv))), 0.5f);

                // Get bump value
                float bump = tex2D(layerHeightMap, input.uv).x;

                // Move view position
                view_pos += normal * bump * layerDisplacementScale;

                // Project view position on scren
                float4 proj_pos = UnityViewToClipPos(view_pos);
                proj_pos.xyz /= proj_pos.w;
                float2 targetID = (proj_pos.xy * float2(0.5, -0.5) + 0.5) * _ScreenParams.xy;

                bool2 mask	= bool2(proj_pos.w > 0, bump > 0);

                modelUV.xy = input.uv;
                raster = float4(targetID, proj_pos.z, packMask(mask));
            }
            
            ENDHLSL
        }
        
        Pass
        {
            Name "Apply Pass"

            ZWrite On
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM

            #pragma target 5.0

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

            sampler2D layerColor;
            float4 layerColor_ST;

            sampler2D layerNormals;
            float4 layerNormals_ST;

            Texture2D<uint> rasterDepthX;
            Texture2D<uint> rasterDepthY;

            sampler2D modelUV;
            float4 modelUV_ST;

            #pragma vertex Vert
            #pragma fragment FullScreenPass

            #define LAYER_MAX_DEPTH 0xFFFF

            void unpackDepth(uint value, out float depth, out uint payload) {
                depth				= (value >> 16) / float(LAYER_MAX_DEPTH);
                payload				= value & 0xFFFF;
            }

            struct DisplacedGB {
                float4 color: SV_Target0;
                float4 normal: SV_Target1;
                float depth: SV_Depth;
            };

            DisplacedGB FullScreenPass(Varyings varyings)
            {
                DisplacedGB out_gb;

                float layerDepth = 0;
                uint2 packedUV = 0;
                unpackDepth(rasterDepthX.Load(float3(varyings.positionCS.xy, 0)), layerDepth, packedUV.x);
                unpackDepth(rasterDepthY.Load(float3(varyings.positionCS.xy, 0)), layerDepth, packedUV.y);

                float2 originalUV = f16tof32(packedUV);
                float2 originalModelUV = tex2D(modelUV, originalUV).xy;

                out_gb.color = tex2D(layerColor, originalModelUV);
                out_gb.normal = tex2D(layerNormals, originalModelUV);
                out_gb.depth = layerDepth;

                return out_gb;
            }
            
            ENDHLSL
        }
    }
    Fallback Off
}
