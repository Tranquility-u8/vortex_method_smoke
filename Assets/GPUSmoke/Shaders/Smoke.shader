Shader "Unlit/Smoke"
{
    Properties 
    {
        _SpriteTex ("Texture", 2D) = "white" {}
        _Radius ("Radius", float) = 0.5
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma target 5.0

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "VortexMethod.cginc"
            #include "ParticleCluster.cginc"
        
            PC_DEF_UNIFORM
            PC_DEF_BUFFER(TracerParticle)
            
            float _Radius;
            Texture2D _SpriteTex;
            SamplerState sampler_SpriteTex;

            struct V2F
            {
                float4 pos : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            V2F vert(uint id : SV_VertexID)
            {
                float4 c = mul(UNITY_MATRIX_VP, float4(PC_GET(UNSAFE, id / 6u).pos, 1.0));
                float4 dx = UNITY_MATRIX_P[0] * _Radius;
                float4 dy = UNITY_MATRIX_P[1] * _Radius;

                id %= 6u;
                id = id < 3u ? id : 6u - id;
                int ix = id >> 1u, iy = id & 1u;

                V2F o;
                o.pos = c + dx * (ix * 2 - 1) + dy * (iy * 2 - 1);
                o.texcoord = float2(ix, iy);
                
                return o;
            }
            
            float4 frag(V2F i) : COLOR
            {
                return _SpriteTex.Sample(sampler_SpriteTex, i.texcoord);
            }

            ENDCG

        }
        // UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
    Fallback Off
}
