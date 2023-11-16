using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vlm
{
    public struct VlmVoxelBaker
    {
        public VlmVoxelBaker(VlmGiArea area)
        {
            Source = area.LightSource;
            Center = area.transform.position;
            Area = area;

            ResX = area.Resolution.x;
            ResY = area.Resolution.y;
            ResZ = area.Resolution.z;
            Solids = new bool[area.Resolution.x, area.Resolution.y, area.Resolution.z];
            InSunlight = new bool[area.Resolution.x, area.Resolution.y, area.Resolution.z];

            Cones = new List<PropogationCone>();
        }

        public Light Source;
        public Vector3 Center;
        public VlmGiArea Area;

        public int ResX;
        public int ResY;
        public int ResZ;
        public bool[,,] Solids;
        public bool[,,] InSunlight;

        public List<PropogationCone> Cones;

        public void Bake()
        {
            if (!Source)
            {
                Debug.LogError("Gi Area doesn't have light source.");
                return;
            }
            PopulateSolids();
            CreateCones();

            if (Cones.Count > 0) CalculateGi();
        }

        private void PopulateSolids()
        {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Vertex Lightmapper", $"({Area.name}) Populating GI Voxel Grid. This make take a while.", 0);
#endif
            Vector3 sunDir = Source.transform.forward;
            for (int x = 0; x < ResX; ++x)
            {
                for (int y = 0; y < ResY; ++y)
                {
                    for (int z = 0; z < ResZ; ++z)
                    {
                        Vector3 pos = GetVoxelPosition(x, y, z);

                        bool isSolid = Physics.CheckBox(pos, Vector3.one * 0.5f, Quaternion.identity, 1, QueryTriggerInteraction.Ignore);
                        Solids[x, y, z] = isSolid;

                        if (isSolid)
                        {
                            bool isShadowed = VlmMath.TestForShadowInfiniteDistance(pos, Vector3.zero, sunDir);
                            InSunlight[x, y, z] = !isShadowed;
                        }
                    }
                }
            }
        }

        private void CreateCones()
        {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Vertex Lightmapper", $"({Area.name}) Creating GI Bounce Cones. This may take a while.", 0);
#endif
            Vector3 sunDir = Source.transform.forward;

            for (int x = 0; x < ResX; ++x)
            {
                for (int y = 0; y < ResY; ++y)
                {
                    for (int z = 0; z < ResZ; ++z)
                    {
                        if (!InSunlight[x, y, z] && !Solids[x, y, z]) continue;

                        Vector3 pos = GetVoxelPosition(x, y, z);

                        if (Cones.Any(c => Vector3.Distance(c.Pos, pos) < VlmBakeData.BounceConeMinimumDistance)) continue;

                        Vector3 normal = -sunDir;
                        bool hasBounceSurface = false;

                        Collider[] colliders = Physics.OverlapBox(pos, Vector3.one * 0.5f, Quaternion.identity, 1, QueryTriggerInteraction.Ignore);
                        if (colliders.Length > 0)
                        {
                            float shortestDistance = Mathf.Infinity;

                            foreach (Collider collider in colliders)
                            {
                                MeshCollider mc = collider as MeshCollider;
                                if (!mc) continue;

                                VlmMeshObject mObj = VlmBakeData.Current.Meshes.FirstOrDefault(m => m.MeshCollider == collider);
                                if (!mObj.Mesh) continue;

                                for (int i = 0; i < mObj.WorldVertices.Length; ++i)
                                {
                                    Vector3 vert = mObj.WorldVertices[i];

                                    if (vert.x < pos.x - 0.5f || vert.x > pos.x + 0.5f || 
                                        vert.y < pos.y - 0.5f || vert.y > pos.y + 0.5f || 
                                        vert.z < pos.z - 0.5f || vert.z > pos.z + 0.5f) continue;

                                    float dist = (vert - pos).sqrMagnitude;
                                    if (dist < shortestDistance)
                                    {
                                        normal = mObj.WorldNormals[i];
                                        shortestDistance = dist;

                                        hasBounceSurface = true;
                                    }
                                }
                            }
                        }

                        if (hasBounceSurface)
                        {
                            PropogationCone newCone = new PropogationCone
                            {
                                ConeAngle = VlmBakeData.BounceConeAngle,
                                Direction = normal,
                                Pos = pos,
                                Intensity = 255
                            };
                            Cones.Add(newCone);
                        }
                    }
                }
            }
        }

        private void CalculateGi()
        {
            int itr = 0;
            int[] trmTris = new int[6];
            foreach (PropogationCone cone in Cones)
            {
#if UNITY_EDITOR
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Vertex Lightmapper", $"({Area.name}) Calculating GI Cone ({itr + 1}/{Cones.Count})", (float)(itr + 1) / Cones.Count);
                if (cancel)
                {
                    return;
                }
#endif
                VlmBakeData data = VlmBakeData.Current;

                foreach (VlmMeshObject mObj in data.Meshes)
                {
                    for (int v = 0; v < mObj.WorldVertices.Length; ++v)
                    {
                        float bounceIntensity = VlmMath.CalculateIntensitySpot(mObj.WorldVertices[v], cone.Pos, cone.Direction, VlmBakeData.BounceDistance, cone.ConeAngle);
                        Color bounceColor = new Color(bounceIntensity, bounceIntensity, bounceIntensity, 0.0f) * Source.color;

                        Color col = mObj.GiAdd[v];
                        if (col.r * col.g * col.b < bounceColor.r * bounceColor.g * bounceColor.b) mObj.GiAdd[v] = bounceColor;
                    }
                }

                ++itr;
            }
        }

        public Vector3 GetVoxelPosition(int x, int y, int z)
        {
            return new Vector3 (
                Center.x - (ResX * 0.5f) + x,
                Center.y - (ResY * 0.5f) + y,
                Center.z - (ResZ * 0.5f) + z);
        }

        public struct PropogationCone
        {
            public Vector3 Pos;
            public Vector3 Direction;
            public float ConeAngle;
            public byte Intensity;
        }
    }
}
