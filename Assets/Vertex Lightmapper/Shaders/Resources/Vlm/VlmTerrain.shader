Shader "Vlm Terrain"
{
    Properties
    {
		_NoisePeriod("Noise Period", Float) = 20
		_NoiseScale("Noise Scale", Float) = 1
		_TextureScale("Texture Scale", Float) = 1
		_TriPlanarHardness("Triplanar Hardness", Float) = 1

        _MainTex("Red Diffuse", 2D) = "white" {}
        _MainTexIllum("Red Illum", 2D) = "black" {}
        _MainTexReflection("Red Reflection", 2D) = "black" {}
		_RedNoiseTintA("Red Noise Tint A", Color) = (1, 1, 1, 1)
		_RedNoiseTintB("Red Noise Tint B", Color) = (1, 1, 1, 1)

		_Texture1("Green Diffuse", 2D) = "white" {}
		_Texture1Illum("Green Illum", 2D) = "black" {}
		_Texture1Reflection("Green Reflection", 2D) = "black" {}
		_GreenNoiseTintA("Green Noise Tint A", Color) = (1, 1, 1, 1)
		_GreenNoiseTintB("Green Noise Tint B", Color) = (1, 1, 1, 1)

		_Texture2("Blue Texture", 2D) = "white" {}
		_Texture2Illum("Blue Illum", 2D) = "black" {}
		_Texture2Reflection("Blue Reflection", 2D) = "black" {}
		_BlueNoiseTintA("Blue Noise Tint A", Color) = (1, 1, 1, 1)
		_BlueNoiseTintB("Blue Noise Tint B", Color) = (1, 1, 1, 1)

		_Texture3("Alpha Diffuse", 2D) = "white" {}
		_Texture3Illum("Alpha Illum", 2D) = "black" {}
		_Texture3Reflection("Alpha Reflection", 2D) = "black" {}
		_AlphaNoiseTintA("Alpha Noise Tint A", Color) = (1, 1, 1, 1)
		_AlphaNoiseTintB("Alpha Noise Tint B", Color) = (1, 1, 1, 1)

		_UvSettings("Uv Settings. X: Min Distance, Y: Max Distance, Y: Min Scale, W: Max Scale", Vector) = (1, 1, 1, 1)
        _Color("Color", Color) = (0.5, 0.5, 0.5, 1)
    	_IllumIntensity("Illumination Intensity", Range(0, 1)) = 1
		_IllumTint("Illumination Tint", Color) = (1, 1, 1, 1)
    	_ReflectionTint("Reflection Tint", Color) = (1, 1, 1, 1)
		[Toggle()] _CanClip("Allow Distance Clip", Float) = 1
		[Toggle(_ALLOW_AFFINE_MAPPING)] _AllowAfineMapping("Allow Afine Mapping", Float) = 1
		[Toggle(_USE_WORLD_NOISE)] _UseWorldNoise("Use World Noise", Float) = 1
		[Toggle(_USE_WORLD_TRIPLANAR)] _UseWorldTriplanar("Use World Triplanar", Float) = 1
	    _FogColor("Fog Color", Color) = (0.0, 0.0, 0.0, 0.0)
		_FogDistance("Fog Distance", Float) = 60
		_AffineBlend("Affine Blend", Range(0, 1)) = 1
    }

	Category
	{
		SubShader
		{
			Pass
			{
				CGPROGRAM

				#pragma multi_compile __ _ALLOW_AFFINE_MAPPING
				#pragma multi_compile __ _USE_WORLD_NOISE
				#pragma multi_compile __ _USE_WORLD_TRIPLANAR
				#include "UnityCG.cginc"
				#include "Retro.cginc"
				#include "ShaderExtras.cginc"
				#include "PerlinNoise3D.hlsl"

				#pragma vertex vert
				#pragma fragment frag

				struct v2f
				{
					float4 position : SV_POSITION;
					float3 normal : TEXCOORD;
					half2 distance : TEXCOORD1;
					float4 blends : TEXCOORD2;
					float4 cubeNoise : TEXCOORD3;
					float3 worldPosition : TEXCOORD4;
					float2 uvY : TEXCOORD5;
					float2 uvX : TEXCOORD6;
					float2 uvZ: TEXCOORD7;
					float4 affineY: TEXCOORD8;
					float4 affineX : TANGENT;
					float4 affineZ : NORMAL;
					fixed4 color : COLOR;
				};

				sampler2D _MainTex;
				sampler2D _MainTexIllum;
				sampler2D _MainTexReflection;
				sampler2D _Texture1;
				sampler2D _Texture1Illum;
				sampler2D _Texture1Reflection;
				sampler2D _Texture2;
				sampler2D _Texture2Illum;
				sampler2D _Texture2Reflection;
				sampler2D _Texture3;
				sampler2D _Texture3Illum;
				sampler2D _Texture3Reflection;
				float4 _MainTex_ST;
				float4 _Color;
				float4 _UvSettings;
				float _AffineBlend;
				float3 pos;

				float _IllumIntensity;
				float4 _IllumTint;
				float4 _ReflectionTint;

				float _NoisePeriod;
				float _NoiseScale;
				float _TextureScale;
				float _TriPlanarHardness;

				float4 _RedNoiseTintA;
				float4 _RedNoiseTintB;
				float4 _GreenNoiseTintA;
				float4 _GreenNoiseTintB;
				float4 _BlueNoiseTintA;
				float4 _BlueNoiseTintB;
				float4 _AlphaNoiseTintA;
				float4 _AlphaNoiseTintB;

				float4 _FogColor;
				float _FogDistance;

				bool _CanClip;

				float4 triPlanarUnwrapTex(sampler2D tex, float3 worldNorm, float2 yUv, float2 xUv, float2 zUv)
				{
					float4 yDiff = tex2D(tex, yUv);
					float4 xDiff = tex2D(tex, xUv);
					float4 zDiff = tex2D(tex, zUv);

					half3 blendWeights = pow(abs(worldNorm), _TriPlanarHardness);
					blendWeights /= blendWeights.x + blendWeights.y + blendWeights.z;
					return xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z;
				}

				float4 getUvBlendedTexture(sampler2D tex, float2 yUv, float2 xUv, float2 zUv, float4 yAffine, float4 xAffine, float4 zAffine, float3 worldNorm, float3 worldPos, float maxUvScale, float uvDist)
				{
					#if defined(_ALLOW_AFFINE_MAPPING)
						yUv = lerp(yUv, yAffine.xy / yAffine.z, yAffine.w * _AffineBlend);
						xUv = lerp(xUv, xAffine.xy / xAffine.z, xAffine.w * _AffineBlend);
						zUv = lerp(zUv, zAffine.xy / zAffine.z, zAffine.w * _AffineBlend);
					#endif

					fixed4 min = triPlanarUnwrapTex(tex, worldNorm, yUv, xUv, zUv);
					fixed4 max = triPlanarUnwrapTex(tex, worldNorm, yUv * maxUvScale, xUv * maxUvScale, zUv * maxUvScale);
					return lerp(min, max, uvDist);
				}

				v2f vert(appdata_full v)
				{
					v2f o;

					// get vertex position
					float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
					float4 normalPosition = mul(UNITY_MATRIX_MV, v.vertex);
					float4 ps1Position = TruncateVertex(v.vertex, _VertexSnapAmt);
					float4 finalPosition = lerp(normalPosition, ps1Position, _VertexSnapping);

					// get distance for clipping
					float dist = GetClippingDistance(v.vertex, 0.3, _VertexSnapping);

					// apply position and distance information
					o.position = mul(UNITY_MATRIX_P, finalPosition);
					#if defined(_USE_WORLD_TRIPLANAR)
						o.worldPosition = worldPosition;
					#else
						o.worldPosition = v.vertex.xyz;
					#endif
					o.distance.x = dist;
					o.distance.y = distance(_WorldSpaceCameraPos, worldPosition);

					/*---Uvs---*/
					o.uvY = o.worldPosition.xz / _TextureScale;
					o.uvX = o.worldPosition.zy / _TextureScale;
					o.uvZ = o.worldPosition.xy / _TextureScale;

					#if defined(_ALLOW_AFFINE_MAPPING)
						o.affineY = CalculateAffineUvs(_AffineTextureMapping, o.uvY, o.position);
						o.affineX = CalculateAffineUvs(_AffineTextureMapping, o.uvX, o.position);
						o.affineZ = CalculateAffineUvs(_AffineTextureMapping, o.uvZ, o.position);
					#endif

					o.color = v.tangent * _Color;
					o.blends = v.color;

					// normals
					half3 worldNormal = UnityObjectToWorldNormal(v.normal);
					float3 worldVert = mul(unity_ObjectToWorld, v.vertex);
					o.cubeNoise.xyz = -reflect(_WorldSpaceCameraPos.xyz - worldVert, normalize(worldNormal));
					
					#if defined(_USE_WORLD_NOISE)
						o.cubeNoise.w = pnoise(worldPosition * _NoiseScale, _NoisePeriod);
					#else
						o.cubeNoise.w = pnoise(v.vertex * _NoiseScale, _NoisePeriod);
					#endif

					#if defined(_USE_WORLD_TRIPLANAR)
						o.normal = worldNormal;
					#else
						o.normal = v.normal;
					#endif	

					float normalDot = dot(worldNormal, float3(0, 1, 0));
					float vertToHoriDot = 1 - abs(normalDot);

					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float uvDistScale = InverseLerp(_UvSettings.x, _UvSettings.y, i.distance.y);
					float uvScale = lerp(_UvSettings.z, _UvSettings.w, uvDistScale);

					if (_CanClip) clip(i.distance.x < _RetroClippingDistance ? 1 : -1);

					/*---Diffuse---*/
					float4 outTex = getUvBlendedTexture(_MainTex, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.x * lerp(_RedNoiseTintA, _RedNoiseTintB, i.cubeNoise.w);
					
					outTex += getUvBlendedTexture(_Texture1, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.y * lerp(_GreenNoiseTintA, _GreenNoiseTintB, i.cubeNoise.w);
					
					outTex += getUvBlendedTexture(_Texture2, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.z * lerp(_BlueNoiseTintA, _BlueNoiseTintB, i.cubeNoise.w);
					
					outTex += getUvBlendedTexture(_Texture3, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.w * lerp(_AlphaNoiseTintA, _AlphaNoiseTintB, i.cubeNoise.w);


					/*---Fog---*/
					const half fogT = clamp(i.distance.y, 0.0f, _FogDistance) / _FogDistance;
					fixed4 finalColor = lerp(outTex * i.color, float4(_FogColor.rgb, 1.0), fogT * _FogColor.a);


					/*---Illum---*/
					finalColor += getUvBlendedTexture(_MainTexIllum, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.x * _IllumIntensity * _IllumTint;

					finalColor += getUvBlendedTexture(_Texture1Illum, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.y * _IllumIntensity * _IllumTint;

					finalColor += getUvBlendedTexture(_Texture2Illum, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.z * _IllumIntensity * _IllumTint;

					finalColor += getUvBlendedTexture(_Texture3Illum, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.w * _IllumIntensity * _IllumTint;

					/*---Reflection---*/
					fixed4 refColor = getUvBlendedTexture(_MainTexReflection, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.x;

					refColor += getUvBlendedTexture(_Texture1Reflection, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.y;

					refColor += getUvBlendedTexture(_Texture2Reflection, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.z;

					refColor += getUvBlendedTexture(_Texture3Reflection, i.uvY, i.uvX, i.uvZ, i.affineY, i.affineX, i.affineZ,
						i.normal, i.worldPosition, _UvSettings.w, uvDistScale) * i.blends.w;

					fixed4 cube = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, i.cubeNoise.xyz);
					cube *= refColor * _ReflectionTint;
					finalColor.rgb += float4(DecodeHDR(cube, unity_SpecCube0_HDR).rgb, 1).rgb;
					
					return finalColor;
				}

				ENDCG
			}
		}
	}
	Fallback "Hidden/Vlm Opaque Depth"
}
