using System;
using UnityEngine;

namespace Vlm
{
    [Serializable]
    public class VlmBakeOptions
    {
        /// <summary>
        /// Whether this object should cast shadows.
        /// </summary>
        [Tooltip("If enabled then this object can cast shadows onto other objects.")]
        public bool CastShadows = true;

        /// <summary>
        /// Whether this object should recieve shadows.
        /// </summary>
        [Tooltip("If enabled then this object can recieve shadows from other objects that cast shadows.")]
        public bool RecieveShadows = true;

        /// <summary>
        /// Whether shadow baking for this object will reference the world up.
        /// </summary>
        public bool WorldUpShadows = false;

        /// <summary>
        /// Whether the color information should be encoded into the vertex tangents instead of the vertex colors.
        /// </summary>
        public bool EncodeInTangents;

        /// <summary>
        /// Whether this meshes vertex color information should be stored in an additional vertex stream (instancing).
        /// </summary>
        public bool UseVertexStream;
    }
}
