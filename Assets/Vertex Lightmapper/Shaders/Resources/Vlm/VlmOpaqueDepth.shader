Shader "Hidden/Vlm Opaque Depth"
{
	Properties
	{
		_MainTex("Base", 2D) = "white" {}
	}

	Category
	{
		SubShader
		{
			Tags { "RenderType"="Opaque"  "LightMode" = "ShadowCaster" }

			ColorMask 0
			ZWrite On

			Pass
			{

				CGPROGRAM
				#include "UnityCG.cginc"
				#include "Retro.cginc"

				#pragma vertex vert
				#pragma fragment frag

				struct v2f
				{
					float4 position : SV_POSITION;
					float2 depth : TEXCOORD0;
					half2 distance : TEXCOORD1;
				};


				sampler2D _MainTex;
				float4 _MainTex_ST;
				

				v2f vert(appdata_full v)
				{
					v2f o;

					// get vertex position
					float4 normalPosition = mul(UNITY_MATRIX_MV, v.vertex);
					float4 ps1Position = TruncateVertex(v.vertex, _VertexSnapAmt);
					float4 finalPosition = lerp(normalPosition, ps1Position, _VertexSnapping);

					// get distance for clipping
					float dist = GetClippingDistance(v.vertex, 0.3, _VertexSnapping);

					// apply position and distance information
					o.position = mul(UNITY_MATRIX_P, finalPosition);
					o.distance = finalPosition.xy;
					o.distance.x = dist;

					// get depth
					UNITY_TRANSFER_DEPTH(o.depth);
					return o;
				}

				fixed4 frag(v2f i) : SV_TARGET
				{
					clip(i.distance.x < _RetroClippingDistance ? 1 : -1);
					UNITY_OUTPUT_DEPTH(i.depth);
				}

				ENDCG
			}
		}
	}
}
