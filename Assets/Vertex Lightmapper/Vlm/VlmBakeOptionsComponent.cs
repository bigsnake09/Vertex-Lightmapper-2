using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vlm
{
    [AddComponentMenu("Vlm/[Vlm] Bake Options")]
    public class VlmBakeOptionsComponent : MonoBehaviour
    {
        /// <summary>
        /// Whether the lightmapper should ignore this object.
        /// </summary>
        [Tooltip("If enabled then this object will be ignored by the lightmapper.")]
        public bool IgnoreLightmapper = false;

        /// <summary>
        /// The bake options attached to this component.
        /// </summary>
        public VlmBakeOptions BakeOptions;
        
        #if UNITY_EDITOR
        [ContextMenu("Strip AVS")]
        public void StripAvs()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();

            if (!mr.additionalVertexStreams) return;
            
            DestroyImmediate(mr.additionalVertexStreams);
            mr.additionalVertexStreams = null;
        }
        
        [ContextMenu("Check Has AVS")]
        public void CheckHasAvs()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();

            if (mr.additionalVertexStreams) EditorUtility.DisplayDialog(name, $"{name} has Additional Vertex Stream", "OK");
            else EditorUtility.DisplayDialog(name, $"{name} does not have Additional Vertex Stream", "OK");
        }
        #endif
    }
}