Shader "DormRoom/TrashOutline"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+20" }
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 Vertex(float4 positionOS : POSITION, float3 normalOS : NORMAL) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                positionWS += TransformObjectToWorldNormal(normalOS) * 0.008;
                return TransformWorldToHClip(positionWS);
            }

            half4 Fragment() : SV_Target
            {
                return half4(1, 0.87, 0.03, 1);
            }
            ENDHLSL
        }
    }
}
