Shader "Unlit/Smoke"
{
	Properties 
	{
		_SpriteTex ("Texture", 2D) = "white" {}
		_Radius ("Radius", float) = 0.5
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Particle.cginc"
		
			StructuredBuffer<VortexParticle> uParticles;
			uint uMaxParticleCount, uFlip;
			#define CTX_IDX(F, X) F##_IDX(uMaxParticleCount, uFlip, X)
			
			
			float _Radius;
			Texture2D _SpriteTex;
			SamplerState sampler_SpriteTex;

			struct V2G
			{
				float4 pos : SV_POSITION;
			};
			struct G2F
			{
				float4 pos : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			V2G vert(uint id : SV_VertexID)
			{
				V2G o;
				float3 pos = uParticles[CTX_IDX(DST, id)].pos;
				o.pos = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
				
				return o;
			}
			
			
			[maxvertexcount(4)]
			void geom(point V2G p[1], inout TriangleStream<G2F> tri) {
				float4 c = p[0].pos;
				float4 dx = UNITY_MATRIX_P[0] * _Radius;
				float4 dy = UNITY_MATRIX_P[1] * _Radius;
				
				G2F o;

				o.pos = c + dx + dy;
				o.texcoord = float2(0, 0);
				tri.Append(o);

				o.pos = c + dx - dy;
				o.texcoord = float2(0, 1);
				tri.Append(o);

				o.pos = c - dx + dy;
				o.texcoord = float2(1, 0);
				tri.Append(o);

				o.pos = c - dx - dy;
				o.texcoord = float2(1, 1);
				tri.Append(o);
			}

			float4 frag(G2F i) : COLOR
			{
				return _SpriteTex.Sample(sampler_SpriteTex, i.texcoord);
			}

			ENDCG

		}
	}
	Fallback Off
}
