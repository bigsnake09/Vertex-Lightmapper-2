using UnityEditor;
using UnityEngine;

namespace Shaders.Editor
{
    [InitializeOnLoad]
    public class EditorShaderPrefs
    {
        static EditorShaderPrefs()
        {
            ApplySettings();
        }
        
        private const string SettingRetroClippingDistanceEnabled = "Vlm_RetroClippingDistanceEnabled";
        private const string SettingRetroClippingDistanceValue = "Vlm_RetroClippingDistanceValue";
        
        private const string SettingVertexSnapEnabled = "Vlm_VertexSnapEnabled";
        private const string SettingVertexSnapValue = "Vlm_VertexSnapValue";
        private const string SettingVertexSnapGridSize = "Vlm_VertexSnapGridSize";
        private const string SettingAffineMappingEnabled = "Vlm_AffineMappingEnabled";

        public static bool PreviewRetroDistanceClippingEnabled
        {
            get => EditorPrefs.GetBool(SettingRetroClippingDistanceEnabled);
            set
            {
                EditorPrefs.SetBool(SettingRetroClippingDistanceEnabled, value);
                ShaderSettings.ClippingDistance = PreviewRetroDistanceClippingEnabled ? PreviewRetroClippingDistanceValue : 0.0f;
            }
        }
        
        public static float PreviewRetroClippingDistanceValue
        {
            get => EditorPrefs.HasKey(SettingRetroClippingDistanceValue) ? EditorPrefs.GetFloat(SettingRetroClippingDistanceValue) : ShaderSettings.DefaultClippingDistance;
            set
            {
                EditorPrefs.SetFloat(SettingRetroClippingDistanceValue, value);
                ShaderSettings.ClippingDistance = PreviewRetroDistanceClippingEnabled ? PreviewRetroClippingDistanceValue : 0.0f;
            }
        }
        
        public static bool PreviewRetroVertexEnabled
        {
            get => EditorPrefs.GetBool(SettingVertexSnapEnabled);
            set
            {
                EditorPrefs.SetBool(SettingVertexSnapEnabled, value);
                ShaderSettings.VertexSnap = PreviewRetroVertexEnabled;
            }
        }
        
        public static float PreviewRetroVertexValue
        {
            get => EditorPrefs.HasKey(SettingVertexSnapValue) ? EditorPrefs.GetFloat(SettingVertexSnapValue) : ShaderSettings.DefaultVertexSnapAmt;
            set
            {
                EditorPrefs.SetFloat(SettingVertexSnapValue, value);
                ShaderSettings.VertexSnapAmt = PreviewRetroVertexValue;
            }
        }
        
        public static float PreviewRetroVertexGridSize
        {
            get => EditorPrefs.HasKey(SettingVertexSnapGridSize) ? EditorPrefs.GetFloat(SettingVertexSnapGridSize) : ShaderSettings.DefaultVertexSnapClipGridSize;
            set
            {
                EditorPrefs.SetFloat(SettingVertexSnapGridSize, value);
                ShaderSettings.VertexSnapClipGridSize = PreviewRetroVertexGridSize;
            }
        }
        
        public static bool PreviewAffineMappingEnabled
        {
            get => EditorPrefs.GetBool(SettingAffineMappingEnabled);
            set
            {
                EditorPrefs.SetBool(SettingAffineMappingEnabled, value);
                ShaderSettings.AffineTextureMapping = PreviewAffineMappingEnabled;
            }
        }
        
        [PreferenceItem("Vertex Lightmapper")]
        private static void PreferencesGui()
        {
            EditorGUILayout.HelpBox("These control an editor preview of the shader settings. Use the ShaderSettings class to set these in-game.", MessageType.Info);
            
            // clipping enable / distance
            ++EditorGUI.indentLevel;
            PreviewRetroDistanceClippingEnabled = EditorGUILayout.Toggle("Retro Clipping", PreviewRetroDistanceClippingEnabled);

            ++EditorGUI.indentLevel;
            GUI.enabled = PreviewRetroDistanceClippingEnabled;
            PreviewRetroClippingDistanceValue = EditorGUILayout.FloatField("Distance", PreviewRetroClippingDistanceValue);
            GUI.enabled = true;
            --EditorGUI.indentLevel;

            GUILayout.Space(10);
            
            // vertex snap enable / resolution
            PreviewRetroVertexEnabled = EditorGUILayout.Toggle("Retro Vertices", PreviewRetroVertexEnabled);
            ++EditorGUI.indentLevel;
            GUI.enabled = PreviewRetroVertexEnabled;
            PreviewRetroVertexValue = EditorGUILayout.FloatField("Amount", PreviewRetroVertexValue);
            PreviewRetroVertexGridSize = EditorGUILayout.FloatField("Clip Grid Size", PreviewRetroVertexGridSize);
            GUI.enabled = true;
            --EditorGUI.indentLevel;
            
            PreviewAffineMappingEnabled = EditorGUILayout.Toggle("Affine Mapping", PreviewAffineMappingEnabled);
            --EditorGUI.indentLevel;
        }

        private static void ApplySettings()
        {
            // just assign properties to themselves to apply each one
            PreviewRetroDistanceClippingEnabled = PreviewRetroDistanceClippingEnabled;
            PreviewRetroClippingDistanceValue = PreviewRetroClippingDistanceValue;
            PreviewRetroVertexEnabled = PreviewRetroVertexEnabled;
            PreviewRetroVertexValue = PreviewRetroVertexValue;
            PreviewRetroVertexGridSize = PreviewRetroVertexGridSize;
            PreviewAffineMappingEnabled = PreviewAffineMappingEnabled;
        }
    }
}