using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vlm
{
    /// <summary>
    /// All of the math functions that VLM uses.
    /// </summary>
    public class VlmMath
    {
        private static readonly Color DefaultColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Retrieves the current scene's ambient color.
        /// </summary>
        /// <param name="normal">The normal to use if the ambient mode is set to Skybox or Gradiant.</param>
        public static Color GetAmbientColor(Vector3 normal)
        {
            switch (RenderSettings.ambientMode)
            {
                case AmbientMode.Skybox:
                    Color[] skyboxColors = new Color[1];
                    Vector3[] directions = {normal};

                    RenderSettings.ambientProbe.Evaluate(directions, skyboxColors);
                    return skyboxColors[0] * RenderSettings.ambientIntensity;
                case AmbientMode.Trilight:
                    float dot = Vector3.Dot(normal.normalized, Vector3.up);
                    if (dot < 0.0f) return Color.Lerp(RenderSettings.ambientGroundColor, RenderSettings.ambientEquatorColor, dot + 1.0f);
                    return Color.Lerp(RenderSettings.ambientEquatorColor, RenderSettings.ambientSkyColor, dot);
                default:
                    return RenderSettings.ambientLight;
            }
        }

        /// <summary>
        /// Calculates the base color, for use before calculating attenuation.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="lightPosition">The position of the light.</param>
        /// <param name="lightColor">The color of the light.</param>
        /// <param name="lightIntensity">The intensity of the light.</param
        public static Color CalculateLightColor(Vector3 vertex, Vector3 normal, Vector3 lightPosition, Color lightColor, float lightIntensity)
        {
            Vector3 lightDir = (lightPosition - vertex).normalized;
            float diffuseFactor = Vector3.Dot(normal, lightDir);
            Color diffuse = diffuseFactor > 0.0f ? lightColor * lightIntensity * diffuseFactor : Color.black;
            diffuse.a = 1.0f;

            return diffuse;
        }

        /// <summary>
        /// Calculates attenutation.
        /// </summary>
        /// <param name="distanceToLight">The distance to the light.</param>
        /// <param name="lightRange">The range of the light.</param>
        public static float CalculateAttenuation(float distanceToLight, float lightRange)
        {
            float att = Mathf.Clamp(1.0f - distanceToLight * distanceToLight / (lightRange * lightRange), 0.0f, 1.0f);
            return att * att;
        }

        /// <summary>
        /// Calculates the attenuation for a point light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="lightPosition">The position of the light.</param>
        /// <param name="lightRange">The range of the light.</param>
        public static float CalculateIntensityPoint(Vector3 vertex, Vector3 lightPosition, float lightRange)
        {
            float distance = Vector3.Distance(lightPosition, vertex);
            return CalculateAttenuation(distance, lightRange);
        }

        /// <summary>
        /// Calculates the attenuation for a spot light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="lightPosition">The position of the light.</param>
        /// <param name="lightForward">The forward direction of the light.</param>
        /// <param name="lightRange">The range of the light.</param>
        /// <param name="lightConeAngle">The cone angle of the light.</param>
        public static float CalculateIntensitySpot(Vector3 vertex, Vector3 lightPosition, Vector3 lightForward, float lightRange, float lightConeAngle)
        {
            float pointAtten = CalculateIntensityPoint(lightPosition, vertex, lightRange);

            float spotFactor = Vector3.Dot((vertex - lightPosition).normalized, lightForward);
            float cutoff = 1.0f - lightConeAngle / 180.0f;

            if (spotFactor > cutoff) return pointAtten * (1.0f - (1.0f - spotFactor) * 1.0f / (1.0f - cutoff));
            return 0.0f;
        }

        /// <summary>
        /// Calculates the intensity for a directional light.
        /// </summary>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// /// <param name="lightForward">The forward direction of the light.</param>
        public static float CalculateIntensityDirectional(Vector3 normal, Vector3 lightForward)
        {
            return Mathf.Clamp(Vector3.Dot(-lightForward.normalized, normal.normalized), 0.0f, 1.0f);
        }

        /// <summary>
        /// Calculates the color of a vertex using a point light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="targetLight">The reference light.</param>
        public static Color CalculateColorPoint(Vector3 vertex, Vector3 normal, Light targetLight, VlmBakeOptions options = null)
        {
            if (!targetLight) return DefaultColor;
            return CalculateColorPoint(vertex, normal, targetLight.transform.position, targetLight.color, targetLight.intensity, targetLight.range, targetLight.shadows != LightShadows.None, targetLight.shadowStrength, options);
        }

        /// <summary>
        /// Calculates the color of a vertex using a point light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="lightPosition">The position of the light.</param>
        /// <param name="lightColor">The color of the light.</param>
        /// <param name="lightIntensity">The intensity of the light.</param>
        /// <param name="lightRange">The range of the light.</param>
        /// <param name="shadows">Whether the light casts shadows.</param>
        /// <param name="lightShadowStrength">The strength of the lights shadows.</param>
        /// <param name="options">The bake options to use.</param>
        public static Color CalculateColorPoint(Vector3 vertex, Vector3 normal, Vector3 lightPosition, Color lightColor, float lightIntensity, float lightRange, bool shadows, float lightShadowStrength, VlmBakeOptions options = null)
        {
            float atten = CalculateIntensityPoint(vertex, lightPosition, lightRange);

            bool isShadowed = false;
            if (atten > 0.0f && shadows && (options == null || options.RecieveShadows)) isShadowed = TestForShadow(vertex, normal, lightPosition);

            Color color = CalculateLightColor(vertex, normal, lightPosition, lightColor, lightIntensity) * atten;
            return isShadowed ? Color.Lerp(color, DefaultColor, lightShadowStrength) : color;
        }

        /// <summary>
        /// Calculates the color of a vertex using a spot light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="targetLight">The reference light.</param>
        public static Color CalculateColorSpot(Vector3 vertex, Vector3 normal, Light targetLight, VlmBakeOptions options = null)
        {
            if (!targetLight) return DefaultColor;
            return CalculateColorSpot(vertex, normal, targetLight.transform.position, targetLight.transform.forward, targetLight.color, targetLight.intensity, targetLight.range, targetLight.spotAngle, targetLight.shadows != LightShadows.None, targetLight.shadowStrength, options);
        }

        /// <summary>
        /// Calculates the color of a vertex using a spot light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="lightPosition">The position of the light.</param>
        /// <param name="lightForward">The forward direction of the light.</param>
        /// <param name="lightColor">The color of the light.</param>
        /// <param name="lightIntensity">The intensity of the light.</param>
        /// <param name="lightRange">The range of the light.</param>
        /// <param name="lightConeAngle">The cone angle of the spot light.</param>
        /// <param name="shadows">Whether the light casts shadows.</param>
        /// <param name="lightShadowStrength">The strength of the lights shadows.</param>
        /// <param name="options">The bake options to use.</param>
        public static Color CalculateColorSpot(Vector3 vertex, Vector3 normal, Vector3 lightPosition, Vector3 lightForward, Color lightColor, float lightIntensity, float lightRange, float lightConeAngle, bool shadows, float lightShadowStrength, VlmBakeOptions options = null)
        {
            float atten = CalculateIntensitySpot(vertex, lightPosition, lightForward, lightRange, lightConeAngle);

            bool isShadowed = false;
            if (atten > 0.0f && shadows && (options == null || options.RecieveShadows)) isShadowed = TestForShadow(vertex, normal, lightPosition);

            Color color = CalculateLightColor(vertex, normal, lightPosition, lightColor, lightIntensity) * atten;
            return isShadowed ? Color.Lerp(color, DefaultColor, lightShadowStrength) : color;
        }

        /// <summary>
        /// Calculates the color of a vertex using a directional light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="targetLight">The reference light..</param>
        public static Color CalculateColorDirectional(Vector3 vertex, Vector3 normal, Light targetLight, VlmMeshObject self, VlmBakeOptions options = null)
        {
            if (!targetLight) return DefaultColor;
            return CalculateColorDirectional(vertex, normal, targetLight.transform.forward, targetLight.color, targetLight.intensity, targetLight.shadows != LightShadows.None, targetLight.shadowStrength, self, options);
        }

        /// <summary>
        /// Calculates the color of a vertex using a directional light.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="lightForward">The forward direction of the light.</param>
        /// <param name="lightColor">The color of the light.</param>
        /// <param name="lightIntensity">The intensity of the light.</param>
        /// <param name="lightShadowStrength">The strength of the lights shadows.</param>
        /// <param name="shadows">Whether the light casts shadows.</param>
        /// <param name="options">The bake options to use.</param>
        public static Color CalculateColorDirectional(Vector3 vertex, Vector3 normal, Vector3 lightForward, Color lightColor, float lightIntensity, bool shadows, float lightShadowStrength, VlmMeshObject self, VlmBakeOptions options = null)
        {
            bool isShadowed = false;
            if (shadows && (options == null || options.RecieveShadows)) isShadowed = TestForShadowInfiniteDistance(vertex, normal, options != null && options.WorldUpShadows ? -Vector3.up : lightForward);

            Color color = lightColor * lightIntensity * CalculateIntensityDirectional(normal, lightForward);
            color.a = 1.0f;

            return isShadowed ? Color.Lerp(color, DefaultColor, lightShadowStrength) : color;
        }

        /// <summary>
        /// Calculates the intensity of a light sponge.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="sponge">The reference light sponge.</param>
        public static Color CalculateColorSponge(Vector3 vertex, Vector3 normal, LightSponge sponge)
        {
            if (!sponge) return Color.white;

            if (sponge.Shape == LightSponge.SpongeShape.Box) return CalculateColorSpongeCubic(vertex, normal, sponge.transform.position, sponge.transform.rotation, sponge.BoxBounds, sponge.Intensity, sponge.IgnoreReverseNormals);
            return CalculateColorSpongeSpherical(vertex, normal, sponge.transform.position, sponge.SphereRadius, sponge.Intensity, sponge.IgnoreReverseNormals);
        }

        /// <summary>
        /// Calculates the intensity of a spherical light sponge for the given vertex.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="spongePosition">The position of the sponge.</param>
        /// <param name="spongeRadius">The radius of the sponge.</param>
        /// <param name="spongeIntensity">The intensity of the sponge.</param>
        /// <param name="ignoreReverseNormals">Whether normals facing away from the sponge should be ignored or not.</param>
        public static Color CalculateColorSpongeSpherical(Vector3 vertex, Vector3 normal, Vector3 spongePosition, float spongeRadius, float spongeIntensity, bool ignoreReverseNormals)
        {
            float diffuseFactor = ignoreReverseNormals ? Mathf.Clamp(Mathf.Sign(Vector3.Dot(normal, (spongePosition - vertex).normalized)), 0.0f, 1.0f) : 1.0f;
            Color col = Color.Lerp(Color.white, (1.0f - CalculateIntensityPoint(vertex, spongePosition, spongeRadius)) * Color.white, spongeIntensity * diffuseFactor);
            col.a = 1.0f;

            return col;
        }

        /// <summary>
        /// Calculates the intensity of a cubic light sponge for the given vertex.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="spongePosition">The position of the sponge.</param>
        /// <param name="spongeForward">The forward direction of the sponge.</param>
        /// <param name="referenceUp">The up vector reference that the sponge is rotated along.</param>
        /// <param name="spongeBounds">The bounds of the sponge.</param>
        /// <param name="spongeIntensity">The intensity of the sponge.</param>
        /// <param name="ignoreReverseNormals">Whether normals facing away from the sponge should be ignored or not.</param>
        public static Color CalculateColorSpongeCubic(Vector3 vertex, Vector3 normal, Vector3 spongePosition, Vector3 spongeForward, Vector3 referenceUp, Vector3 spongeBounds, float spongeIntensity, bool ignoreReverseNormals)
        {
            return CalculateColorSpongeCubic(vertex, normal, spongePosition, Quaternion.LookRotation(spongeForward, referenceUp), spongeBounds, spongeIntensity, ignoreReverseNormals);
        }

        /// <summary>
        /// Calculates the intensity of a cubic light sponge for the given vertex.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to light.</param>
        /// <param name="normal">The world normal of the vertex to light.</param>
        /// <param name="spongePosition">The position of the sponge.</param>
        /// <param name="spongeRotation">The rotation of the sponge.</param>
        /// <param name="spongeBounds">The bounds of the sponge.</param>
        /// <param name="spongeIntensity">The intensity of the sponge.</param>
        /// <param name="ignoreReverseNormals">Whether normals facing away from the sponge should be ignored or not.</param>
        public static Color CalculateColorSpongeCubic(Vector3 vertex, Vector3 normal, Vector3 spongePosition, Quaternion spongeRotation, Vector3 spongeBounds, float spongeIntensity, bool ignoreReverseNormals)
        {
            Vector3 itp = Quaternion.Inverse(spongeRotation) * (vertex - spongePosition);
            float xAtten = CalculateAttenuation(Mathf.Abs(itp.x), spongeBounds.x);
            float yAtten = CalculateAttenuation(Mathf.Abs(itp.y), spongeBounds.y);
            float zAtten = CalculateAttenuation(Mathf.Abs(itp.z), spongeBounds.z);

            float atten = xAtten * yAtten * zAtten;
            float diffuseFactor = ignoreReverseNormals ? Mathf.Clamp(Mathf.Sign(Vector3.Dot(normal, (spongePosition - vertex).normalized)), 0.0f, 1.0f) : 1.0f;

            Color col = Color.Lerp(Color.white, (1.0f - atten) * Color.white, spongeIntensity * diffuseFactor);
            col.a = 1.0f;

            return col;
        }

        /// <summary>
        /// Tests for shadows.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to check for shadowing against.</param>
        /// <param name="normal">The world normal of the vertex to check for shadowing against.</param>
        /// <param name="lightPosition">The position of the light.</param>
        public static bool TestForShadow(Vector3 vertex, Vector3 normal, Vector3 lightPosition)
        {
            Vector3 pos = vertex + normal * VlmBakeData.ShadowBias;
            RaycastHit[] hits = Physics.RaycastAll(pos, (lightPosition - pos).normalized, Vector3.Distance(vertex, lightPosition), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return CheckShadowHitResults(hits);
        }

        /// <summary>
        /// Tests for a shadow on directional lights.
        /// </summary>
        /// <param name="vertex">The world position of the vertex to test for shadowing of.</param>
        /// <param name="direction">The direction of the light.</param>
        public static bool TestForShadowInfiniteDistance(Vector3 vertex, Vector3 normal, Vector3 direction)
        {
            Vector3 pos = vertex + normal * VlmBakeData.ShadowBias;
            RaycastHit[] hits = Physics.RaycastAll(pos, -direction, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return CheckShadowHitResults(hits);
        }

        /// <summary>
        /// Runs through an array of raycast hits and determines if any of them have a VlmBakeOptions component that has disabled shadow casting.
        /// </summary>
        /// <param name="hits">The array of hits to check.</param>
        public static bool CheckShadowHitResults(RaycastHit[] hits)
        {
            foreach (RaycastHit hit in hits)
            {
                VlmBakeOptionsComponent hitOptions = hit.transform.GetComponent<VlmBakeOptionsComponent>();
                if (!hitOptions || hitOptions && hitOptions.BakeOptions != null && hitOptions.BakeOptions.CastShadows) return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to return a mesh object a raycast hit. Returns null if not found. Call this after creating VLM bake data!
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static VlmMeshObject GetMeshObjectFromRaycastHit(RaycastHit hit)
        {
            if (VlmBakeData.Current == null) return new VlmMeshObject();

            MeshCollider collider = hit.collider as MeshCollider;
            if (!collider) return new VlmMeshObject();

            return VlmBakeData.Current.Meshes.FirstOrDefault(m => m.MeshCollider == collider);
        }

        /// <summary>
        /// Bakes GI for all 
        /// </summary>
        /// <param name="area"></param>
        public static void VoxelGiBake(VlmGiArea area)
        {
            VlmVoxelBaker baker = new VlmVoxelBaker(area);
            baker.Bake();
        }
    }
}
