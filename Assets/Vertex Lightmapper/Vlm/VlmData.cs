using UnityEngine;

namespace Vlm
{
    /// <summary>
    /// Holds baked vertex colors that will be re-applied to a mesh when the track is loaded.
    /// </summary>
    [ExecuteInEditMode]
    public class VlmData : MonoBehaviour
    {
        /// <summary>
        /// Takes the provided color array and encodes the data into the provided meshes tangent array.
        /// </summary
        public static void EncodeMeshTangents(Color[] colors, Mesh mesh)
        {
            Vector4[] tangents = new Vector4[colors.Length];
            for (int i = 0; i < colors.Length; ++i)
            {
                tangents[i].x = colors[i].r;
                tangents[i].y = colors[i].g;
                tangents[i].z = colors[i].b;
                tangents[i].w = colors[i].a;
            }
            mesh.tangents = tangents;
        }

        /// <summary>
        /// Takes the provided color array and encodes the data into the provided meshes tangent array.
        /// </summary
        public static void EncodeMeshTangents(Color32[] colors, Mesh mesh)
        {
            Vector4[] tangents = new Vector4[colors.Length];
            for (int i = 0; i < colors.Length; ++i)
            {
                tangents[i].x = (float)colors[i].r / 255;
                tangents[i].y = (float)colors[i].g / 255;
                tangents[i].z = (float)colors[i].b / 255;
                tangents[i].w = (float)colors[i].a / 255;
            }
            mesh.tangents = tangents;
        }
        
        /// <summary>
        /// Returns the mesh that the vertex colors should be applied to.
        /// </summary>
        public static Mesh GetApplyMesh(bool useVertexStream, Mesh mesh, MeshRenderer mr)
        {
            if (mr && useVertexStream)
            {
                if (mr.additionalVertexStreams) DestroyImmediate(mr.additionalVertexStreams);
                Mesh avs = Instantiate(mesh);
                avs.name = $"{mesh.name} (AVS)";
                mr.additionalVertexStreams = avs;

                return avs;
            }

            return mesh;
        }
        
        [HideInInspector]
        public Color[] BakedColors;
        
        [Header("Settings")]
        public bool EncodeInTangents;
        public bool UseVertexStream;
        public bool AutoApplyColors = true;
        
        private void Start()
        {
            if (AutoApplyColors) Apply();   
        }

        /// <summary>
        /// Applies the stored colors to the mesh.
        /// </summary>
        public void Apply()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (!mf)
            {
                Debug.LogWarning($"Couldn't apply vertex lighting to {name} - No Mesh Filter.");
                return;
            }

            Mesh m = mf.sharedMesh;
            if (!m)
            {
                Debug.LogWarning($"Couldn't apply vertex lighting to {name} - No Mesh.");
            }

            Mesh targetMesh = GetApplyMesh(UseVertexStream, m, GetComponent<MeshRenderer>());
            
            ApplyColors(BakedColors, targetMesh);
        }
        
        private void ApplyColors(Color[] colors, Mesh m)
        {
            if (BakedColors.Length == 0)
            {
                Debug.LogWarning($"Couldn't apply vertex lighting to {name} - No Baked Colors.");
                return;
            }
            
            if (EncodeInTangents) EncodeMeshTangents(colors, m);
            else m.colors = colors;
        }
    }
}