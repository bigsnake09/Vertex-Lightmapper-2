using Shaders.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shaders.Editor
{
    public class UberShaderEditor : ShaderGUI
    {
        private const string StandardShader = "Standard (Vlm)";
        private const string StandardShaderNoBatching = "Standard (Vlm - No Batching)";

        /*---Keywords---*/
        private const string KeywordRenderOpaque = "_RENDER_OPAQUE";
        private const string KeywordRenderCutout = "_RENDER_CUTOUT";

        private const string KeywordReflectionNone = "_REFLECTION_NONE";
        private const string KeywordReflectionCube = "_REFLECTION_CUBE";
        private const string KeywordReflectionProbe = "_REFLECTION_PROBE";
        private const string KeywordReflectionWorldSpace = "_REFLECTION_WORLDSPACE";
        private const string KeywordReflectionFromDiffuseAlpha = "_REFLECTION_FROM_DIFFUSE_ALPHA";

        private const string KeywordAllowAffineMapping = "_ALLOW_AFFINE_MAPPING";

        private const string KeywordAnimationUvNone = "_ANIMATION_UV_NONE";
        private const string KeywordAnimationUvScroll = "_ANIMATION_UV_SCROLL";
        private const string KeywordAnimationUvJump = "_ANIMATION_UV_JUMP";

        /*---Properties---*/
        private static readonly int PropSrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int PropDstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int PropRenderQueue = Shader.PropertyToID("_RenderQueue");
        private static readonly int PropCustomRenderQueue = Shader.PropertyToID("_CustomRenderQueue");
        private static readonly int PropCull = Shader.PropertyToID("_Cull");
        private static readonly int PropZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int PropZTest = Shader.PropertyToID("_ZTest");
        private static readonly int PropRenderMode = Shader.PropertyToID("_RenderMode");
        private static readonly int PropLighting = Shader.PropertyToID("_Lighting");
        private static readonly int PropDistanceClipping = Shader.PropertyToID("_DistanceClipping");
        private static readonly int PropVertexWobble = Shader.PropertyToID("_VertexWobble");
        private static readonly int PropScreenSpaceUvs = Shader.PropertyToID("_ScreenSpaceUvs");
        private static readonly int PropFadeToColor = Shader.PropertyToID("_FadeToColor");
        private static readonly int PropFadeDistanceMin = Shader.PropertyToID("_FadeDistanceMin");
        private static readonly int PropFadeDistanceMax = Shader.PropertyToID("_FadeDistanceMax");
        private static readonly int PropFadeColor = Shader.PropertyToID("_FadeColor");
        private static readonly int PropFadeOrigin = Shader.PropertyToID("_FadeOrigin");
        private static readonly int PropAffineBlend = Shader.PropertyToID("_AffineBlend");
        private static readonly int PropUvScroll = Shader.PropertyToID("_UvScroll");
        private static readonly int PropUvJump = Shader.PropertyToID("_UvJump");
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PropAlphaClip = Shader.PropertyToID("_AlphaClip");
        private static readonly int PropIllum = Shader.PropertyToID("_Illum");
        private static readonly int PropIllumIntensity = Shader.PropertyToID("_IllumIntensity");
        private static readonly int PropIllumTint = Shader.PropertyToID("_IllumTint");
        private static readonly int PropReflectionCube = Shader.PropertyToID("_ReflectionCube");
        private static readonly int PropReflectionColor = Shader.PropertyToID("_ReflectionColor");
        private static readonly int PropReflectionTint = Shader.PropertyToID("_ReflectionTint");

        public static void TryConfigureStandardMaterial(Material material)
        {
            string shaderName = material.shader.name;
            if (shaderName == StandardShader || shaderName == StandardShaderNoBatching) SetDefaulKeywords(material);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            //base.OnGUI(materialEditor, properties);

            Material mat = materialEditor.target as Material;

            GUILayout.Label("BallisticNG Standard Shader", EditorStyles.largeLabel);

            DrawGeneralSettingsGui(mat);
            DrawTextureMapsGui(mat);
            DrawUvAnimationGui(mat);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            if (oldShader.name != StandardShader && oldShader.name != StandardShaderNoBatching) SetDefaulKeywords(material);
        }

        private void DrawGeneralSettingsGui(Material mat)
        {
            GUI.color = Color.gray;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = Color.white;

            GUILayout.Label("General Settings", EditorStyles.largeLabel);

            /*---Render Mode---*/
            GUI.enabled = false;
            EditorGUILayout.LabelField("Draw Settings", EditorStyles.boldLabel);
            GUI.enabled = true;

            StandardBlendMode oldBlendMode = GetRenderMode(mat);
            if (oldBlendMode != StandardBlendMode.Opaque && oldBlendMode != StandardBlendMode.Cutout)
            {
                EditorGUILayout.HelpBox("The material will not be written to the Z buffer in this render mode.", MessageType.Info);
                if (mat.GetInt(PropZWrite) > 0)
                {
                    EditorGUILayout.HelpBox("It's reccomended you turn Z Write off in this render mode..", MessageType.Warning);
                }
            }

            StandardBlendMode newBlendMode = (StandardBlendMode)EditorGUILayout.EnumPopup("Render Mode", oldBlendMode);

            if (newBlendMode != oldBlendMode)
            {
                Undo.RecordObject(mat, "Updated Material Blend Mode");
                SetBlendMode(mat, newBlendMode);
                SetRenderQueue(mat, newBlendMode, true);
            }

            /*---Cull Mode---*/
            CullMode oldCullMode = (CullMode)mat.GetInt(PropCull);
            CullMode newCullMode = (CullMode)EditorGUILayout.EnumPopup("Face Culling", oldCullMode);

            if (newCullMode != oldCullMode)
            {
                Undo.RecordObject(mat, "Updated Cull Mode");
                mat.SetInt(PropCull, (int)newCullMode);
            }

            /*---Ignore Projector---*/
            bool oldIgnoreProjector = GetTagBool(mat, "IgnoreProjector");
            bool newIgnoreProjector = EditorGUILayout.Toggle("Ignore Projectors", oldIgnoreProjector);

            if (newIgnoreProjector != oldIgnoreProjector)
            {
                Undo.RecordObject(mat, "Update Ignore Projector");
                SetTagBool(mat, "IgnoreProjector", newIgnoreProjector);
            }

            GUILayout.Space(10);

            /*---ZWrite---*/
            GUI.enabled = false;
            EditorGUILayout.LabelField("Depth Settings", EditorStyles.boldLabel);
            GUI.enabled = true;

            bool oldZWrite = mat.GetInt(PropZWrite) > 0;
            bool newZWrite = EditorGUILayout.Toggle("Z Write", oldZWrite);

            if (newZWrite != oldZWrite)
            {
                Undo.RecordObject(mat, "Toggled Z Write");
                mat.SetInt(PropZWrite, newZWrite ? 1 : 0);
            }

            /*---ZTest---*/
            CompareFunction oldZTest = (CompareFunction)mat.GetInt(PropZTest);
            CompareFunction newZTest = (CompareFunction)EditorGUILayout.EnumPopup("Z Test", oldZTest);

            if (newZTest != oldZTest)
            {
                Undo.RecordObject(mat, "Updated Z Test");
                mat.SetInt(PropZTest, (int)newZTest);
            }

            bool oldFadeToColor = mat.GetInt(PropFadeToColor) > 0;
            bool newFadeToColor = EditorGUILayout.Toggle("Fade To Color", oldFadeToColor);
            if (oldFadeToColor != newFadeToColor)
            {
                Undo.RecordObject(mat, "Updated Fade Color");
                mat.SetInt(PropFadeToColor, newFadeToColor ? 1 : 0);
            }

            if (newFadeToColor)
            {
                float oldFadeDistance = mat.GetFloat(PropFadeDistanceMin);
                float newFadeDistance = EditorGUILayout.FloatField("Fade Distance Min", oldFadeDistance);
                if (!Mathf.Approximately(oldFadeDistance, newFadeDistance))
                {
                    Undo.RecordObject(mat, "Updated Fade Distance Min");
                    mat.SetFloat(PropFadeDistanceMin, newFadeDistance);
                }

                oldFadeDistance = mat.GetFloat(PropFadeDistanceMax);
                newFadeDistance = EditorGUILayout.FloatField("Fade Distance Max", oldFadeDistance);
                if (!Mathf.Approximately(oldFadeDistance, newFadeDistance))
                {
                    Undo.RecordObject(mat, "Updated Fade Distance Max");
                    mat.SetFloat(PropFadeDistanceMax, newFadeDistance);
                }

                Color oldFadeColor = mat.GetColor(PropFadeColor);
                Color newFadeColor = EditorGUILayout.ColorField("Fade Color", oldFadeColor);
                if (oldFadeColor != newFadeColor)
                {
                    Undo.RecordObject(mat, "Updated Fade Color");
                    mat.SetColor(PropFadeColor, newFadeColor);
                }

                float fadeOrigin = mat.GetFloat(PropFadeOrigin);
                float newFadeOrigin = EditorGUILayout.Slider("Fade Origin (Vertex <> Object)", fadeOrigin, 0.0f, 1.0f);
                if (fadeOrigin != newFadeOrigin)
                {
                    Undo.RecordObject(mat, "Updated Fade Origin");
                    mat.SetFloat(PropFadeOrigin, newFadeOrigin);
                }
            }

            GUILayout.Space(10);

            /*---Render Queue---*/
            GUI.enabled = false;
            EditorGUILayout.LabelField($"Current Render Queue: {mat.renderQueue}", EditorStyles.boldLabel);
            GUI.enabled = true;

            bool oldUseCustomRenderQueue = mat.GetInt(PropCustomRenderQueue) > 0.0f;
            bool newUseCustomRenderQueue = EditorGUILayout.Toggle("Use Custom Render Queue", oldUseCustomRenderQueue);

            if (newUseCustomRenderQueue != oldUseCustomRenderQueue)
            {
                Undo.RecordObject(mat, "Toggled Use Custom Render Queue");
                mat.SetInt(PropCustomRenderQueue, newUseCustomRenderQueue ? 1 : 0);

                if (!newUseCustomRenderQueue) SetRenderQueue(mat, newBlendMode, false);
                else mat.renderQueue = mat.GetInt(PropRenderQueue);
            }

            if (newUseCustomRenderQueue)
            {
                int oldRenderQueue = mat.GetInt(PropRenderQueue);
                int newRenderQueue = EditorGUILayout.IntField("Custom Render Queue Index", oldRenderQueue);

                if (newRenderQueue != oldRenderQueue)
                {
                    Undo.RecordObject(mat, "Updated Custom Render Queue Index");

                    mat.SetInt(PropRenderQueue, newRenderQueue);
                    mat.renderQueue = newRenderQueue;
                }
            }
            GUILayout.Space(10);

            GUI.enabled = false;
            EditorGUILayout.LabelField("BallisticNG Draw Settings", EditorStyles.boldLabel);
            GUI.enabled = true;
            /*---Lit---*/
            float oldLit = mat.GetFloat(PropLighting);
            float newLit = EditorGUILayout.Slider("Vertex Lighting Blend", oldLit, 0, 1);

            if (!Mathf.Approximately(newLit, oldLit))
            {
                Undo.RecordObject(mat, "Updated Vertex Lighting");
                mat.SetFloat(PropLighting, newLit);
            }

            /*---Allow Distance Clip---*/
            bool oldDistanceClip = mat.GetInt(PropDistanceClipping) > 0;
            bool newDistanceClip = EditorGUILayout.Toggle("Allow Distance Clip", oldDistanceClip);

            if (newDistanceClip != oldDistanceClip)
            {
                Undo.RecordObject(mat, "Toggled Allow Distance Clip");
                mat.SetInt(PropDistanceClipping, newDistanceClip ? 1 : 0);
            }

            /*---Allow Vertex Wobble---*/
            bool oldVertexWobble = mat.GetInt(PropVertexWobble) > 0;
            bool newVertexWobble = EditorGUILayout.Toggle("Allow Vertex Wobble", oldVertexWobble);

            if (newVertexWobble != oldVertexWobble)
            {
                Undo.RecordObject(mat, "Toggled Allow Vertex Wobble");
                mat.SetInt(PropVertexWobble, newVertexWobble ? 1 : 0);
            }

            /*---Allow Affine Mapping---*/
            GUILayout.BeginHorizontal();
            {
                bool oldAffineMapping = mat.IsKeywordEnabled(KeywordAllowAffineMapping);
                bool newAffineMapping = EditorGUILayout.Toggle("Allow Affine Mapping", oldAffineMapping);

                if (newAffineMapping != oldAffineMapping)
                {
                    Undo.RecordObject(mat, "Toggled Allow Affine Mapping");
                    ShaderEditorHelpers.SetKeyword(mat, KeywordAllowAffineMapping, newAffineMapping);
                }

                GUI.enabled = newAffineMapping;
                float oldAffineBlend = mat.GetFloat(PropAffineBlend);
                float newAffineBlend = EditorGUILayout.Slider("Blend", oldAffineBlend, 0, 1);

                if (!Mathf.Approximately(newAffineBlend, oldAffineBlend))
                {
                    Undo.RecordObject(mat, "Updated Affine Blend");
                    mat.SetFloat(PropAffineBlend, newAffineBlend);
                }
                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawTextureMapsGui(Material mat)
        {
            GUI.color = Color.gray;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = Color.white;

            GUILayout.Label("Texture Settings", EditorStyles.largeLabel);

            Vector2 oldTextureOffset = mat.mainTextureOffset;
            Vector2 newTextureOffset = EditorGUILayout.Vector2Field("Texture Offset", mat.mainTextureOffset);
            if (oldTextureOffset != newTextureOffset)
            {
                Undo.RecordObject(mat, "Changed Texture Offset");
                mat.mainTextureOffset = newTextureOffset;
            }

            Vector2 oldTextureTile = mat.mainTextureScale;
            Vector2 newTextureTile = EditorGUILayout.Vector2Field("Texture Tiling", mat.mainTextureScale);
            if (oldTextureTile != newTextureTile)
            {
                Undo.RecordObject(mat, "Changed Texture Tiling");
                mat.mainTextureScale = newTextureTile;
            }

            bool oldScreenSpaceUvs = mat.GetInt(PropScreenSpaceUvs) > 0;
            bool newScreenSpaceUvs = EditorGUILayout.Toggle("Screen Space Uvs", oldScreenSpaceUvs);
            if (oldScreenSpaceUvs != newScreenSpaceUvs)
            {
                Undo.RecordObject(mat, "Changed Screen Space Uvs");
                mat.SetInt(PropScreenSpaceUvs, newScreenSpaceUvs ? 1 : 0);
            }

            GUILayout.Space(10);

            DrawDiffuseTextureMap(mat);
            DrawIllumTextureMap(mat);
            DrawReflectionMap(mat);

            GUILayout.EndVertical();
        }

        private void DrawUvAnimationGui(Material mat)
        {
            GUI.color = Color.gray;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = Color.white;

            GUILayout.Label("UV Animation Settings", EditorStyles.largeLabel);

            /*---Mode Selector---*/
            StandardUvAnimationMode oldUvMode = GetUvAnimationMode(mat);
            StandardUvAnimationMode newUvMode = (StandardUvAnimationMode)EditorGUILayout.EnumPopup("UV Animation Mode", oldUvMode);

            if (newUvMode != oldUvMode)
            {
                Undo.RecordObject(mat, "Toggled Uv Animation Mode");
                SetuvAnimationMode(mat, newUvMode);
            }

            /*---Settings---*/
            switch (newUvMode)
            {
                case StandardUvAnimationMode.None:
                    break;
                case StandardUvAnimationMode.Scroll:
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);

                        Vector4 oldUvSettings = mat.GetVector(PropUvScroll);
                        Vector2 scroll = EditorGUILayout.Vector2Field("Scroll Speed", new Vector2(oldUvSettings.x, oldUvSettings.y));
                        Vector4 newUvSettings = new Vector4(scroll.x, scroll.y, 0.0f, 0.0f);

                        if (newUvSettings != oldUvSettings)
                        {
                            Undo.RecordObject(mat, "Updated UV Scroll Settings");
                            mat.SetVector(PropUvScroll, newUvSettings);
                        }

                        GUILayout.EndVertical();
                    }
                    break;
                case StandardUvAnimationMode.Jump:
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);

                        Vector4 oldUvSettings = mat.GetVector(PropUvJump);
                        Vector2 jumpAmount = EditorGUILayout.Vector2Field("Jump Amount", new Vector2(oldUvSettings.x, oldUvSettings.y));
                        Vector3 jumpTime = EditorGUILayout.Vector2Field("Time Between Jump", new Vector2(oldUvSettings.z, oldUvSettings.w));
                        Vector4 newUvSettings = new Vector4(jumpAmount.x, jumpAmount.y, jumpTime.x, jumpTime.y);

                        if (newUvSettings != oldUvSettings)
                        {
                            Undo.RecordObject(mat, "Updated Uv Jump Settings");
                            mat.SetVector(PropUvJump, newUvSettings);
                        }

                        GUILayout.EndVertical();
                    }
                    break;
            }

            GUILayout.EndVertical();
        }

        private void DrawDiffuseTextureMap(Material mat)
        {
            Color oldTint = mat.GetColor(PropColor);
            Color newTint = EditorGUILayout.ColorField("Diffuse Tint", oldTint);

            if (newTint != oldTint)
            {
                Undo.RecordObject(mat, "Updated Diffuse Tint");
                mat.SetColor(PropColor, newTint);
            }

            Color oldIllumTint = mat.GetColor(PropIllumTint);
            Color newIllumTint = EditorGUILayout.ColorField("Illumination Tint", oldIllumTint);
            if (oldIllumTint != newIllumTint)
            {
                Undo.RecordObject(mat, "Updated Illumination TInt");
                mat.SetColor(PropIllumTint, newIllumTint);
            }

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            /*---Texture Box---*/
            GUILayout.BeginVertical();
            {
                Texture oldTexture = mat.GetTexture(PropMainTex);
                Texture newTexture = (Texture)EditorGUILayout.ObjectField("Diffuse Map", oldTexture, typeof(Texture), false);

                if (newTexture != oldTexture)
                {
                    Undo.RecordObject(mat, "Updated Diffuse Map");
                    mat.SetTexture(PropMainTex, newTexture);
                }

                if (GetRenderMode(mat) == StandardBlendMode.Cutout)
                {
                    float oldAlphaClip = mat.GetFloat(PropAlphaClip);
                    float newAlphaClip = EditorGUILayout.Slider("Alpha Clip", oldAlphaClip, 0, 1);

                    if (!Mathf.Approximately(newAlphaClip, oldAlphaClip))
                    {
                        Undo.RecordObject(mat, "Updated Alpha Clip");
                        mat.SetFloat(PropAlphaClip, newAlphaClip);
                    }
                }

            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawIllumTextureMap(Material mat)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            /*---Texture Box---*/
            GUILayout.BeginVertical();
            {
                Texture oldTexture = mat.GetTexture(PropIllum);
                Texture newTexture = (Texture)EditorGUILayout.ObjectField("Illumination Map", oldTexture, typeof(Texture), false);

                if (newTexture != oldTexture)
                {
                    Undo.RecordObject(mat, "Updated Illumination Map");
                    mat.SetTexture(PropIllum, newTexture);
                }

                float oldIllumIntensity = mat.GetFloat(PropIllumIntensity);
                float newIllumIntensity = EditorGUILayout.Slider("Illumination Intensity", oldIllumIntensity, 0, 1);

                if (!Mathf.Approximately(newIllumIntensity, oldIllumIntensity))
                {
                    Undo.RecordObject(mat, "Updated Illumination Intensity");
                    mat.SetFloat(PropIllumIntensity, newIllumIntensity);
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawReflectionMap(Material mat)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            /*---Reflection Mode Selector---*/
            StandardReflectionMode oldReflectionMode = GetReflectionMode(mat);
            StandardReflectionMode newReflectionMode = (StandardReflectionMode)EditorGUILayout.EnumPopup("Reflection Mode", oldReflectionMode);

            if (newReflectionMode != oldReflectionMode)
            {
                Undo.RecordObject(mat, "Updated Reflection Mode");
                SetReflectionMode(mat, newReflectionMode);
            }

            if (newReflectionMode != StandardReflectionMode.None)
            {
                bool oldReflectionSpace = ShaderEditorHelpers.GetKeyword(mat, KeywordReflectionWorldSpace);
                bool newReflectionSpace = EditorGUILayout.Toggle("World Space Reflection", oldReflectionSpace);

                if (newReflectionSpace != oldReflectionSpace)
                {
                    Undo.RecordObject(mat, "Updated Reflection Space");
                    ShaderEditorHelpers.SetKeyword(mat, KeywordReflectionWorldSpace, newReflectionSpace);
                }
            }

            if (newReflectionMode == StandardReflectionMode.Cube)
            {
                Texture oldTexture = mat.GetTexture(PropReflectionCube);
                Texture newTexture = (Texture)EditorGUILayout.ObjectField("Reflection Cube", oldTexture, typeof(Cubemap), false);

                if (newTexture != oldTexture)
                {
                    Undo.RecordObject(mat, "Updated Reflection Cube");
                    if (newTexture as Cubemap) mat.SetTexture(PropReflectionCube, newTexture);
                }
            }
            else if (newReflectionMode == StandardReflectionMode.Probe)
            {
                EditorGUILayout.HelpBox("This material will rely on Unity's reflection probes for this reflection.", MessageType.Info);
            }

            if (newReflectionMode != StandardReflectionMode.None)
            {
                bool oldUseDiffuse = ShaderEditorHelpers.GetKeyword(mat, KeywordReflectionFromDiffuseAlpha);
                bool newUseDiffuse = EditorGUILayout.Toggle("Use Diffuse Alpha As Mask", oldUseDiffuse);

                if (newUseDiffuse != oldUseDiffuse)
                {
                    Undo.RecordObject(mat, "Updated Use Diffuse As Reflection Mask");
                    ShaderEditorHelpers.SetKeyword(mat, KeywordReflectionFromDiffuseAlpha, newUseDiffuse);
                }

                if (!newUseDiffuse)
                {
                    Texture oldTexture = mat.GetTexture(PropReflectionColor);
                    Texture newTexture = (Texture)EditorGUILayout.ObjectField("Reflection Color Mask", oldTexture, typeof(Texture), false);

                    if (newTexture != oldTexture)
                    {
                        Undo.RecordObject(mat, "Updated Reflection Mask");
                        mat.SetTexture(PropReflectionColor, newTexture);
                    }
                }

                Color oldReflectionColor = mat.GetColor(PropReflectionTint);
                Color newReflectionColor = EditorGUILayout.ColorField("Reflection Tint", oldReflectionColor);

                if (newReflectionColor != oldReflectionColor)
                {
                    Undo.RecordObject(mat, "Updated Reflection Tint");
                    mat.SetColor(PropReflectionTint, newReflectionColor);
                }
            }

            GUILayout.EndVertical();
        }

        private static void SetDefaulKeywords(Material m)
        {
            ShaderEditorHelpers.KeywordStateBuilder stateBuilder = new ShaderEditorHelpers.KeywordStateBuilder();

            stateBuilder.Add(KeywordRenderOpaque, true);
            stateBuilder.Add(KeywordRenderCutout, false);

            stateBuilder.Add(KeywordReflectionNone, true);
            stateBuilder.Add(KeywordReflectionCube, false);
            stateBuilder.Add(KeywordReflectionProbe, false);
            stateBuilder.Add(KeywordReflectionWorldSpace, false);
            stateBuilder.Add(KeywordReflectionFromDiffuseAlpha, false);

            stateBuilder.Add(KeywordAllowAffineMapping, true);

            stateBuilder.Add(KeywordAnimationUvNone, true);
            stateBuilder.Add(KeywordAnimationUvScroll, false);
            stateBuilder.Add(KeywordAnimationUvJump, false);

            ShaderEditorHelpers.SetKeywords(m, stateBuilder.States);

            SetBlendMode(m, StandardBlendMode.Opaque);
            m.SetInt(PropCull, (int)CullMode.Back);
            m.SetInt(PropZWrite, 1);
            m.SetInt(PropZTest, (int)CompareFunction.LessEqual);
            m.SetFloat(PropLighting, 1.0f);
            m.SetInt(PropDistanceClipping, 1);
            m.SetInt(PropVertexWobble, 1);
            m.SetFloat(PropAffineBlend, 1);
            SetTagBool(m, "IgnoreProjector", false);
        }

        private static void SetBlendMode(Material m, StandardBlendMode blendMode)
        {
            /*---Material Keywords---*/
            ShaderEditorHelpers.KeywordStateBuilder stateBuilder = new ShaderEditorHelpers.KeywordStateBuilder();
            stateBuilder.Add(KeywordRenderOpaque, blendMode == StandardBlendMode.Opaque);
            stateBuilder.Add(KeywordRenderCutout, blendMode == StandardBlendMode.Cutout);

            m.SetInt(PropRenderMode, (int)blendMode);
            ShaderEditorHelpers.SetKeywords(m, stateBuilder.States);

            /*---Apply Settings---*/
            switch (blendMode)
            {
                case StandardBlendMode.Opaque:
                    {
                        m.SetInt(PropSrcBlend, (int)BlendMode.One);
                        m.SetInt(PropDstBlend, (int)BlendMode.Zero);
                        SetRenderType(m, blendMode);
                        SetRenderQueue(m, blendMode, true);
                        break;
                    }
                case StandardBlendMode.Cutout:
                    {
                        m.SetInt(PropSrcBlend, (int)BlendMode.One);
                        m.SetInt(PropDstBlend, (int)BlendMode.Zero);
                        SetRenderType(m, blendMode);
                        SetRenderQueue(m, blendMode, true);
                        break;
                    }
                case StandardBlendMode.Transparent:
                    {
                        m.SetInt(PropSrcBlend, (int)BlendMode.SrcAlpha);
                        m.SetInt(PropDstBlend, (int)BlendMode.OneMinusSrcAlpha);
                        SetRenderType(m, blendMode);
                        SetRenderQueue(m, blendMode, true);
                        break;
                    }
                case StandardBlendMode.Additive:
                    {
                        m.SetInt(PropSrcBlend, (int)BlendMode.SrcAlpha);
                        m.SetInt(PropDstBlend, (int)BlendMode.One);
                        SetRenderType(m, blendMode);
                        SetRenderQueue(m, blendMode, true);
                        break;
                    }
                case StandardBlendMode.Multiply:
                    {
                        m.SetInt(PropSrcBlend, (int)BlendMode.DstColor);
                        m.SetInt(PropDstBlend, (int)BlendMode.Zero);
                        SetRenderType(m, blendMode);
                        SetRenderQueue(m, blendMode, true);
                        break;
                    }
            }
        }

        private static void SetRenderQueue(Material mat, StandardBlendMode mode, bool allowCustomRenderQueue)
        {
            bool zWrite = mat.GetInt(PropZWrite) > 0.0f;

            bool useCustomRenderQueue = allowCustomRenderQueue && mat.GetInt(PropCustomRenderQueue) > 0.0f;
            if (useCustomRenderQueue)
            {
                int customRenderQueue = mat.GetInt(PropRenderQueue);
                mat.renderQueue = customRenderQueue;

                return;
            }

            switch (mode)
            {
                case StandardBlendMode.Opaque:
                    mat.renderQueue = (int)RenderQueue.Geometry;
                    break;
                case StandardBlendMode.Cutout:
                    mat.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
                case StandardBlendMode.Transparent:
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case StandardBlendMode.Additive:
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case StandardBlendMode.Multiply:
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    break;
            }
        }

        private static void SetRenderType(Material m, StandardBlendMode blendMode)
        {
            switch (blendMode)
            {
                case StandardBlendMode.Opaque:
                    m.SetOverrideTag("RenderType", "Opaque");
                    break;
                case StandardBlendMode.Cutout:
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                    break;
                case StandardBlendMode.Transparent:
                    m.SetOverrideTag("RenderType", "Transparent");
                    break;
                case StandardBlendMode.Additive:
                    m.SetOverrideTag("RenderType", "Transparent");
                    break;
                case StandardBlendMode.Multiply:
                    m.SetOverrideTag("RenderType", "Transparent");
                    break;
            }
        }

        /// <summary>
        /// Returns the current render type of a material.
        /// </summary>
        private StandardBlendMode GetRenderMode(Material m)
        {
            return (StandardBlendMode)m.GetInt(PropRenderMode);
        }

        /// <summary>
        /// Returns the current reflection mode of a material.
        /// </summary>
        private StandardReflectionMode GetReflectionMode(Material m)
        {
            if (m.IsKeywordEnabled(KeywordReflectionNone)) return StandardReflectionMode.None;
            if (m.IsKeywordEnabled(KeywordReflectionCube)) return StandardReflectionMode.Cube;
            if (m.IsKeywordEnabled(KeywordReflectionProbe)) return StandardReflectionMode.Probe;

            return StandardReflectionMode.None;
        }

        private void SetReflectionMode(Material m, StandardReflectionMode mode)
        {
            ShaderEditorHelpers.KeywordStateBuilder stateBuilder = new ShaderEditorHelpers.KeywordStateBuilder();
            stateBuilder.Add(KeywordReflectionNone, mode == StandardReflectionMode.None);
            stateBuilder.Add(KeywordReflectionCube, mode == StandardReflectionMode.Cube);
            stateBuilder.Add(KeywordReflectionProbe, mode == StandardReflectionMode.Probe);

            ShaderEditorHelpers.SetKeywords(m, stateBuilder.States);
        }

        /// <summary>
        /// Returns the current reflection mode of a material.
        /// </summary>
        private StandardUvAnimationMode GetUvAnimationMode(Material m)
        {
            if (m.IsKeywordEnabled(KeywordAnimationUvNone)) return StandardUvAnimationMode.None;
            if (m.IsKeywordEnabled(KeywordAnimationUvScroll)) return StandardUvAnimationMode.Scroll;
            if (m.IsKeywordEnabled(KeywordAnimationUvJump)) return StandardUvAnimationMode.Jump;

            return StandardUvAnimationMode.None;
        }

        private void SetuvAnimationMode(Material m, StandardUvAnimationMode mode)
        {
            ShaderEditorHelpers.KeywordStateBuilder stateBuilder = new ShaderEditorHelpers.KeywordStateBuilder();
            stateBuilder.Add(KeywordAnimationUvNone, mode == StandardUvAnimationMode.None);
            stateBuilder.Add(KeywordAnimationUvScroll, mode == StandardUvAnimationMode.Scroll);
            stateBuilder.Add(KeywordAnimationUvJump, mode == StandardUvAnimationMode.Jump);

            ShaderEditorHelpers.SetKeywords(m, stateBuilder.States);
        }

        private static bool GetTagBool(Material mat, string tag)
        {
            return mat.GetTag(tag, true, "false") == "true";
        }

        private static void SetTagBool(Material mat, string tag, bool value)
        {
            mat.SetOverrideTag(tag, value ? "true" : "false");
        }

        public enum StandardBlendMode
        {
            Opaque,
            Cutout,
            Transparent,
            Additive,
            Multiply
        }

        public enum StandardReflectionMode
        {
            None,
            Cube,
            Probe
        }

        public enum StandardUvAnimationMode
        {
            None,
            Scroll,
            Jump
        }
    }
}