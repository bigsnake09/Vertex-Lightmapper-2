#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Vlm.Editor;
using Object = UnityEngine.Object;

namespace Vlm
{
    public class VlmWindow : EditorWindow
    {
        /// <summary>
        /// Retriieves or creates a new VLM settings object in the scene.
        /// </summary>
        /// <returns></returns>
        private static VlmSettings Settings
        {
            get
            {
                VlmSettings settings = FindObjectOfType<VlmSettings>();
                if (!settings)
                {
                    GameObject newSettings = new GameObject("Lighting Settings") { hideFlags = HideFlags.HideInHierarchy };
                    settings = newSettings.AddComponent<VlmSettings>();
                }
                return settings;
            }
        }

        private Vector2 _scroll;

        [MenuItem("Window/Rendering/Vertex Lighting")]
        public static void InitWindow()
        {
            const float width = 300;
            const float height = 600;

            EditorWindow thisWindow = GetWindow(typeof(VlmWindow));

            thisWindow.position = new Rect(Screen.currentResolution.width * 0.5f - width / 2, Screen.currentResolution.height * 0.5f - height / 2, width, height);
            thisWindow.titleContent = new GUIContent("Vertex Lighting");
        }

        private static void SetSelectionLightmappable(Object[] objects, bool state)
        {
            bool affectChildren = false;
            bool userConfirmedAffectChild = false;

            foreach (Object o in objects)
            {
                GameObject go = o as GameObject;
                if (!go) continue;

                bool hasChildren = go.transform.childCount > 0;
                if (!userConfirmedAffectChild && !affectChildren && hasChildren)
                {
                    affectChildren = EditorUtility.DisplayDialog("Affect Childen", $"One or more objects in the selection have children. Would you like to {(state ? "expose" : "hide")} its children {(state ? "to" : "from")} VLM too?", "Yes", "No");
                    userConfirmedAffectChild = true;
                }

                if (hasChildren && affectChildren) SetSelectionLightmappableRecursive(go, state);
                else SetLightmapFlag(go, state);
            }
        }

        private static void SetSelectionLightmappableRecursive(GameObject go, bool state)
        {
            SetLightmapFlag(go, state);
            foreach (Transform t in go.transform) SetSelectionLightmappableRecursive(t.gameObject, state);
        }

        private static void SetLightmapFlag(GameObject obj, bool state)
        {
            bool flagSet = GameObjectUtility.AreStaticEditorFlagsSet(obj, StaticEditorFlags.ContributeGI);
            if (flagSet == state) return;

            Undo.RecordObject(obj, "Updated Lightmap Static");
            if (state) GameObjectUtility.SetStaticEditorFlags(obj, GameObjectUtility.GetStaticEditorFlags(obj) | StaticEditorFlags.ContributeGI);
            else GameObjectUtility.SetStaticEditorFlags(obj, GameObjectUtility.GetStaticEditorFlags(obj) & ~StaticEditorFlags.ContributeGI);
        }

        /// <summary>
        /// Loads settings from the scene.
        /// </summary>
        private void LoadSettings()
        {
            Settings.Apply();
        }

        /// <summary>
        /// Saves settings to the scene.
        /// </summary>
        private void SaveSettings()
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Settings.Copy();
        }

        /// <summary>
        /// Checks if the Unity lightmapper is enabled and lets the user know it's being disabled.
        /// </summary>
        private void UnityLightingCheck()
        {
            if (BuildPipeline.isBuildingPlayer) return;

            if (Lightmapping.bakedGI || Lightmapping.realtimeGI)
            {
                bool disable = EditorUtility.DisplayDialog("Notice", "Unity's lightmapper is currently enabled. Would you like to disable it?", "Yes", "No");
                if (disable)
                {
                    Lightmapping.bakedGI = false;
                    Lightmapping.realtimeGI = false;
                }
            }
        }

        private void DrawSettings()
        {
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = Color.white;

            GUILayout.Label("Settings", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                VlmBakeData.BounceLight = EditorGUILayout.Toggle("Bounce Light", VlmBakeData.BounceLight);
                GUI.enabled = VlmBakeData.BounceLight;
                ++EditorGUI.indentLevel;
                VlmBakeData.BounceDistance = EditorGUILayout.FloatField("Bounce Distance", VlmBakeData.BounceDistance);
                VlmBakeData.BounceIntensity = EditorGUILayout.FloatField("Bounce Intensity", VlmBakeData.BounceIntensity);
                VlmBakeData.BounceConeAngle = EditorGUILayout.FloatField("Bounce Cone Angle", VlmBakeData.BounceConeAngle);
                VlmBakeData.BounceConeMinimumDistance = EditorGUILayout.FloatField("Bounce Cone Minimum Distance", VlmBakeData.BounceConeMinimumDistance);
                --EditorGUI.indentLevel;
                GUI.enabled = true;

                GUILayout.Space(10);
                VlmBakeData.ShadowBias = EditorGUILayout.FloatField("Shadow Bias", VlmBakeData.ShadowBias);
                VlmBakeData.BackfaceShadows = EditorGUILayout.Toggle("Backface Shadows", VlmBakeData.BackfaceShadows);

                GUILayout.Space(10);
                RenderSettings.ambientMode = (AmbientMode)EditorGUILayout.EnumPopup("Ambient Source", RenderSettings.ambientMode);
                ++EditorGUI.indentLevel;
                switch (RenderSettings.ambientMode)
                {
                    case AmbientMode.Skybox:
                        RenderSettings.ambientIntensity = EditorGUILayout.Slider(RenderSettings.ambientIntensity, 0, 8);
                        break;
                    case AmbientMode.Trilight:
                        RenderSettings.ambientSkyColor = EditorGUILayout.ColorField("Sky Color", RenderSettings.ambientSkyColor);
                        RenderSettings.ambientEquatorColor = EditorGUILayout.ColorField("Equator Color", RenderSettings.ambientEquatorColor);
                        RenderSettings.ambientGroundColor = EditorGUILayout.ColorField("Ground Color", RenderSettings.ambientGroundColor);
                        break;
                    case AmbientMode.Flat:
                        RenderSettings.ambientLight = EditorGUILayout.ColorField("Ambient Color", RenderSettings.ambientLight);
                        break;
                    case AmbientMode.Custom:
                        EditorGUILayout.HelpBox("This mode is not supported.", MessageType.Error);
                        break;
                }
                --EditorGUI.indentLevel;

                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Load", EditorStyles.toolbarButton)) LoadSettings();
                if (GUILayout.Button("Save", EditorStyles.toolbarButton)) SaveSettings();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            --EditorGUI.indentLevel;

             GUILayout.EndVertical();
        }

        private void DrawBakeButtons()
        {
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = Color.white;

            GUILayout.Label("Bake", EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                VlmBakeData.AutoApply = EditorGUILayout.Toggle("Auto Apply On load", VlmBakeData.AutoApply);
                VlmBakeData.AlwaysUseVertexStreams = EditorGUILayout.Toggle("Always Use Vertex Streams", VlmBakeData.AlwaysUseVertexStreams);

                GUILayout.Space(10);
                if (GUILayout.Button("Bake Lighting", EditorStyles.toolbarButton))
                {
                    UnityLightingCheck();
                    SaveSettings();
                    VlmBakeTask.Bake();
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }

        private void DrawTools()
        {
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = Color.white;

            GUILayout.Label("Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use the expose and hide buttons below to control which objects will be affected by the lightmapper. If you'd like to expose everything, just select the top level object and you'll be given the option to automatically expose all child objects.", MessageType.Info);

            GUILayout.Space(10);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                ++EditorGUI.indentLevel;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Expose Selection", EditorStyles.toolbarButton)) SetSelectionLightmappable(Selection.objects, true);
                if (GUILayout.Button("Unexpose Selection", EditorStyles.toolbarButton)) SetSelectionLightmappable(Selection.objects, false);
                GUILayout.EndHorizontal();
                
                --EditorGUI.indentLevel;
            }
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }

        #region Unity Methods

        private void OnGUI()
        {
            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawSettings();

            DrawTools();
            DrawBakeButtons();
            GUILayout.EndScrollView();
        }

        private void OnEnable()
        {
            EditorSceneManager.sceneOpened += EditorSceneManagerOnsceneOpened;
            EditorSceneManager.newSceneCreated += EditorSceneManagerOnnewSceneCreated;
            LoadSettings();
        }

        private void EditorSceneManagerOnnewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            LoadSettings();
        }

        private void EditorSceneManagerOnsceneOpened(Scene scene, OpenSceneMode mode)
        {
            LoadSettings();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= EditorSceneManagerOnsceneOpened;
            EditorSceneManager.newSceneCreated -= EditorSceneManagerOnnewSceneCreated;
        }

        #endregion
    }
}
#endif
