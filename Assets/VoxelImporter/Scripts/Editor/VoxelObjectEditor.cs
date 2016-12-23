using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    [CustomEditor(typeof(VoxelObject))]
    public class VoxelObjectEditor : VoxelBaseEditor
    {
        public VoxelObject objectTarget { get; protected set; }
        public VoxelObjectCore objectCore { get; protected set; }

        public virtual Mesh mesh { get { return objectTarget.mesh; } set { objectTarget.mesh = value; } }
        public virtual List<Material> materials { get { return objectTarget.materials; } set { objectTarget.materials = value; } }
        public virtual Texture2D atlasTexture { get { return objectTarget.atlasTexture; } set { objectTarget.atlasTexture = value; } }

        protected override void OnEnable()
        {
            base.OnEnable();

            objectTarget = target as VoxelObject;

            if (objectTarget != null)
            {
                baseCore = objectCore = new VoxelObjectCore(objectTarget);
                OnEnableInitializeSet();
            }
        }

        protected override void InspectorGUI()
        {
            base.InspectorGUI();

            InspectorGUI_Import();

            InspectorGUI_Object();

            InspectorGUI_Refresh();
        }

        protected virtual void InspectorGUI_Object()
        {
            #region Object
            if (!string.IsNullOrEmpty(baseTarget.voxelFilePath))
            {
                //Object
                baseTarget.edit_objectFoldout = EditorGUILayout.Foldout(baseTarget.edit_objectFoldout, "Object", guiStyleFoldoutBold);
                if (baseTarget.edit_objectFoldout)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    InspectorGUI_Object_Mesh();
                    InspectorGUI_Object_Material();
                    InspectorGUI_Object_Texture();
                    InspectorGUI_Object_HelpBox();
                    EditorGUILayout.EndVertical();
                }
            }
            #endregion
        }
        protected void TypeTitle(UnityEngine.Object o, string title)
        {
            if (o == null)
                EditorGUILayout.LabelField(title, guiStyleMagentaBold);
            else if (prefabEnable && !AssetDatabase.Contains(o))
                EditorGUILayout.LabelField(title, guiStyleRedBold);
            else
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
        protected virtual void InspectorGUI_Object_Mesh()
        {
            #region Mesh
            {
                TypeTitle(mesh, "Mesh");
                EditorGUI.indentLevel++;
                #region Mesh
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(mesh, typeof(Mesh), false);
                        EditorGUI.EndDisabledGroup();
                    }
                    if (mesh != null)
                    {
                        if (!AssetDatabase.Contains(mesh))
                        {
                            if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Create Mesh
                                string path = EditorUtility.SaveFilePanel("Save mesh", objectCore.GetDefaultPath(), string.Format("{0}_mesh.asset", baseTarget.gameObject.name), "asset");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    if (path.IndexOf(Application.dataPath) < 0)
                                    {
                                        SaveInsideAssetsFolderDisplayDialog();
                                    }
                                    else
                                    {
                                        UndoRecordObject("Save Mesh");
                                        path = path.Replace(Application.dataPath, "Assets");
                                        AssetDatabase.CreateAsset(Mesh.Instantiate(mesh), path);
                                        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                                        objectCore.SetRendererCompornent();
                                    }
                                }
                                #endregion
                            }
                        }
                        {
                            if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Reset Mesh
                                UndoRecordObject("Reset Mesh");
                                mesh = null;
                                Refresh();
                                #endregion
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                #region Generate Lightmap UVs
                {
                    EditorGUI.BeginChangeCheck();
                    var generateLightmapUVs = EditorGUILayout.Toggle("Generate Lightmap UVs", baseTarget.generateLightmapUVs);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UndoRecordObject("Inspector");
                        baseTarget.generateLightmapUVs = generateLightmapUVs;
                        Refresh();
                    }
                }
                #endregion
                #region Vertex Count
                {
                    EditorGUILayout.LabelField("Vertex Count", mesh != null ? mesh.vertexCount.ToString() : "");
                }
                #endregion
                EditorGUI.indentLevel--;
            }
            #endregion
        }
        protected virtual void InspectorGUI_Object_Material()
        {
            #region Material
            {
                #region Title
                {
                    if (materials == null || materials.Count == 0)
                        EditorGUILayout.LabelField("Material", guiStyleMagentaBold);
                    else if (prefabEnable)
                    {
                        bool contains = true;
                        for (int i = 0; i < materials.Count; i++)
                        {
                            if (materials[i] == null || !AssetDatabase.Contains(materials[i]))
                            {
                                contains = false;
                                break;
                            }
                        }
                        EditorGUILayout.LabelField("Material", contains ? EditorStyles.boldLabel : guiStyleRedBold);
                    }
                    else
                        EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
                }
                #endregion
                EditorGUI.indentLevel++;
                #region updateMeshRendererMaterials
                {
                    EditorGUI.BeginChangeCheck();
                    var updateMeshRendererMaterials = EditorGUILayout.Toggle("Update the Mesh Renderer Materials", baseTarget.updateMeshRendererMaterials);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UndoRecordObject("Inspector");
                        baseTarget.updateMeshRendererMaterials = updateMeshRendererMaterials;
                        baseCore.SetRendererCompornent();
                    }
                }
                #endregion
                if (materialList != null)
                {
                    materialList.DoLayoutList();
                }
                #region Configure Material
                if (baseTarget.materialData != null && baseTarget.materialData.Count > 1)
                {
                    EditorGUI.BeginDisabledGroup(baseTarget.voxelData == null);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Configure Material", baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material ? guiStyleBoldActiveButton : GUI.skin.button))
                    {
                        UndoRecordObject("Configure Material");
                        if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                        {
                            baseTarget.edit_configureMode = VoxelBase.Edit_configureMode.None;
                            AfterRefresh();
                        }
                        else
                        {
                            baseTarget.edit_configureMode = VoxelBase.Edit_configureMode.Material;
                            UpdateMaterialEnableMesh();
                        }
                        InternalEditorUtility.RepaintAllViews();
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    baseTarget.edit_configureMode = VoxelBase.Edit_configureMode.None;
                }
                #endregion
                EditorGUI.indentLevel--;
            }
            #endregion
        }
        protected virtual void InspectorGUI_Object_Texture()
        {
            #region Texture
            {
                TypeTitle(atlasTexture, "Texture");
                EditorGUI.indentLevel++;
                #region updateMaterialTexture
                {
                    EditorGUI.BeginChangeCheck();
                    var updateMaterialTexture = EditorGUILayout.Toggle("Update the Material Texture", baseTarget.updateMaterialTexture);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UndoRecordObject("Inspector");
                        baseTarget.updateMaterialTexture = updateMaterialTexture;
                        baseCore.SetRendererCompornent();
                    }
                }
                #endregion
                #region Texture
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(atlasTexture, typeof(Texture2D), false);
                        EditorGUI.EndDisabledGroup();
                    }
                    if (atlasTexture != null)
                    {
                        if (!AssetDatabase.Contains(atlasTexture))
                        {
                            if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Create Texture
                                string path = EditorUtility.SaveFilePanel("Save atlas texture", objectCore.GetDefaultPath(), string.Format("{0}_tex.png", baseTarget.gameObject.name), "png");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    if (path.IndexOf(Application.dataPath) < 0)
                                    {
                                        SaveInsideAssetsFolderDisplayDialog();
                                    }
                                    else
                                    {
                                        UndoRecordObject("Save Atlas Texture");
                                        File.WriteAllBytes(path, atlasTexture.EncodeToPNG());
                                        path = path.Replace(Application.dataPath, "Assets");
                                        AssetDatabase.ImportAsset(path);
                                        objectCore.SetTextureImporterSetting(path, atlasTexture);
                                        atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                                        objectCore.SetRendererCompornent();
                                    }
                                }
                                #endregion
                            }
                        }
                        {
                            if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Reset Texture
                                UndoRecordObject("Reset Atlas Texture");
                                atlasTexture = null;
                                Refresh();
                                #endregion
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                #region Generate Mip Maps
                if (atlasTexture != null && !AssetDatabase.Contains(atlasTexture))
                {
                    EditorGUI.BeginChangeCheck();
                    var generateMipMaps = EditorGUILayout.Toggle("Generate Mip Maps", baseTarget.generateMipMaps);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UndoRecordObject("Inspector");
                        baseTarget.generateMipMaps = generateMipMaps;
                        Refresh();
                    }
                }
                #endregion
                #region Texture Size
                {
                    EditorGUILayout.LabelField("Texture Size", atlasTexture != null ? string.Format("{0} x {1}", atlasTexture.width, atlasTexture.height) : "");
                }
                #endregion
                EditorGUI.indentLevel--;
            }
            #endregion
        }
        protected virtual void InspectorGUI_Object_HelpBox()
        {
            #region HelpBox
            {
                if (prefabEnable)
                {
                    bool materialsContains = materials != null;
                    if (materials != null)
                    {
                        for (int i = 0; i < materials.Count; i++)
                        {
                            if (materials[i] == null || !AssetDatabase.Contains(materials[i]))
                            {
                                materialsContains = false;
                                break;
                            }
                        }
                    }

                    if (!AssetDatabase.Contains(mesh) || !materialsContains || !AssetDatabase.Contains(atlasTexture))
                    {
                        List<string> helpList = new List<string>();
                        if (!AssetDatabase.Contains(mesh)) { helpList.Add("Mesh"); }
                        if (materials != null)
                        {
                            for (int i = 0; i < materials.Count; i++)
                            {
                                if (materials[i] == null || !AssetDatabase.Contains(materials[i]))
                                {
                                    helpList.Add("Material");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            helpList.Add("Material");
                        }
                        if (!AssetDatabase.Contains(atlasTexture)) { helpList.Add("Texture"); }
                        if (helpList.Count > 0)
                        {
                            EditorGUILayout.HelpBox(GetPrefabHelpStrings(helpList), MessageType.Error);
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.Space();
                            if (GUILayout.Button("Save All Unsaved Assets"))
                            {
                                SaveAllUnsavedAssets();
                            }
                            EditorGUILayout.Space();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                        }
                    }
                }
            }
            #endregion
        }

        protected override void DrawBaseMesh()
        {
            if (mesh != null)
            {
                editorCommon.unlitColorMaterial.color = new Color(0, 0, 0, 1f);
                editorCommon.unlitColorMaterial.SetPass(0);
                Graphics.DrawMeshNow(mesh, baseTarget.transform.localToWorldMatrix);
            }
        }

        protected override List<Material> GetMaterialListMaterials()
        {
            return materials;
        }

        protected virtual void SaveAllUnsavedAssets()
        {
            ContextSaveAllUnsavedAssets(new MenuCommand(baseTarget));
        }

        [MenuItem("CONTEXT/VoxelObject/Save All Unsaved Assets")]
        private static void ContextSaveAllUnsavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelObjectCore(objectTarget);

            var folder = EditorUtility.OpenFolderPanel("Save all", objectCore.GetDefaultPath(), null);
            if (string.IsNullOrEmpty(folder)) return;
            if (folder.IndexOf(Application.dataPath) < 0)
            {
                SaveInsideAssetsFolderDisplayDialog();
                return;
            }

            Undo.RecordObject(objectTarget, "Save All Unsaved Assets");

            #region Mesh
            if (objectTarget.mesh != null && !AssetDatabase.Contains(objectTarget.mesh))
            {
                var path = folder + "/" + string.Format("{0}_mesh.asset", objectTarget.gameObject.name);
                path = path.Replace(Application.dataPath, "Assets");
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CreateAsset(Mesh.Instantiate(objectTarget.mesh), path);
                objectTarget.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
            #endregion

            #region Material
            if (objectTarget.materials != null)
            {
                for (int index = 0; index < objectTarget.materials.Count; index++)
                {
                    if (objectTarget.materials[index] == null) continue;
                    if (AssetDatabase.Contains(objectTarget.materials[index])) continue;
                    var path = folder + "/" + string.Format("{0}_mat{1}.mat", objectTarget.gameObject.name, index);
                    path = path.Replace(Application.dataPath, "Assets");
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(Material.Instantiate(objectTarget.materials[index]), path);
                    objectTarget.materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            #endregion

            #region Texture
            if (objectTarget.atlasTexture != null && !AssetDatabase.Contains(objectTarget.atlasTexture))
            {
                var path = folder + "/" + string.Format("{0}_tex.png", objectTarget.gameObject.name);
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path.Replace(Application.dataPath, "Assets"));
                    path = (Application.dataPath + path).Replace("AssetsAssets", "Assets");
                }
                File.WriteAllBytes(path, objectTarget.atlasTexture.EncodeToPNG());
                path = path.Replace(Application.dataPath, "Assets");
                AssetDatabase.ImportAsset(path);
                objectCore.SetTextureImporterSetting(path, objectTarget.atlasTexture);
                objectTarget.atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            #endregion

            objectCore.SetRendererCompornent();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelObject/Reset All Assets")]
        private static void ResetAllSavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelObjectCore(objectTarget);

            Undo.RecordObject(objectTarget, "Reset All Assets");

            #region Mesh
            objectTarget.mesh = null;
            #endregion

            #region Material
            objectTarget.materials = null;
            #endregion

            #region Texture
            objectTarget.atlasTexture = null;
            #endregion

            objectCore.ReCreate();
            InternalEditorUtility.RepaintAllViews();
        }
    }
}

