using UnityEngine;

namespace Vlm
{
    /// <summary>
    /// Holds data about a mesh that will be baked.
    /// </summary>
    public struct VlmMeshObject
    {
        public VlmMeshObject(GameObject go, VlmBakeOptionsComponent bakeOptions = null)
        {
            MeshCollider = go.GetComponent<MeshCollider>();

            if (!MeshCollider)
            {
                MeshCollider = go.AddComponent<MeshCollider>();
                MeshCollider.hideFlags = HideFlags.HideAndDontSave;
                _attachedCollider = true;
            }
            else _attachedCollider = false;

            MeshBakeOptions = bakeOptions ? bakeOptions.BakeOptions : null;
            EncodeInTangents = bakeOptions && MeshBakeOptions.EncodeInTangents;
            UseVertexStream = bakeOptions && MeshBakeOptions.UseVertexStream;
            Transform = go.transform;

            MeshFilter mf = go.GetComponent<MeshFilter>();
            Mesh = mf ? mf.sharedMesh : null;

            if (Mesh)
            {
                Vertices = Mesh.vertices;
                Triangles = Mesh.triangles;
                Normals = Mesh.normals;
                Uvs = Mesh.uv;

                /*---Calculate World Vertices/Normals---*/
                WorldVertices = new Vector3[Vertices.Length];
                for (int i = 0; i < WorldVertices.Length; ++i) WorldVertices[i] = Transform.TransformPoint(Vertices[i]);

                WorldNormals = new Vector3[Normals.Length];
                for (int i = 0; i < WorldNormals.Length; ++i) WorldNormals[i] = Transform.TransformVector(Normals[i]).normalized;

                /*---Reset Mesh Colors---*/
                Colors = new Color[Mesh.vertices.Length];

                GiAdd = new Color[Vertices.Length];
                ApplyMeshColors();
            }
            else
            {
                Vertices = new Vector3[0];
                WorldVertices = new Vector3[0];
                Triangles = new int[0];
                Normals = new Vector3[0];
                WorldNormals = new Vector3[0];
                Uvs = new Vector2[0];
                Colors = new Color[0];
                GiAdd = new Color[0];
            }
        }

        /// <summary>
        /// The mesh collider attached to this object for baking.
        /// </summary>
        public MeshCollider MeshCollider;

        /// <summary>
        /// The bake options component attached to this object for baking.
        /// </summary>
        public VlmBakeOptions MeshBakeOptions;

        /// <summary>
        /// The transform of this object.
        /// </summary>
        public Transform Transform;

        /// <summary>
        /// The mesh of this object.
        /// </summary>
        public Mesh Mesh;

        /// <summary>
        /// The vertices for this object.
        /// </summary>
        public Vector3[] Vertices;

        /// <summary>
        /// The world vertices for this object.
        /// </summary>
        public Vector3[] WorldVertices;

        /// <summary>
        /// The triangles for this object.
        /// </summary>
        public int[] Triangles;

        /// <summary>
        /// The normals for this object.
        /// </summary>
        public Vector3[] Normals;

        /// <summary>
        /// The world normals for this object.
        /// </summary>
        public Vector3[] WorldNormals;

        /// <summary>
        /// The uvs for this object.
        /// </summary>
        public Vector2[] Uvs;

        /// <summary>
        /// The colors for this object.
        /// </summary>
        public Color[] Colors;

        /// <summary>
        /// The global illumination map for this object.
        /// </summary>
        public Color[] GiAdd;

        /// <summary>
        /// Whether the lighting information should be encoded in the meshes tangents instead of its color array.
        /// </summary>
        public bool EncodeInTangents;
        
        /// <summary>
        /// Whether this meshes vertex color information should be stored in an additional vertex stream (instancing).
        /// </summary>
        public bool UseVertexStream;

        /// <summary>
        /// Whether a mesh collider was added to this object.
        /// </summary>
        private readonly bool _attachedCollider;

        /// <summary>
        /// Applies the current colors to the target mesh.
        /// </summary>
        public void ApplyMeshColors()
        {
            Mesh m = VlmData.GetApplyMesh(UseVertexStream, Mesh, Transform.gameObject.GetComponent<MeshRenderer>());
            
            if (EncodeInTangents) ColorsToMeshTangent(m);
            else m.colors = Colors;
        }

        /// <summary>
        /// Encodes the current colors into the target mesh tangents.
        /// </summary>
        private void ColorsToMeshTangent(Mesh m)
        {
            VlmData.EncodeMeshTangents(Colors, m);
        }

        /// <summary>
        /// Removes the mesh collider from this object is a new one was created.
        /// </summary>
        public void Cleanup()
        {
            if (MeshCollider && _attachedCollider) Object.DestroyImmediate(MeshCollider);
        }
    }
}