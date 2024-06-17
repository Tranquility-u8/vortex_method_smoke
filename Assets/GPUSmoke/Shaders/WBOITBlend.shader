Shader "WBOIT/Blend"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // The Blit.hlsl file provides the vertex shader (Vert),
    // the input structure (Attributes), and the output structure (Varyings)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        
    Texture2D _AccumTexture;
    Texture2D _RevealTexture;

    float4 Blend(Varyings input) : SV_Target
    {
        float3 background = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, input.texcoord).rgb;
        float4 accum = SAMPLE_TEXTURE2D(_AccumTexture, sampler_PointClamp, input.texcoord);
        float reveal = SAMPLE_TEXTURE2D(_RevealTexture, sampler_PointClamp, input.texcoord).r;
        float4 color = float4(accum.rgb / max(accum.a, 1e-6), reveal);
        return float4(lerp(color.rgb, background, color.a), 1.0);
    }

    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Blend
            
            ENDHLSL
        }
    }
}