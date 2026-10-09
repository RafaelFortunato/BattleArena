// Additive fresnel rim drawn over a model as an extra material, for frost-slowed enemies: the body keeps its own
// material and lighting, and its silhouette edges glow icy blue. Works on skinned meshes (no vertex tricks).
Shader "BattleArena/FrostFresnel"
{
    Properties
    {
        [HDR] _RimColor ("Rim Color", Color) = (0.45, 0.85, 1.6, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.2
        _Fill ("Fill (whole-body tint)", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FrostFresnel"
            Tags { "LightMode" = "UniversalForward" }

            // Over the body's own surface: same depth, so LEqual passes exactly where the body is visible.
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half _RimPower;
                half _Fill;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half facing = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rim = pow(1.0h - facing, _RimPower);
                return half4(_RimColor.rgb * (rim + _Fill), 1.0h);
            }
            ENDHLSL
        }
    }
}
