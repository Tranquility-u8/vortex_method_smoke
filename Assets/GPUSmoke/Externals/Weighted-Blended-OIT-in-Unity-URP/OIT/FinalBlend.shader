Shader "cdc/FinalBlend"
{
    Properties{
		_MainTex ("Main Tex", 2D) = "white" {}
	}
	SubShader{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }

		Pass {
			CGPROGRAM

            #pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			sampler2D _AccumTex;
			sampler2D _RevealageTex;
			
			struct A2V {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};
			
			struct V2F {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD1;
			};
			
			V2F vert(A2V v) {
				V2F o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				return o;
			}
			
			fixed4 frag(V2F i) : SV_Target {
				fixed4 background = tex2D(_MainTex, i.uv);
				float4 accum = tex2D(_AccumTex, i.uv);
				float r = tex2D(_RevealageTex, i.uv).r;

				fixed4 col = float4(accum.rgb / max(accum.a, 1e-6), r);

				return (1.0 - col.a) * col + col.a * background;
			}
			
			ENDCG
		}
	} 
	FallBack "Transparent/VertexLit"
}
