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
    [CustomEditor(typeof(VoxelChunksObject))]
    public class VoxelChunksObjectEditor : VoxelBaseEditor
    {
        public VoxelChunksObject objectTarget { get; protected set; }
        public VoxelChunksObjectCore objectCore { get; protected set; }

        #region strings
        private static string[] SplitModeNormalStrings =
        {
            VoxelChunksObject.SplitMode.ChunkSize.ToString(),
        };
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();

            objectTarget = target as VoxelChunksObject;
            if (objectTarget == null) return;
            baseCore = objectCore = new VoxelChunksObjectCore(objectTarget);
            OnEnableInitializeSet();
        }

        protected override void InspectorGUI()
        {
            base.InspectorGUI();

            Action<UnityEngine.Object, string> TypeTitle = (o, title) =>
            {
                if (o == null)
                    EditorGUILayout.LabelField(title, guiStyleMagentaBold);
                else if (prefabEnable && !AssetDatabase.Contains(o))
                    EditorGUILayout.LabelField(title, guiStyleRedBold);
                else
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            };

            InspectorGUI_Import();

            #region Object
            if (!string.IsNullOrEmpty(baseTarget.voxelFilePath))
            {
                //Object
                baseTarget.edit_objectFoldout = EditorGUILayout.Foldout(baseTarget.edit_objectFoldout, "Object", guiStyleFoldoutBold);
                if (baseTarget.edit_objectFoldout)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    #region Mesh
                    {
                        EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
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
                        EditorGUI.indentLevel--;
                    }
                    #endregion
                    #region Material
                    {
                        if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine)
                        {
                            if (objectTarget.materials == null || objectTarget.materials.Count == 0)
                                EditorGUILayout.LabelField("Material", guiStyleMagentaBold);
                            else if (prefabEnable)
                            {
                                bool contains = true;
                                for (int i = 0; i < objectTarget.materials.Count; i++)
                                {
                                    if (objectTarget.materials[i] == null || !AssetDatabase.Contains(objectTarget.materials[i]))
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
                        else
                        {
                            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
                        }
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
                        #region Material Mode
                        {
                            EditorGUI.BeginChangeCheck();
                            var materialMode = (VoxelChunksObject.MaterialMode)EditorGUILayout.EnumPopup("Material Mode", objectTarget.materialMode);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector");
                                {
                                    var chunkObjects = objectTarget.chunks;
                                    if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine)
                                    {
                                        objectTarget.materials = null;
                                        objectTarget.atlasTexture = null;
                                    }
                                    else if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Individual)
                                    {
                                        for (int i = 0; i < chunkObjects.Length; i++)
                                        {
                                            if (chunkObjects[i] == null) continue;
                                            chunkObjects[i].materials = null;
                                            chunkObjects[i].atlasTexture = null;
                                        }
                                    }
                                }
                                objectTarget.materialMode = materialMode;
                                Refresh();
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
                    #region Texture
                    {
                        if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine)
                            TypeTitle(objectTarget.atlasTexture, "Texture");
                        else
                            EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
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
                        if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.ObjectField(objectTarget.atlasTexture, typeof(Texture2D), false);
                                EditorGUI.EndDisabledGroup();
                            }
                            if (objectTarget.atlasTexture != null)
                            {
                                if (!AssetDatabase.Contains(objectTarget.atlasTexture))
                                {
                                    if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                    {
                                        #region Create Texture
                                        string path = EditorUtility.SaveFilePanel("Save atlas texture", baseCore.GetDefaultPath(), string.Format("{0}_tex.png", baseTarget.gameObject.name), "png");
                                        if (!string.IsNullOrEmpty(path))
                                        {
                                            if (path.IndexOf(Application.dataPath) < 0)
                                            {
                                                SaveInsideAssetsFolderDisplayDialog();
                                            }
                                            else
                                            {
                                                UndoRecordObject("Save Atlas Texture");
                                                File.WriteAllBytes(path, objectTarget.atlasTexture.EncodeToPNG());
                                                path = path.Replace(Application.dataPath, "Assets");
                                                AssetDatabase.ImportAsset(path);
                                                objectCore.SetTextureImporterSetting(path, objectTarget.atlasTexture);
                                                objectTarget.atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
                                        objectTarget.atlasTexture = null;
                                        Refresh();
                                        #endregion
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
                        #region Generate Mip Maps
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
                        if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine)
                        {
                            EditorGUILayout.LabelField("Texture Size", objectTarget.atlasTexture != null ? string.Format("{0} x {1}", objectTarget.atlasTexture.width, objectTarget.atlasTexture.height) : "");
                        }
                        #endregion
                        EditorGUI.indentLevel--;
                    }
                    #endregion
                    #region HelpBox
                    {
                        if (prefabEnable)
                        {
                            var chunkObjects = objectTarget.chunks;

                            HashSet<string> helpList = new HashSet<string>();
                            if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine)
                            {
                                for (int i = 0; i < chunkObjects.Length; i++)
                                {
                                    if (chunkObjects[i] == null || !AssetDatabase.Contains(chunkObjects[i].mesh))
                                    {
                                        helpList.Add("Mesh");
                                        break;
                                    }
                                }
                                if (objectTarget.materials != null)
                                {
                                    for (int i = 0; i < objectTarget.materials.Count; i++)
                                    {
                                        if (objectTarget.materials[i] == null || !AssetDatabase.Contains(objectTarget.materials[i]))
                                        {
                                            helpList.Add("Material");
                                            break;
                                        }
                                    }
                                }
                                if (!AssetDatabase.Contains(objectTarget.atlasTexture))
                                    helpList.Add("Texture");
                            }
                            else if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Individual)
                            {
                                for (int i = 0; i < chunkObjects.Length; i++)
                                {
                                    if (chunkObjects[i] == null || !AssetDatabase.Contains(chunkObjects[i].mesh))
                                        helpList.Add("Mesh");

                                    if (chunkObjects[i] != null && chunkObjects[i].materials != null)
                                    {
                                        for (int j = 0; j < chunkObjects[i].materials.Count; j++)
                                        {
                                            if (chunkObjects[i].materials[j] == null || !AssetDatabase.Contains(chunkObjects[i].materials[j]))
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
                                    if (chunkObjects[i] == null || !AssetDatabase.Contains(chunkObjects[i].atlasTexture))
                                        helpList.Add("Texture");
                                }
                            }
                            else
                            {
                                Assert.IsTrue(false);
                            }
                            if (helpList.Count > 0)
                            {
                                EditorGUILayout.HelpBox(GetPrefabHelpStrings(new List<string>(helpList)), MessageType.Error);
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.Space();
                                if (GUILayout.Button("Save All Unsaved Assets"))
                                {
                                    ContextSaveAllUnsavedAssets(new MenuCommand(baseTarget));
                                }
                                EditorGUILayout.Space();
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.Space();
                            }
                        }
                    }
                    #endregion
                    EditorGUILayout.EndVertical();
                }
            }
            #endregion

            InspectorGUI_Refresh();
        }
        protected override void UndoRecordObject(string text, bool reset = false)
        {
            base.UndoRecordObject(text);

            for (int i = 0; i < objectTarget.chunks.Length; i++)
            {
                if (objectTarget.chunks[i] == null) continue;
                Undo.RecordObject(objectTarget.chunks[i], text);
            }

            if (reset)
            {
                if (objectTarget.splitMode == VoxelChunksObject.SplitMode.QubicleMatrix)
                {
                    objectTarget.splitMode = VoxelChunksObject.SplitMode.ChunkSize;
                }

                objectCore.RemoveAllChunk();
            }
        }
        protected override void InspectorGUI_ImportSettingsExtra()
        {
            #region Split Mode
            if (objectTarget.fileType == VoxelBase.FileType.qb)
            {
                EditorGUI.BeginChangeCheck();
                var splitMode = (VoxelChunksObject.SplitMode)EditorGUILayout.EnumPopup("Split Mode", objectTarget.splitMode);
                if (EditorGUI.EndChangeCheck())
                {
                    UndoRecordObject("Inspector", true);
                    objectTarget.splitMode = splitMode;
                    Refresh();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var splitMode = (VoxelChunksObject.SplitMode)EditorGUILayout.Popup("Split Mode", (int)objectTarget.splitMode, SplitModeNormalStrings);
                if (EditorGUI.EndChangeCheck())
                {
                    UndoRecordObject("Inspector", true);
                    objectTarget.splitMode = splitMode;
                    Refresh();
                }
            }
            #endregion
            {
                EditorGUI.indentLevel++;
                #region Chunk Size
                if (objectTarget.splitMode == VoxelChunksObject.SplitMode.ChunkSize)
                {
                    EditorGUI.BeginChangeCheck();
                    var chunkSize = EditorGUILayout.Vector3Field("Chunk Size", new Vector3(objectTarget.edit_chunkSize.x, objectTarget.edit_chunkSize.y, objectTarget.edit_chunkSize.z));
                    if (EditorGUI.EndChangeCheck())
                    {
                        UndoRecordObject("Inspector");
                        objectTarget.edit_chunkSize.x = Mathf.RoundToInt(chunkSize.x);
                        objectTarget.edit_chunkSize.y = Mathf.RoundToInt(chunkSize.y);
                        objectTarget.edit_chunkSize.z = Mathf.RoundToInt(chunkSize.z);
                    }
                    if (objectTarget.chunkSize != objectTarget.edit_chunkSize)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        if (GUILayout.Button("Revert", GUILayout.Width(64f)))
                        {
                            UndoRecordObject("Inspector");
                            objectTarget.edit_chunkSize = objectTarget.chunkSize;
                        }
                        if (GUILayout.Button("Apply", GUILayout.Width(64f)))
                        {
                            UndoRecordObject("Inspector", true);
                            objectTarget.chunkSize = objectTarget.edit_chunkSize;
                            Refresh();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                #endregion
                EditorGUI.indentLevel--;
            }
            #region Create contact faces of chunks
            {
                EditorGUI.BeginChangeCheck();
                var createContactChunkFaces = EditorGUILayout.Toggle("Create contact faces of chunks", objectTarget.createContactChunkFaces);
                if (EditorGUI.EndChangeCheck())
                {
                    UndoRecordObject("Inspector");
                    objectTarget.createContactChunkFaces = createContactChunkFaces;
                    Refresh();
                }
            }
            #endregion
        }
        
        protected override void DrawBaseMesh()
        {
            var chunkObjects = objectTarget.chunks;
            for (int i = 0; i < chunkObjects.Length; i++)
            {
                if (chunkObjects[i] == null || chunkObjects[i].mesh == null) continue;
                editorCommon.unlitColorMaterial.color = new Color(0, 0, 0, 1f);
                editorCommon.unlitColorMaterial.SetPass(0);
                Graphics.DrawMeshNow(chunkObjects[i].mesh, chunkObjects[i].transform.localToWorldMatrix);
            }
        }

        protected override List<Material> GetMaterialListMaterials()
        {
            return objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine ? objectTarget.materials : null;
        }

        [MenuItem("CONTEXT/VoxelChunksObject/Save All Unsaved Assets")]
        private static void ContextSaveAllUnsavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelChunksObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelChunksObjectCore(objectTarget);

            var folder = EditorUtility.OpenFolderPanel("Save all", objectCore.GetDefaultPath(), null);
            if (string.IsNullOrEmpty(folder)) return;
            if (folder.IndexOf(Application.dataPath) < 0)
            {
                SaveInsideAssetsFolderDisplayDialog();
                return;
            }

            Undo.RecordObject(objectTarget, "Save All Unsaved Assets");
            if (objectTarget.chunks != null)
                Undo.RecordObjects(objectTarget.chunks, "Save All Unsaved Assets");

            if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Combine)
            {
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

                if(objectTarget.chunks != null)
                {
                    for (int i = 0; i < objectTarget.chunks.Length; i++)
                    {
                        if (objectTarget.chunks[i] == null) continue;
                        #region Mesh
                        if (objectTarget.chunks[i].mesh != null && !AssetDatabase.Contains(objectTarget.chunks[i].mesh))
                        {
                            var path = folder + "/" + string.Format("{0}_{1}_mesh.asset", objectTarget.gameObject.name, objectTarget.chunks[i].chunkName);
                            path = path.Replace(Application.dataPath, "Assets");
                            path = AssetDatabase.GenerateUniqueAssetPath(path);
                            AssetDatabase.CreateAsset(Mesh.Instantiate(objectTarget.chunks[i].mesh), path);
                            objectTarget.chunks[i].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        }
                        #endregion
                    }
                }
            }
            else if (objectTarget.materialMode == VoxelChunksObject.MaterialMode.Individual)
            {
                if (objectTarget.chunks != null)
                {
                    for (int i = 0; i < objectTarget.chunks.Length; i++)
                    {
                        if (objectTarget.chunks[i] == null) continue;
                        #region Mesh
                        if (objectTarget.chunks[i].mesh != null && !AssetDatabase.Contains(objectTarget.chunks[i].mesh))
                        {
                            var path = folder + "/" + string.Format("{0}_{1}_mesh.asset", objectTarget.gameObject.name, objectTarget.chunks[i].chunkName);
                            path = path.Replace(Application.dataPath, "Assets");
                            path = AssetDatabase.GenerateUniqueAssetPath(path);
                            AssetDatabase.CreateAsset(Mesh.Instantiate(objectTarget.chunks[i].mesh), path);
                            objectTarget.chunks[i].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        }
                        #endregion

                        #region Material
                        if (objectTarget.chunks[i].materials != null)
                        {
                            for (int index = 0; index < objectTarget.chunks[i].materials.Count; index++)
                            {
                                if (objectTarget.chunks[i].materials[index] == null) continue;
                                if (AssetDatabase.Contains(objectTarget.chunks[i].materials[index])) continue;
                                var path = folder + "/" + string.Format("{0}_{1}_mat{2}.mat", objectTarget.gameObject.name, objectTarget.chunks[i].chunkName, index);
                                path = path.Replace(Application.dataPath, "Assets");
                                path = AssetDatabase.GenerateUniqueAssetPath(path);
                                AssetDatabase.CreateAsset(Material.Instantiate(objectTarget.chunks[i].materials[index]), path);
                                objectTarget.chunks[i].materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                            }
                        }
                        #endregion

                        #region Texture
                        if (objectTarget.chunks[i].atlasTexture != null && !AssetDatabase.Contains(objectTarget.chunks[i].atlasTexture))
                        {
                            var path = folder + "/" + string.Format("{0}_{1}_tex.png", objectTarget.gameObject.name, objectTarget.chunks[i].chunkName);
                            {
                                path = AssetDatabase.GenerateUniqueAssetPath(path.Replace(Application.dataPath, "Assets"));
                                path = (Application.dataPath + path).Replace("AssetsAssets", "Assets");
                            }
                            File.WriteAllBytes(path, objectTarget.chunks[i].atlasTexture.EncodeToPNG());
                            path = path.Replace(Application.dataPath, "Assets");
                            AssetDatabase.ImportAsset(path);
                            objectCore.SetTextureImporterSetting(path, objectTarget.chunks[i].atlasTexture);
                            objectTarget.chunks[i].atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        }
                        #endregion
                    }
                }
            }
            else
            {
                Assert.IsTrue(false);
            }

            objectCore.SetRendererCompornent();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelChunksObject/Reset All Assets")]
        private static void ResetAllSavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelChunksObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelChunksObjectCore(objectTarget);

            Undo.RecordObject(objectTarget, "Reset All Assets");
            if (objectTarget.chunks != null)
                Undo.RecordObjects(objectTarget.chunks, "Save All Unsaved Assets");

            #region Material
            objectTarget.materials = null;
            #endregion

            #region Texture
            objectTarget.atlasTexture = null;
            #endregion

            if (objectTarget.chunks != null)
            {
                for (int i = 0; i < objectTarget.chunks.Length; i++)
                {
                    if (objectTarget.chunks[i] == null) continue;
                    objectTarget.chunks[i].mesh = null;
                    objectTarget.chunks[i].materials = null;
                    objectTarget.chunks[i].atlasTexture = null;
                }
            }

            objectCore.ReCreate();
            InternalEditorUtility.RepaintAllViews();
        }
    }
}

