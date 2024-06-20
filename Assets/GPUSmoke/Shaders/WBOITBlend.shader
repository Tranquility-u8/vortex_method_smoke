Shader "WBOIT/Blend"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
        
    Texture2D _AccumTexture;
    Texture2D _RevealTexture;

    struct V2F
    {
        float4 c_pos : SV_POSITION; // [Clip] Position
        float2 uv : TEXCOORD0;      // Texcoord
    };

    V2F Vert(uint id : SV_VertexID)
    {
        V2F o;
        o.c_pos = GetFullScreenTriangleVertexPosition(id);
        o.uv = GetFullScreenTriangleTexCoord(id);
        return o;
    }
            

    float4 Frag(V2F i) : SV_Target
    {
        float4 accum = SAMPLE_TEXTURE2D(_AccumTexture, sampler_PointClamp, i.uv);
        float reveal = SAMPLE_TEXTURE2D(_RevealTexture, sampler_PointClamp, i.uv).r;
        return float4(accum.rgb / max(accum.a, 1e-6), reveal);
    }

    ENDHLSL
    
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend OneMinusSrcAlpha SrcAlpha // Reversed since Reveal is (1 - Alpha)
        Cull Off
        LOD 100
        ZTest Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}