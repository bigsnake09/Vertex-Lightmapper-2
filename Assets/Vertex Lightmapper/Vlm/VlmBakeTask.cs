#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vlm.Editor
{
    public struct VlmBakeTask
    {
        public static VlmBakeData BakeData;
        private static bool _prevQueryBackfaces;

        /// <summary>
        /// Runs the bake task.
        /// </summary>
        public static void Bake()
        {
            EditorUtility.DisplayProgressBar("Vertex Light Mapper", "Gathering Scene Data", 0.0f);

            _prevQueryBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = VlmBakeData.BackfaceShadows;

            try
            {
                BakeData = new VlmBakeData();
                VlmBakeData.Current = BakeData;

                /*---Destroy Previous Lightmap Data---*/
                VlmData[] prevData = Object.FindObjectsOfType<VlmData>();

                for (int i = 0; i < prevData.Length; ++i) Object.DestroyImmediate(prevData[i]);

                /*---Bake GI---*/
                if (VlmBakeData.BounceLight)
                {
                    VlmGiArea[] giAreas = Object.FindObjectsOfType<VlmGiArea>();
                    for (int i = 0; i < giAreas.Length; ++i)
                    {
                        bool cancel = EditorUtility.DisplayCancelableProgressBar("Vertex Lightmapper", $"Calculating GI ({giAreas[i].name} {i + 1} / {giAreas.Length})", (float) (i + 1) / giAreas.Length);
                        if (cancel)
                        {
                            Cleanup(true);
                            return;
                        }

                        VlmMath.VoxelGiBake(giAreas[i]);
                    }
                }

                /*---Bake Scenery Lighting---*/
                int count = BakeData.Meshes.Count;
                for (int i = 0; i < count; ++i)
                {
                    /*---Progress Report---*/
                    VlmMeshObject meshObj = BakeData.Meshes[i];
                    if (!meshObj.Mesh) continue;

                    bool cancel = EditorUtility.DisplayCancelableProgressBar("Vertex Lightmapper", $"Calculating Scenery Lighting ({meshObj.Transform.name} {i + 1} / {count})", (float) (i + 1) / count);
                    if (cancel)
                    {
                        Cleanup(true);
                        return;
                    }

                    Vector3[] verts = meshObj.Mesh.vertices;

                    Color[] colors = meshObj.Colors;
                    int vertLen = verts.Length;
                    for (int j = 0; j < vertLen; ++j)
                    {
                        Vector3 vert = meshObj.WorldVertices[j];
                        Vector3 normal = meshObj.WorldNormals[j];

                        /*---Apply Ambient Light---*/
                        Color newColor = VlmMath.GetAmbientColor(normal);

                        /*---Apply Directional Lights---*/
                        for (int k = 0; k < BakeData.DirectionalLights.Count; ++k) newColor += VlmMath.CalculateColorDirectional(vert, normal, BakeData.DirectionalLights[k], meshObj, meshObj.MeshBakeOptions);
                        newColor += meshObj.GiAdd[j] * VlmBakeData.BounceIntensity;

                        /*---Apply Light Sponges---*/
                        for (int k = 0; k < BakeData.LightSponges.Count; ++k) newColor *= VlmMath.CalculateColorSponge(vert, normal, BakeData.LightSponges[k]);

                        /*---Apply Other Lights---*/
                        for (int k = 0; k < BakeData.Lights.Count; ++k)
                        {
                            Light l = BakeData.Lights[k];

                            if (l.type == LightType.Spot) newColor += VlmMath.CalculateColorSpot(vert, normal, l, meshObj.MeshBakeOptions);
                            else newColor += VlmMath.CalculateColorPoint(vert, normal, l, meshObj.MeshBakeOptions);
                        }

                        colors[j] += newColor;
                        colors[j].a = 1.0f;
                    }

                    /*---Apply Colors---*/
                    meshObj.Colors = colors.ToArray();
                }

                /*---Apply Colors---*/
                count = BakeData.Meshes.Count;
                for (int i = 0; i < count; ++i)
                {
                    VlmMeshObject meshObj = BakeData.Meshes[i];
                    if (!meshObj.Mesh) continue;

                    meshObj.ApplyMeshColors();

                    /*---Setup Colors---*/
                    VlmData newData = BakeData.Meshes[i].Transform.gameObject.AddComponent<VlmData>();
                    newData.BakedColors = meshObj.Colors;
                    newData.EncodeInTangents = meshObj.EncodeInTangents;
                    newData.UseVertexStream = meshObj.UseVertexStream || VlmBakeData.AlwaysUseVertexStreams;
                }

                Cleanup(false);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorUtility.DisplayDialog("Lightmapper Failed", $"VLM has failed with the following error:\n\n{e.ToString()}", "Close");
                Cleanup(true);
            }
        }

        public static void Cleanup(bool removeIds)
        {
            Physics.queriesHitBackfaces = _prevQueryBackfaces;

            if (removeIds)
            {
                VlmData[] prevData = Object.FindObjectsOfType<VlmData>();
                for (int i = 0; i < prevData.Length; ++i) Object.DestroyImmediate(prevData[i]);
            }

            foreach (VlmMeshObject mesh in BakeData.Meshes) mesh.Cleanup();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.ClearProgressBar();

            BakeData = null;
            VlmBakeData.Current = null;
        }
    }
}
#endif