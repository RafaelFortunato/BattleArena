// World-space health bar drawn on a unit quad.
// The quad is billboarded towards the camera in the vertex shader (no per-frame script work),
// and the fill amount is a per-instance property so all bars render with GPU instancing.
Shader "BattleArena/HealthBar"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.9, 0.2, 0.2, 1)
        _BackColor ("Back Color", Color) = (0.05, 0.05, 0.08, 0.75)
        _BorderColor ("Border Color", Color) = (0, 0, 0, 0.9)
        _Size ("Size (width, height)", Vector) = (1.2, 0.14, 0, 0)
        _Border ("Border (world units)", Float) = 0.025
        [PerRendererData] _Fill ("Fill", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HealthBar"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _BackColor;
                half4 _BorderColor;
                float4 _Size;
                float _Border;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _Fill)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Expand the quad along the camera's right/up axes around the object's pivot.
                float3 center = TransformObjectToWorld(float3(0, 0, 0));
                float3 cameraRight = UNITY_MATRIX_V[0].xyz;
                float3 cameraUp = UNITY_MATRIX_V[1].xyz;
                float3 positionWS = center
                    + cameraRight * (input.positionOS.x * _Size.x)
                    + cameraUp * (input.positionOS.y * _Size.y);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float fill = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Fill);

                float2 border = _Border / _Size.xy;
                float2 uv = input.uv;
                if (any(uv < border) || any(uv > 1.0 - border))
                    return _BorderColor;

                float x = (uv.x - border.x) / (1.0 - 2.0 * border.x);
                return x <= fill ? _FillColor : _BackColor;
            }
            ENDHLSL
        }
    }
}
