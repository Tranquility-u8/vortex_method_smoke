Shader "Custom/Smoke2"
{
    Properties 
    {
        [Header(Surface)]
        _SpriteTex ("MainTex", 2D) = "white" {}
        _Normal ("Normal", 2D) = "white" {}
        _Width ("Width", Range(0.01, 1)) = 0.1
        _Height ("Height", Range(0.01, 1)) = 0.1
        _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        _ClipThreshold ("ClipThreshold", Range(0, 1)) = 0.1

        [Space(10)][Header(Shadow)]
        _ShadowFadeIn ("ShadowFadeIn", Range(1, 100)) = 30
        _ShadowFadeOut ("ShadowFadeOut", Range(1, 100)) = 80
   

    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _SpriteTex_ST;
                float _Width;
                float _Height;
            CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex vert
            #pragma fragment frag

            #include "VortexMethod.cginc"
            #include "ParticleCluster.cginc"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


            PC_DEF_UNIFORM
            PC_DEF_BUFFER(TracerParticle)
            
            sampler2D _SpriteTex;
            float _ShadowFadeIn;
            float _ShadowFadeOut;
            float _ClipThreshold;

            struct V2F
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
                float3 normal : NORMAL;
            };

            V2F vert(uint id : SV_VertexID)
            {
                V2F o;
                float4 centerWP = float4(PC_GET(UNSAFE, id / 6u).pos, 1.0);
                
                float4 dx = UNITY_MATRIX_P[0] * _Width;
                float4 dy = UNITY_MATRIX_P[1] * _Height;

                id %= 6u;
                id = id < 3u ? id : 6u - id;
                int ix = id >> 1u, iy = id & 1u;

                o.worldPos = centerWP + dx * (ix * 2 - 1) + dy * (iy * 2 - 1);
                o.pos = mul(UNITY_MATRIX_VP, centerWP) + dx * (ix * 2 - 1) + dy * (iy * 2 - 1);
                o.uv = float2(ix, iy);
                o.normal = -mul((float3x3)UNITY_MATRIX_V, float3(0, 0, 1));
                o.shadowCoord = TransformWorldToShadowCoord(o.worldPos);
                
                return o;
            }

            float GetFadedShadow(float3 worldPos)
            {
                float4 posVS = mul(GetWorldToViewMatrix(), float4(worldPos, 1));
            #if UNITY_REVERSED_Z
                float vz = -posVS.z;
            #else
                float vz = posVS.z;
            #endif
                float fade = 1 - smoothstep(_ShadowFadeIn, _ShadowFadeOut, vz);
                return fade;
            }

            half4 frag(V2F i) : COLOR
            {
                half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                
                half shadow = MainLightRealtimeShadow(i.shadowCoord);   
                half fadedShadow = GetFadedShadow(i.worldPos);
                shadow = lerp(1, shadow, fadedShadow);
                
                float4 texColor = tex2D(_SpriteTex, i.uv);
                half4 finalColor = _BaseColor * texColor;

                finalColor.rgb = lerp(finalColor.rgb * ambient.rgb, finalColor.rgb, shadow);
                clip(finalColor.a - _ClipThreshold);
                return finalColor;
            }

            ENDHLSL
        }


        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            //#pragma multi_compile DIRECTIONAL SHADOWS_SCREEN

            #include "UnityCG.cginc"
            #include "VortexMethod.cginc"
            #include "ParticleCluster.cginc"
            #include "AutoLight.cginc"


            PC_DEF_UNIFORM
            PC_DEF_BUFFER(TracerParticle)

            float _Width;
            float _Height;

            struct V2F
            {
                float4 uv : TEXCOORD;
                V2F_SHADOW_CASTER;
            };

            V2F vert(uint id : SV_VertexID)
            {

                float4 c = mul(UNITY_MATRIX_VP, float4(PC_GET(UNSAFE, id / 6u).pos, 1.0));

                float4 dx = UNITY_MATRIX_P[0] * _Width;
                float4 dy = UNITY_MATRIX_P[1] * _Height;

                id %= 6u;
                id = id < 3u ? id : 6u - id;
                int ix = id >> 1u, iy = id & 1u;

                V2F o;
                o.pos = c + dx * (ix * 2 - 1) + dy * (iy * 2 - 1);

                return o;
            }

            fixed4 frag(V2F i) : COLOR
            {
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }

    }
    Fallback Off
}
