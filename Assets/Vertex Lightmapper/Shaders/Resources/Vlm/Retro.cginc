#include "UnityCG.cginc"

/*---Retro Settings---*/
float _RetroClippingDistance;
float _AffineTextureMapping;
float _VertexSnapping;
float _VertexSnapAmt;
float _VertexSnapClipAmt;

static float FOV_REF = 75;
static float VERTEX_GRIDSNAP = 2500;

static float AFFINE_MIN_DISTANCE = 4;

float4 CalculateAffineUvs(float globalBlend, float2 uv, float4 vert)
{
	float4 depth = ComputeScreenPos(vert);
	float distT = clamp(depth.w / AFFINE_MIN_DISTANCE, -1, 1);
	float blend = globalBlend * distT;

	return half4(half3(uv * vert.w, vert.w), blend);
}

float4 TruncateVertex(float4 vert, half snapping)
{
	half renderFov = atan(1 / unity_CameraProjection[1].y) * (360 / UNITY_PI);
	half dist = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, vert));

	half stepValue = (snapping / VERTEX_GRIDSNAP) * (renderFov / FOV_REF) * dist;

	float4 clipPos = mul(UNITY_MATRIX_MV, vert);
	clipPos.xyz = floor(clipPos.xyz / stepValue + 0.5) * stepValue;

	return clipPos;
}

float GetClippingDistance(float4 vert, float snapping, float t)
{
	float3 pos = lerp(_WorldSpaceCameraPos, floor(_WorldSpaceCameraPos * snapping) / snapping, t);
	pos.y = mul(unity_ObjectToWorld, vert).y;

	return distance(pos, mul(unity_ObjectToWorld, vert));
}