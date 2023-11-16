using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Vlm
{
    /// <summary>
    /// A sponge that absorbs ambient and directional light.
    /// </summary>
    [AddComponentMenu("Vlm/[Vlm] Light Sponge")]
    [ExecuteInEditMode]
    public class LightSponge : MonoBehaviour
    {
        /// <summary>
        /// The shape of this sponge.
        /// </summary>
        public SpongeShape Shape = SpongeShape.Sphere;

        /// <summary>
        /// How much light this sponge will absorb. (0 - 1).
        /// </summary>
        public float Intensity = 1.0f;

        /// <summary>
        /// The radius of the sponge when in sphere mode.
        /// </summary>
        public float SphereRadius = 10.0f;

        /// <summary>
        /// If true then baking this sponge will ignore normals facing away from the sponge.
        /// </summary>
        public bool IgnoreReverseNormals;

        /// <summary>
        /// The extends of the sponge when in box mode.
        /// </summary>
        public Vector3 BoxBounds = Vector3.one * 10.0f;

        public enum SpongeShape
        {
            Sphere,
            Box,
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/Light/[Vlm] Light Sponge")]
        public static void CreateNewLightSponge()
        {
            SceneView view = SceneView.currentDrawingSceneView;
            if (!view) view = SceneView.lastActiveSceneView;
            if (view)
            {
                Transform cameraT = view.camera.transform;

                GameObject newSponge = new GameObject("Light Sponge");
                newSponge.transform.position = cameraT.position + cameraT.forward * 10.0f;

                LightSponge ls = newSponge.AddComponent<LightSponge>();

                Selection.activeObject = newSponge;
            }
        }

        private void OnEnable()
        {
            SetIcon();
        }

        private void SetIcon()
        {
            string filePath = "Vlm_LightSponge";
            Texture2D icon = Resources.Load<Texture2D>(filePath);
            if (!icon)
            {
                Debug.LogError($"Couldn't find the light sponge icon texture in the resources folder! ({filePath})");
                return;
            }

            MethodInfo iconMethod = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            iconMethod?.Invoke(null, new[] { (object)gameObject, icon });
        }

        #region Custom Editor

        [CustomEditor(typeof(LightSponge)), CanEditMultipleObjects]
        public class AdvancedLightSpongeEditor : UnityEditor.Editor
        {
            private readonly Color _handlesColor = new Color(0.9882353F, 0.9843137F, 0.5294118F);
            private BoxBoundsHandle _boundsHandle;

            public override void OnInspectorGUI()
            {
                LightSponge sponge = (LightSponge) targets[0];

                GUILayout.Space(10);

                /*---Intensity---*/
                float prevIntensity = sponge.Intensity;
                float newIntensity = EditorGUILayout.Slider("Intensity", prevIntensity, 0.0f, 1.0f);
                if (!Mathf.Approximately(prevIntensity, newIntensity))
                    foreach (Object t in CreateUndoPoint("Updated Intensity")) ((LightSponge) t).Intensity = newIntensity;

                /*---Ignore Reverse Normals---*/
                bool prevIgnoreReverseNormals = sponge.IgnoreReverseNormals;
                bool newIgnoreReverseNormals = EditorGUILayout.Toggle("Ignore Reverse Normals", prevIgnoreReverseNormals);
                if (prevIgnoreReverseNormals != newIgnoreReverseNormals)
                    foreach (Object t in CreateUndoPoint("Updated Ignore Reverse Normal")) ((LightSponge) t).IgnoreReverseNormals = newIgnoreReverseNormals;

                GUILayout.Space(10);

                /*---Shape---*/
                SpongeShape prevShape = sponge.Shape;
                SpongeShape newShape = (SpongeShape) EditorGUILayout.EnumPopup("Shape", prevShape);
                if (prevShape != newShape)
                    foreach (Object t in CreateUndoPoint("Updated Shape")) ((LightSponge) t).Shape = newShape;

                ++EditorGUI.indentLevel;
                if (sponge.Shape == SpongeShape.Box)
                {
                    Vector3 prevBoxBounds = sponge.BoxBounds;
                    Vector3 newBoxBounds = EditorGUILayout.Vector3Field("Box Bounds", sponge.BoxBounds);
                    if (prevBoxBounds != newBoxBounds)
                        foreach (Object t in CreateUndoPoint("Box Bounds")) ((LightSponge) t).BoxBounds = newBoxBounds;
                }
                else
                {
                    float prevRadius = sponge.SphereRadius;
                    float newRadius = EditorGUILayout.FloatField("Sphere Radius", sponge.SphereRadius);
                    if (!Mathf.Approximately(prevRadius, newRadius))
                        foreach (Object t in CreateUndoPoint("Sphere Radius")) ((LightSponge) t).SphereRadius = newRadius;
                }
                --EditorGUI.indentLevel;
            }

            private Object[] CreateUndoPoint(string undoName)
            {
                Object[] sponges = targets;
                Undo.RecordObjects(sponges, undoName);

                return sponges;
            }

            private void OnSceneGUI()
            {
                LightSponge sponge = (LightSponge)target;

                if (sponge.Shape == SpongeShape.Box) DoBoxHandles(sponge);
                else DoSphereHandles(sponge);
            }

            private void DoBoxHandles(LightSponge sponge)
            {
                Matrix4x4 prevMatrix = Handles.matrix;
                Handles.matrix = sponge.transform.localToWorldMatrix;

                /*---Box Handle---*/
                Handles.color = _handlesColor;
                if (_boundsHandle == null) _boundsHandle = new BoxBoundsHandle();
                _boundsHandle.size = sponge.BoxBounds;
                _boundsHandle.center = Vector3.zero;
                _boundsHandle.DrawHandle();

                if (sponge.BoxBounds != _boundsHandle.size)
                {
                    Undo.RecordObject(sponge, "Updated Light Sponge Bounds");
                    sponge.BoxBounds = _boundsHandle.size;
                }

                Handles.color = Color.white;
                Handles.matrix = prevMatrix;
            }

            private void DoSphereHandles(LightSponge sponge)
            {
                EditorGUI.BeginChangeCheck();

                /*---Radius Handle---*/
                Handles.color = _handlesColor;
                float newRadius = Handles.RadiusHandle(Quaternion.identity, sponge.transform.position, sponge.SphereRadius);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sponge, "Updated Light Sponge Radius");
                    sponge.SphereRadius = newRadius;
                }

                Handles.color = Color.white;
            }
        }

        #endregion
#endif
    }
}