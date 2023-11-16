using UnityEngine;

namespace Shaders
{
    public class ShaderSettings
    {
        /// <summary>
        /// The default value to use for the clipping distance.
        /// </summary>
        public const float DefaultClippingDistance = 0.0f;
        
        /// <summary>
        /// The default value to for vertex snapping amount.
        /// </summary>
        public const float DefaultVertexSnapAmt = 12;
        
        /// <summary>
        /// The default value to use for the vertex snapping clipping grid size.
        /// </summary>
        public const float DefaultVertexSnapClipGridSize = 0.3f;
        
        /// <summary>
        /// Load the default shader globals on load to make sure that shaders are setup.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnLoad()
        {
            ClippingDistance = ClippingDistance;
            
            VertexSnap = VertexSnap;
            VertexSnapAmt = VertexSnapAmt;
            VertexSnapClipGridSize = VertexSnapClipGridSize;
            
            AffineTextureMapping = AffineTextureMapping;
        }
        
        private static readonly int RetroClippingDistance = Shader.PropertyToID("_RetroClippingDistance");
        private static readonly int VertexSnapping = Shader.PropertyToID("_VertexSnapping");
        private static readonly int VertexSnappingAmt = Shader.PropertyToID("_VertexSnapAmt");
        private static readonly int VertexSnappingClipGridSize = Shader.PropertyToID("_VertexSnapClipAmt");
        private static readonly int AffineTextureMap = Shader.PropertyToID("_AffineTextureMapping");

        /// <summary>
        /// Returns or sets the current clipping distance. This will be applied to shaders supporting the _RetroClippingDistance global.
        /// </summary>
        public static float ClippingDistance
        {
            get => _clippingDistance;
            set
            {
                _clippingDistance = value;
                if (_clippingDistance <= 0.0f)
                {
                    _clippingDistance = 0.0f;
                    Shader.SetGlobalFloat(RetroClippingDistance, Mathf.Infinity);
                } else Shader.SetGlobalFloat(RetroClippingDistance, _clippingDistance);
            }
        }
        private static float _clippingDistance = DefaultClippingDistance;


        /// <summary>
        /// Returns or sets the current vertex snap state. This will be applied to shaders supporting the _VertexSnap global.
        /// </summary>
        public static bool VertexSnap
        {
            get => _vertexSnap;
            set
            {
                _vertexSnap = value;
                Shader.SetGlobalFloat(VertexSnapping, _vertexSnap ? 1.0f : 0.0f);
            }
        }
        private static bool _vertexSnap = false;

        /// <summary>
        /// Returns or sets the current vertex snap resolutioon. This will be applied to shaders supporting the _VertexSnapAmt global.
        /// </summary>
        public static float VertexSnapAmt
        {
            get => _vertexSnapAmt;
            set
            {
                _vertexSnapAmt = value;
                Shader.SetGlobalFloat(VertexSnappingAmt, _vertexSnapAmt);
            }
        }
        private static float _vertexSnapAmt = DefaultVertexSnapAmt;
        
        /// <summary>
        /// Returns or sets the current vertex snap clipping grid size. This will be applied to shaders supporting the _VertexSnapAmt global.
        /// </summary>
        public static float VertexSnapClipGridSize
        {
            get => _vertexSnapClipGridSize;
            set
            {
                _vertexSnapClipGridSize = value;
                Shader.SetGlobalFloat(VertexSnappingClipGridSize, _vertexSnapClipGridSize);
            }
        }
        private static float _vertexSnapClipGridSize = DefaultVertexSnapClipGridSize;
        
        /// <summary>
        /// Returns or sets the current affine texture mapping state. This will be applied to shaders supporting the _AffineTextureMapping global.
        /// </summary>
        public static bool AffineTextureMapping
        {
            get => _affineTextureMapping;
            set
            {
                _affineTextureMapping = value;
                Shader.SetGlobalFloat(AffineTextureMap, _affineTextureMapping ? 1.0f : 0.0f);
            }
        }
        private static bool _affineTextureMapping = false;
        
    }
}