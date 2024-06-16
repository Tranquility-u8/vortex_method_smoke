Shader "Custom/Smoke6Way"
{
    Properties 
    {
        [Header(Surface)]
        _LightmapA ("Texture", 3D) = "white" {} // Right + Top + Back + Transparency
        _LightmapB ("Texture", 3D) = "white" {} // Left + Bottom + Front
        _ClipThreshold ("ClipThreshold", Range(0, 1)) = 0.1
        _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        _Radius ("Radius", float) = 0.5

        [Space(10)][Header(Shadow)]
        _ShadowFadeIn ("ShadowFadeIn", Range(1, 100)) = 30
        _ShadowFadeOut ("ShadowFadeOut", Range(1, 100)) = 80
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        // Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "VortexMethod.cginc"
            #include "ParticleCluster.cginc"
            #include "MatCol.cginc"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            PC_DEF_UNIFORM
            PC_DEF_BUFFER(TracerParticle)
            
            float _Radius;
            Texture3D _LightmapA;
            SamplerState sampler_LightmapA;
            Texture3D _LightmapB;
            SamplerState sampler_LightmapB;
            float4 _BaseColor;
            float _ShadowFadeIn;
            float _ShadowFadeOut;
            float _ClipThreshold;

            struct V2F
            {
                float4 c_pos : SV_POSITION; // [Clip] Position
                float3 w_pos : TEXCOORD0;   // [World] Position
                float3 v_pos : TEXCOORD1;   // [View] Position
                float3 v_light : TEXCOORD2; // [View] Light Direction
                float2 uv : TEXCOORD3;      // UV
                float4 s_pos : TEXCOORD4;   // Shadow Position
                float life : TEXCOORD5;
            };

            V2F vert(uint id : SV_VertexID)
            {
                TracerParticle p = PC_GET(UNSAFE, id / 6u);
                float3x3 view3 = (float3x3)(UNITY_MATRIX_V);
                // World
                float3 w_p = p.pos;
                float3 w_dx = FMAT3_ROW(view3, 0) * _Radius;
                float3 w_dy = FMAT3_ROW(view3, 1) * _Radius; // Correct (Col of transpose/inverse(view3))
                
                // View
                float4 v_p4 = mul(UNITY_MATRIX_V, float4(w_p, 1.0));
                float3 v_p = v_p4.xyz / v_p4.w;

                // Clip
                float4 c_p = mul(UNITY_MATRIX_P, v_p4);
                float4 c_dx = FMAT4_COL(UNITY_MATRIX_P, 0) * _Radius;
                float4 c_dy = FMAT4_COL(UNITY_MATRIX_P, 1) * _Radius;

                id %= 6u;
                id = id < 3u ? id : 6u - id;
                int ix = id >> 1u, iy = id & 1u;
                int ix2 = 1 - (ix << 1), iy2 = 1 - (iy << 1);

                V2F o;
                o.c_pos = c_p + c_dx * ix2 + c_dy * iy2;
                o.v_pos = v_p + float3(ix2, iy2, 0);
                o.v_light = mul(view3, normalize(GetMainLight().direction));
                o.w_pos = w_p + w_dx * ix2 + w_dy * iy2;
                o.uv = float2(ix, iy);
                o.s_pos = TransformWorldToShadowCoord(o.w_pos);
                o.life = p.life;
                return o;
            }

            float get_faded_shadow(in const float3 pos_vs)
            {
            #if UNITY_REVERSED_Z
                float vz = -pos_vs.z;
            #else
                float vz = pos_vs.z;
            #endif
                float fade = 1 - smoothstep(_ShadowFadeIn, _ShadowFadeOut, vz);
                return fade;
            }

            
            float4 frag(V2F i) : COLOR
            {
                float3 ambient = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                
                float shadow = MainLightRealtimeShadow(i.s_pos);   
                float faded_shadow = get_faded_shadow(i.v_pos);
                shadow = lerp(1, shadow, faded_shadow);
                
                float4 map_a = _LightmapA.Sample(sampler_LightmapA, float3(i.uv, i.life));
                float3 map_b = _LightmapB.Sample(sampler_LightmapB, float3(i.uv, i.life)).rgb;
                
                float3 light_d = i.v_light;
                float3 light_w = light_d * light_d;
                float light = dot(light_d < 0 ? map_a.xyz : map_b, light_w);
                float4 color = _BaseColor * float4(1, 1, 1, map_a.w);
                color.rgb = lerp(color.rgb * ambient, color.rgb, min(0.8 * shadow + 0.2, light));
                clip(color.a - _ClipThreshold);
                return color;
            }

            ENDHLSL

        }

        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            //#pragma multi_compile DIRECTIONAL SHADOWS_SCREEN

            #include "VortexMethod.cginc"
            #include "ParticleCluster.cginc"
            #include "MatCol.cginc"

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"


            PC_DEF_UNIFORM
            PC_DEF_BUFFER(TracerParticle)

            float _Radius;

            Texture2D _SpriteTex;
            SamplerState sampler_SpriteTex;
            float4 _BaseColor;

            float _ClipThreshold;

            struct V2F
            {
                float2 uv : TEXCOORD;
                V2F_SHADOW_CASTER;
            };

            V2F vert(uint id : SV_VertexID)
            {
                TracerParticle p = PC_GET(UNSAFE, id / 6u);
                // World
                float3 w_p = p.pos;

                // Clip
                float4 c_p = mul(UNITY_MATRIX_VP, float4(w_p, 1.0));
                float4 c_dx = FMAT4_COL(UNITY_MATRIX_P, 0) * _Radius;
                float4 c_dy = FMAT4_COL(UNITY_MATRIX_P, 1) * _Radius;

                id %= 6u;
                id = id < 3u ? id : 6u - id;
                int ix = id >> 1u, iy = id & 1u;
                int ix2 = (ix << 1) - 1, iy2 = (iy << 1) - 1;

                V2F o;
                o.uv = float2(ix, iy);
                o.pos = c_p + c_dx * ix2 + c_dy * iy2;
                /* #if UNITY_REVERSED_Z
                    o.pos -= c_dz;
                #else
                    o.pos += c_dz;
                #endif */

                return o;
            }

            fixed4 frag(V2F i) : COLOR
            {
                float alpha = _SpriteTex.Sample(sampler_SpriteTex, i.uv).a * _BaseColor.a;
                clip(alpha - _ClipThreshold);
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
