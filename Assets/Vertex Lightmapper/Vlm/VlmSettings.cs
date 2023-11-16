using UnityEngine;
using UnityEngine.Serialization;

namespace Vlm
{
    public class VlmSettings : MonoBehaviour
    {
        /// <summary>
        /// Applies the settings to the static bake data fields.
        /// </summary>
        public void Apply()
        {
            VlmBakeData.BounceLight = BounceLight;
            VlmBakeData.BounceDistance = BounceDistance;
            VlmBakeData.BounceIntensity = BounceIntensity;
            VlmBakeData.BounceConeAngle = BounceConeAngle;
            VlmBakeData.BounceConeMinimumDistance = BounceConeMinimumDistance;
            VlmBakeData.ShadowBias = ShadowBias;
            VlmBakeData.BackfaceShadows = BackfaceShadows;
            VlmBakeData.AutoApply = AutoApply;
            VlmBakeData.AlwaysUseVertexStreams = AlwaysUseVertexStreams;
        }

        /// <summary>
        /// Copies the settings from the static bake data fields.
        /// </summary>
        public void Copy()
        {
            BounceLight = VlmBakeData.BounceLight;
            BounceDistance = VlmBakeData.BounceDistance;
            BounceIntensity = VlmBakeData.BounceIntensity;
            BounceConeAngle = VlmBakeData.BounceConeAngle;
            BounceConeMinimumDistance = VlmBakeData.BounceConeMinimumDistance;
            ShadowBias = VlmBakeData.ShadowBias;
            BackfaceShadows = VlmBakeData.BackfaceShadows;
            AutoApply = VlmBakeData.AutoApply;
            AlwaysUseVertexStreams = VlmBakeData.AlwaysUseVertexStreams;
        }

        /// <summary>
        /// Whether a light bounce pass should be performed.
        /// </summary>
        public bool BounceLight;

        /// <summary>
        /// How far light bounces should go.
        /// </summary>
        public float BounceDistance = 100.0f;

        /// <summary>
        /// How intense indirect lighting should be.
        /// </summary>
        public float BounceIntensity = 0.3f;

        /// <summary>
        /// The maximum angle that light will bounce at.
        /// </summary>
        public float BounceConeAngle = 120.0f;

        /// <summary>
        /// How far apart each bounce cone will be.
        /// </summary>
        public float BounceConeMinimumDistance = 30.0f;

        /// <summary>
        /// How far away from a vertex to check for shadows using.
        /// </summary>
        public float ShadowBias = 0.05f;

        /// <summary>
        /// Whether backfaces should be raycasts against.
        /// </summary>
        public bool BackfaceShadows = true;

        /// <summary>
        /// Whether a legacy bake will be used.
        /// </summary>
        public bool AutoApply = true;

        /// <summary>
        /// Whether vertex streams will always be enabled for baked objects.
        /// </summary>
        public bool AlwaysUseVertexStreams = false;
    }
}
