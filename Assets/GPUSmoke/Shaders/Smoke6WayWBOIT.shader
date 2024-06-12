Shader "Custom/Smoke6WayWBOIT"
{
    Properties 
    {
        [Header(Surface)]
        _LightmapA ("Texture", 3D) = "white" {} // Right + Top + Back + Transparency
        _LightmapB ("Texture", 3D) = "white" {} // Left + Bottom + Front
        _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        _ClipThreshold ("ClipThreshold", Range(0, 1)) = 0.1
        _Radius ("Radius", float) = 0.5

        [Space(10)][Header(Shadow)]
        _ShadowFadeIn ("ShadowFadeIn", Range(1, 100)) = 30
        _ShadowFadeOut ("ShadowFadeOut", Range(1, 100)) = 80
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector"="True" "RenderType" = "Transparent" "Queue" = "Transparent" }
        // Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull back
        LOD 100

        Pass
        {
			Tags { "LightMode" = "WBOIT" }

			ZWrite Off
			Blend 0 One One
			Blend 1 Zero OneMinusSrcAlpha

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
            float _ClipThreshold;
            Texture3D _LightmapA;
            SamplerState sampler_LightmapA;
            Texture3D _LightmapB;
            SamplerState sampler_LightmapB;
            float4 _BaseColor;
            float _ShadowFadeIn;
            float _ShadowFadeOut;

            struct V2F
            {
                float4 c_pos : SV_POSITION;             // [Clip] Position
                float4 s_pos : TEXCOORD0;               // Shadow Position
                half2 uv : TEXCOORD1;                   // UV
                nointerpolation half v_z : TEXCOORD2;   // [View] Position Z
                nointerpolation half life : TEXCOORD3;
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
                float4 v_p = mul(UNITY_MATRIX_V, float4(w_p, 1.0));

                // Clip
                float4 c_p = mul(UNITY_MATRIX_P, v_p);
                float4 c_dx = FMAT4_COL(UNITY_MATRIX_P, 0) * _Radius;
                float4 c_dy = FMAT4_COL(UNITY_MATRIX_P, 1) * _Radius;

                id %= 6u;
                id = id < 3u ? id : 6u - id;
                int ix = id >> 1u, iy = id & 1u;
                int ix2 = 1 - (ix << 1), iy2 = 1 - (iy << 1);

                V2F o;
                o.c_pos = c_p + c_dx * ix2 + c_dy * iy2;
                o.uv = float2(ix, iy);
                o.s_pos = TransformWorldToShadowCoord(w_p + w_dx * ix2 + w_dy * iy2);
                o.v_z = abs(v_p.z);
                o.life = p.life;
                return o;
            }

            float get_faded_shadow(in const float view_z)
            {
                float fade = 1 - smoothstep(_ShadowFadeIn, _ShadowFadeOut, abs(view_z));
                return fade;
            }

			float get_wboit_weight(in const float view_z, in const float alpha) {
            	return pow(alpha + 0.01, 4.0) +
	                max(1e-2, min(3.0 * 1e3, 100.0 / (1e-5 + pow(view_z / 10.0, 3.0) + pow(view_z / 200.0, 6.0))));
				return alpha * max(0.01, min(3000.0, 0.03 / (1e-5 + pow(abs(view_z) / 200.0, 4.0))));
			}
            
            void frag(V2F i, out float4 accum : SV_Target0, out float4 reveal : SV_Target1)
            {
                half4 map_a = _LightmapA.Sample(sampler_LightmapA, float3(i.uv, i.life));
                half alpha = map_a.w;
                clip(alpha - _ClipThreshold);

                half3 map_b = _LightmapB.Sample(sampler_LightmapB, float3(i.uv, i.life)).rgb;

                half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                
                half shadow = MainLightRealtimeShadow(i.s_pos);   
                half faded_shadow = get_faded_shadow(i.v_z);
                shadow = lerp(1, shadow, faded_shadow);
                
                half3 light_d = mul((float3x3)UNITY_MATRIX_V, normalize(GetMainLight().direction));
                half3 light_w = light_d * light_d;
                half light = dot(light_d < 0 ? map_a.xyz : map_b, light_w);
                half4 color = _BaseColor * half4(1, 1, 1, alpha * min(i.life, 1));
                color.rgb = lerp(color.rgb * ambient, color.rgb, min(0.8 * shadow + 0.2, light));
                
                accum = float4(color.rgb * color.a, color.a) * get_wboit_weight(i.v_z, color.a);
                reveal = float4(color.a, color.a, color.a, color.a);
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
                float alpha = _SpriteTex.Sample(sampler_SpriteTex, i.uv).a;
                clip(alpha - _ClipThreshold);
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
