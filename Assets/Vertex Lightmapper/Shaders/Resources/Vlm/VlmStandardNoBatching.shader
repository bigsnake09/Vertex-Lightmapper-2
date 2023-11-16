Shader "Standard (Vlm - No Batching)"
{
	Properties
	{
		/*---Textures---*/
		_MainTex("Diffuse Map", 2D) = "white" {}
		_Illum("Illumination Map" , 2D) = "black" {}
		_ReflectionCube("Reflection Cube Map", CUBE) = "" {}
		_ReflectionColor("Reflection Mask Map", 2D) = "" {}
		_AlphaClip("Alpha Clip", Range(0, 1)) = 0.5
		_AffineBlend("Affine Blend", Range(0, 1)) = 1

		/*---Colors---*/
		_Color("Diffuse Tint", Color) = (1, 1, 1, 1)
		_ReflectionTint("Reflection Tint", Color) = (1, 1, 1, 1)
		_IllumIntensity("Illumination Intensity", Range(0, 1)) = 1
		_IllumTint("Illumination Tint", Color) = (1, 1, 1, 1)
		_FadeDistanceMin("Fade To Color Distance Min", Float) = 50
		_FadeDistanceMax("Fade To Color Distance Max", Float) = 100
		_FadeColor("Fade Color", Color ) = (1, 1, 1, 1)
		_FadeOrigin("Fade Origin (Vertex <> Object)", Range(0, 1)) = 0

		/*---Animation---*/
		_UvScroll("Uv Scroll", Vector) = (0, 0, 0, 0) // x scroll, y scroll, null, null
		_UvJump("Uv Jump", Vector) = (0, 0, 0, 0) // x jump, y jump, x time, y time

		/*----Rendering---*/
		[HideInInspector] _SrcBlend ("__src", Int) = 1.0
		[HideInInspector] _DstBlend ("__dst", Int) = 0.0
		[HideInInspector] _RenderQueue ("__queue", Int) = 0.0
		[HideInInspector] _CustomRenderQueue ("__customRenderQueue", Int) = 0.0
		[HideInInspector] _Cull ("__cull", Int) = 0.0
		[HideInInspector] _ZWrite ("__zwrite", Int) = 0.0
		[HideInInspector] _ZTest("__ztest", Int) = 0.0
		[HideInInspector] _IgnoreProjector("__ignoreProjector", Int) = 0.0
		[HideInInspector] _RenderMode("__renderMode", Int) = 0.0
		[HideInInspector] _Lighting("__lighting", Float) = 1
		[HideInInspector] _DistanceClipping("__distanceClipping", Int) = 1
		[HideInInspector] _VertexWobble("__vertexWobble", Int) = 1
		[HideInInspector] _ScreenSpaceUvs("__screenSpaceUvs", Int) = 0
		[HideInInspector] _FadeToColor("__fadeToColor", Int) = 0
	}

	SubShader
	{
		Tags {"DisableBatching"="True"}
		Pass
		{
			Name "Main"

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Lighting Off
			Cull [_Cull]

			CGPROGRAM
				
				/*---Preprocessor Directives---*/
				#pragma multi_compile __ _RENDER_OPAQUE _RENDER_CUTOUT

				#pragma multi_compile __ _REFLECTION_NONE _REFLECTION_CUBE _REFLECTION_PROBE
				#pragma multi_compile __ _REFLECTION_WORLDSPACE
				#pragma multi_compile __ _REFLECTION_FROM_DIFFUSE_ALPHA

				#pragma multi_compile __ _ALLOW_AFFINE_MAPPING

				#pragma multi_compile __ _ANIMATION_UV_NONE _ANIMATION_UV_SCROLL _ANIMATION_UV_JUMP

				#pragma vertex vert
				#pragma fragment frag

				/*---Includes---*/
				#include "UnityCG.cginc"
				#include "Retro.cginc"
				#include "ShaderExtras.cginc"

				float4 _Color;

				#if defined(_RENDER_CUTOUT)
					float _AlphaClip;
				#endif

				sampler2D _MainTex;

				sampler2D _Illum;
				float _IllumIntensity;
				float4 _IllumTint;

				#if defined(_REFLECTION_CUBE)
					samplerCUBE _ReflectionCube;
				#endif

				#if (defined(_REFLECTION_CUBE) || defined(_REFLECTION_PROBE))
					sampler2D _ReflectionColor;
					float4 _ReflectionTint;
				#endif

				#if defined(_ANIMATION_UV_SCROLL)
					float2 _UvScroll;
				#endif

				#if defined(_ANIMATION_UV_JUMP)
					float4 _UvJump;
				#endif

				half _Lighting;
				half _DistanceClipping;
				half _VertexWobble;
				half _AffineBlend;
				int _ScreenSpaceUvs;
				int _FadeToColor;
				float _FadeDistanceMin;
				float _FadeDistanceMax;
		        float4 _FadeColor;
				float _FadeOrigin;
				float4 _MainTex_ST;

				struct v2f
				{
					float4 position: SV_POSITION;
					half2 texCoord : TEXCOORD;

					#if defined(_ALLOW_AFFINE_MAPPING)
						half4 affineTexCoord : TEXCOORD1;
					#endif

					half3 distance : TEXCOORD2;

					#if (defined(_REFLECTION_CUBE) || defined(_REFLECTION_PROBE))
						float3 cubeNormal : TEXCOORD3;
					#endif
					half4 screenPos : TEXCOORD4;

					fixed4 vertexColor : COLOR;
				};

				v2f vert(appdata_full v)
				{
					v2f o;

					/*---Vertex Position---*/
					float4 vert = mul(UNITY_MATRIX_MV, v.vertex);

					float4 ps1Vert = TruncateVertex(v.vertex, _VertexSnapAmt);
					vert = lerp(vert, ps1Vert, _VertexSnapping * _VertexWobble);

					float4 outVert = mul(UNITY_MATRIX_P, vert);
					o.position = outVert;

					/*---Distance To Camera---*/
					o.distance.x = GetClippingDistance(v.vertex, _VertexSnapClipAmt, _VertexSnapping);
					o.distance.y = length(ObjSpaceViewDir(v.vertex));
					o.distance.z = length(ObjSpaceViewDir(float4(0, 0, 0, 1)));

					/*---Cube Normal---*/
					#if defined(_REFLECTION_CUBE) || defined(_REFLECTION_PROBE)
						#if defined(_REFLECTION_WORLDSPACE)
							float3 worldViewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex)));
							float3 worldNormal = UnityObjectToWorldNormal(v.normal);

							float4x4 modelMatrix = unity_ObjectToWorld;
							o.cubeNormal = mul(modelMatrix, v.vertex).xyz - _WorldSpaceCameraPos;
						#else
							float3 normal = mul(unity_ObjectToWorld, v.normal);
							float3 worldVert = mul(unity_ObjectToWorld, v.vertex);
							o.cubeNormal = -reflect(_WorldSpaceCameraPos.xyz - worldVert, normalize(normal));
						#endif	
					#endif

					/*---Uvs---*/
					half2 uv = TRANSFORM_TEX(v.texcoord, _MainTex);

					#if defined(_ALLOW_AFFINE_MAPPING)
						o.texCoord = uv;
						o.affineTexCoord = CalculateAffineUvs(_AffineTextureMapping, uv, outVert);
					#else
						o.texCoord = uv;
					#endif

					o.screenPos = ComputeScreenPos(o.position);

					/*---Color---*/
					o.vertexColor = lerp(half4(1, 1, 1, 1), v.color, _Lighting);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					clip(i.distance.x < _RetroClippingDistance || _DistanceClipping < 1 ? 1 : -1);

					/*--Uv---*/
					float2 uv;

					#if defined(_ALLOW_AFFINE_MAPPING)
						uv = lerp(i.texCoord, i.affineTexCoord.xy / i.affineTexCoord.z, i.affineTexCoord.w * _AffineBlend);
					#else
						uv = i.texCoord.xy;
					#endif

					half2 screenCoords = (i.screenPos.xy / i.screenPos.w);
					uv = lerp(uv, screenCoords, _ScreenSpaceUvs);

					#if defined(_ANIMATION_UV_SCROLL)
						uv += _Time * _UvScroll.xy;
					#endif

					#if defined(_ANIMATION_UV_JUMP)
						if (abs(_UvJump.x) > 0 && _UvJump.z > 0) uv.x += round((fmod(_Time.y, (_UvJump.z / _UvJump.x)) / (_UvJump.z / _UvJump.x)) / _UvJump.x) * _UvJump.x;
						if (abs(_UvJump.y) > 0 && _UvJump.w > 0) uv.y += round((fmod(_Time.y, (_UvJump.w / _UvJump.y)) / (_UvJump.w / _UvJump.y)) / _UvJump.w) * _UvJump.y;
					#endif

					/*---Diffuse---*/
					float4 outputColor;

					fixed4 diffuseCol = tex2D(_MainTex, uv);
					outputColor = diffuseCol * _Color * i.vertexColor;

					#if defined (_RENDER_CUTOUT)
						clip(diffuseCol.a - _AlphaClip);
					#endif

					outputColor.rgb += tex2D(_Illum, uv).rgb * _IllumIntensity * _IllumTint;

					/*---Reflection Cube---*/
					#if defined(_REFLECTION_CUBE)
						fixed4 reflectionColor;

						#if defined(_REFLECTION_FROM_DIFFUSE_ALPHA)
							reflectionColor = diffuseCol.a;
						#else
							reflectionColor = tex2D(_ReflectionColor, uv);
						#endif

						fixed4 cube = texCUBE(_ReflectionCube, i.cubeNormal);
						cube *= reflectionColor * _ReflectionTint;
						outputColor += cube;
					#endif

					/*---Reflection Probe---*/
					#if defined(_REFLECTION_PROBE)
						fixed4 reflectionColor;

						#if defined(_REFLECTION_FROM_DIFFUSE_ALPHA)
							reflectionColor = diffuseCol.a;
						#else
							reflectionColor = tex2D(_ReflectionColor, uv);
						#endif

						fixed4 cube = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, i.cubeNormal);
						cube *= reflectionColor * _ReflectionTint;
						outputColor.rgb += float4(DecodeHDR(cube, unity_SpecCube0_HDR).rgb, 1).rgb;
					#endif

					/*---Fade To Color---*/
					float distNormVert = InverseLerp(_FadeDistanceMin, _FadeDistanceMax, clamp(i.distance.y, 0, _FadeDistanceMax));
					float distNormInvertedVert = 1 - InverseLerp(_FadeDistanceMax, _FadeDistanceMin, clamp(i.distance.y, 0, _FadeDistanceMin));
					float distSignVert = step(_FadeDistanceMin, _FadeDistanceMax);
					distNormVert = lerp(distNormInvertedVert, distNormVert, distSignVert);

					float distNormObj = InverseLerp(_FadeDistanceMin, _FadeDistanceMax, clamp(i.distance.z, 0, _FadeDistanceMax));
					float distNormInvertedObj = 1 - InverseLerp(_FadeDistanceMax, _FadeDistanceMin, clamp(i.distance.z, 0, _FadeDistanceMin));
					float distSignObj = step(_FadeDistanceMin, _FadeDistanceMax);
					distNormObj = lerp(distNormInvertedObj, distNormObj, distSignObj);

					outputColor = lerp(outputColor, _FadeColor, lerp(distNormVert, distNormObj, _FadeOrigin) * _FadeToColor);

					/*---Return final color---*/
					return outputColor;
				}

			ENDCG
		}

		Pass
		{
			Name "Depth"
			Tags { "LightMode" = "ShadowCaster" }

			ColorMask 0
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Lighting Off
			Cull [_Cull]

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile __ _RENDER_OPAQUE _RENDER_CUTOUT

				#pragma multi_compile __ _ALLOW_DISTANCE_CLIP
				#pragma multi_compile __ _ALLOW_VERTEX_WOBBLE
				#pragma multi_compile __ _ALLOW_AFFINE_MAPPING

				#pragma multi_compile __ _ANIMATION_UV_NONE _ANIMATION_UV_SCROLL _ANIMATION_UV_JUMP

				/*---Includes---*/
				#include "UnityCG.cginc"
				#include "Retro.cginc"
				#include "ShaderExtras.cginc"

				#if defined(_RENDER_CUTOUT)
					float _AlphaClip;
				#endif

				#if defined(_RENDER_CUTOUT)
					sampler2D _MainTex;
				#endif

				#if defined(_ANIMATION_UV_SCROLL)
					float2 _UvScroll;
				#endif

				#if defined(_ANIMATION_UV_JUMP)
					float4 _UvJump;
				#endif

				half _DistanceClipping;
				half _VertexWobble;
				float4 _MainTex_ST;

				struct v2f
				{
					float4 position: SV_POSITION;

					#if (defined(_RENDER_CUTOUT))
						half2 texCoord : TEXCOORD;

						#if defined(_ALLOW_AFFINE_MAPPING)
							half4 affineTexCoord : TEXCOORD1;
						#endif
					#endif

					half distance : TEXCOORD2;
				};

				v2f vert(appdata_full v)
				{
					v2f o;

					/*---Vertex Position---*/
					float4 vert = mul(UNITY_MATRIX_MV, v.vertex);

					float4 ps1Vert = TruncateVertex(v.vertex, _VertexSnapAmt);
					vert = lerp(vert, ps1Vert, _VertexSnapping * _VertexWobble);

					float4 outVert = mul(UNITY_MATRIX_P, vert);
					o.position = outVert;

					/*---Distance To Camera---*/
					o.distance = GetClippingDistance(v.vertex, _VertexSnapClipAmt, _VertexSnapping);

					/*---Uvs---*/
					#if defined(_RENDER_CUTOUT)
						half2 uv = TRANSFORM_TEX(v.texcoord, _MainTex);

						#if defined(_ALLOW_AFFINE_MAPPING)
							o.texCoord = uv;
							o.affineTexCoord = CalculateAffineUvs(_AffineTextureMapping, uv, outVert);
						#else
							o.texCoord = uv;
						#endif
					#endif

					#if (defined(_RENDER_OPAQUE) || defined(_RENDER_CUTOUT))
						UNITY_TRANSFER_DEPTH(o.depth);
					#endif
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					clip(i.distance < _RetroClippingDistance || _DistanceClipping < 1 ? 1 : -1);

					#if defined(_RENDER_CUTOUT)
						float2 uv;

						#if defined(_ALLOW_AFFINE_MAPPING)
							uv = lerp(i.texCoord, i.affineTexCoord.xy / i.affineTexCoord.z, i.affineTexCoord.w);
						#else
							uv = i.texCoord.xy;
						#endif

						#if defined(_ANIMATION_UV_SCROLL)
							uv += _Time * _UvScroll.xy;
						#endif

						#if defined(_ANIMATION_UV_JUMP)
							if (abs(_UvJump.x > 0) && _UvJump.z > 0) uv.x += round((fmod(_Time.y, (_UvJump.z / _UvJump.x)) / (_UvJump.z / _UvJump.x)) / _UvJump.x) * _UvJump.x;
							if (abs(_UvJump.y > 0) && _UvJump.w > 0) uv.y += round((fmod(_Time.y, (_UvJump.w / _UvJump.y)) / (_UvJump.w / _UvJump.y)) / _UvJump.w) * _UvJump.y;
						#endif

						fixed4 diffuseCol = tex2D(_MainTex, uv);
						clip(diffuseCol.a - _AlphaClip);
					#endif

					#if (defined(_RENDER_OPAQUE) || defined(_RENDER_CUTOUT))
						UNITY_OUTPUT_DEPTH(i.depth);
					#else
						clip(-1);
						return float4(0, 0, 0, 0);
					#endif
				}

			ENDCG
		}
	}

	CustomEditor "Shaders.Editor.UberShaderEditor"
}