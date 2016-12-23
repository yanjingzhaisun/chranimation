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
    [CustomEditor(typeof(VoxelChunksObjectExplosion))]
    public class VoxelChunksObjectExplosionEditor : VoxelBaseExplosionEditor
    {
        public VoxelChunksObjectExplosion explosionObject { get; protected set; }
        public VoxelChunksObjectExplosionCore explosionObjectCore { get; protected set; }

        protected override void OnEnable()
        {
            base.OnEnable();

            explosionBase = explosionObject = target as VoxelChunksObjectExplosion;
            if (explosionObject == null) return;
            explosionCore = explosionObjectCore = new VoxelChunksObjectExplosionCore(explosionObject);
        }

        protected override void Inspector_MeshMaterial()
        {
            #region Material
            if (explosionObjectCore.voxelObject.materialMode == VoxelChunksObject.MaterialMode.Combine)
            {
                if (explosionObject.materials != null)
                {
                    if (prefabEnable)
                    {
                        bool contains = true;
                        for (int i = 0; i < explosionObject.materials.Count; i++)
                        {
                            if (explosionObject.materials[i] == null || !AssetDatabase.Contains(explosionObject.materials[i]))
                                contains = false;
                        }
                        EditorGUILayout.LabelField("Material", contains ? EditorStyles.boldLabel : guiStyleRedBold);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Material", guiStyleMagentaBold);
                }
                {
                    EditorGUI.indentLevel++;
                    {
                        if (explosionObject.materials != null)
                        {
                            for (int i = 0; i < explosionObject.materials.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField(explosionObject.materials[i], typeof(Material), false);
                                    EditorGUI.EndDisabledGroup();
                                }
                                if (explosionObject.materials[i] != null)
                                {
                                    if (!AssetDatabase.Contains(explosionObject.materials[i]))
                                    {
                                        if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                        {
                                            #region Create Material
                                            string path = EditorUtility.SaveFilePanel("Save material", explosionCore.voxelBaseCore.GetDefaultPath(), string.Format("{0}_explosion_mat{1}.mat", explosionObject.gameObject.name, i), "mat");
                                            if (!string.IsNullOrEmpty(path))
                                            {
                                                if (path.IndexOf(Application.dataPath) < 0)
                                                {
                                                    SaveInsideAssetsFolderDisplayDialog();
                                                }
                                                else
                                                {
                                                    Undo.RecordObject(explosionObject, "Save Material");
                                                    path = path.Replace(Application.dataPath, "Assets");
                                                    AssetDatabase.CreateAsset(Material.Instantiate(explosionObject.materials[i]), path);
                                                    explosionObject.materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                                                }
                                            }

                                            #endregion
                                        }
                                    }
                                    {
                                        if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                                        {
                                            #region Reset Material
                                            Undo.RecordObject(explosionObject, "Reset Material");
                                            explosionObject.materials[i] = null;
                                            explosionCore.Generate();
                                            #endregion
                                        }
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            #endregion

            #region HelpBox
            {
                if (prefabEnable)
                {
                    List<string> helpList = new List<string>();
                    {
                        for (int i = 0; i < explosionObject.chunksExplosion.Length; i++)
                        {
                            if (explosionObject.chunksExplosion[i] == null) continue;
                            if (explosionObject.chunksExplosion[i].meshes == null)
                            {
                                helpList.Add("Mesh");
                                break;
                            }
                            bool done = false;
                            for (int j = 0; j < explosionObject.chunksExplosion[i].meshes.Count; j++)
                            {
                                if (explosionObject.chunksExplosion[i].meshes[j] == null || explosionObject.chunksExplosion[i].meshes[j].mesh == null || !AssetDatabase.Contains(explosionObject.chunksExplosion[i].meshes[j].mesh))
                                {
                                    helpList.Add("Mesh");
                                    done = true;
                                    break;
                                }
                            }
                            if (done) break;
                        }
                        if (explosionObjectCore.voxelObject.materialMode == VoxelChunksObject.MaterialMode.Combine)
                        {
                            if (explosionObject.materials == null)
                            {
                                helpList.Add("Material");
                            }
                            else
                            {
                                for (int i = 0; i < explosionObject.materials.Count; i++)
                                {
                                    if (explosionObject.materials[i] == null || !AssetDatabase.Contains(explosionObject.materials[i]))
                                    {
                                        helpList.Add("Material");
                                        break;
                                    }
                                }
                            }
                        }
                        else if (explosionObjectCore.voxelObject.materialMode == VoxelChunksObject.MaterialMode.Individual)
                        {
                            for (int i = 0; i < explosionObject.chunksExplosion.Length; i++)
                            {
                                if (explosionObject.chunksExplosion[i] == null) continue;
                                if (explosionObject.chunksExplosion[i].materials == null)
                                {
                                    helpList.Add("Material");
                                    break;
                                }
                                bool done = false;
                                for (int j = 0; j < explosionObject.chunksExplosion[i].materials.Count; j++)
                                {
                                    if (explosionObject.chunksExplosion[i].materials[j] == null || !AssetDatabase.Contains(explosionObject.chunksExplosion[i].materials[j]))
                                    {
                                        helpList.Add("Material");
                                        done = true;
                                        break;
                                    }
                                }
                                if (done) break;
                            }
                        }
                    }
                    if (helpList.Count > 0)
                    {
                        EditorGUILayout.HelpBox(GetPrefabHelpStrings(helpList), MessageType.Error);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space();
                        if (GUILayout.Button("Save All Unsaved Assets"))
                        {
                            ContextSaveAllUnsavedAssets(new MenuCommand(explosionBase));
                        }
                        EditorGUILayout.Space();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                    }
                }
            }
            #endregion
        }

        [MenuItem("CONTEXT/VoxelChunksObjectExplosion/Save All Unsaved Assets")]
        private static void ContextSaveAllUnsavedAssets(MenuCommand menuCommand)
        {
            var explosionObject = menuCommand.context as VoxelChunksObjectExplosion;
            if (explosionObject == null) return;

            var explosionCore = new VoxelChunksObjectExplosionCore(explosionObject);

            var folder = EditorUtility.OpenFolderPanel("Save all", explosionCore.voxelBaseCore.GetDefaultPath(), null);
            if (string.IsNullOrEmpty(folder)) return;
            if (folder.IndexOf(Application.dataPath) < 0)
            {
                SaveInsideAssetsFolderDisplayDialog();
                return;
            }

            Undo.RecordObject(explosionObject, "Save All Unsaved Assets");
            if (explosionObject.chunksExplosion != null)
                Undo.RecordObjects(explosionObject.chunksExplosion, "Save All Unsaved Assets");

            if (explosionObject.materialMode == VoxelChunksObject.MaterialMode.Combine)
            {
                if (explosionObject.chunksExplosion != null)
                {
                    for (int i = 0; i < explosionObject.chunksExplosion.Length; i++)
                    {
                        if (explosionObject.chunksExplosion[i] == null) continue;
                        #region Mesh
                        for (int j = 0; j < explosionObject.chunksExplosion[i].meshes.Count; j++)
                        {
                            if (explosionObject.chunksExplosion[i].meshes[j] != null && explosionObject.chunksExplosion[i].meshes[j].mesh != null && !AssetDatabase.Contains(explosionObject.chunksExplosion[i].meshes[j].mesh))
                            {
                                var chunkObject = explosionObject.chunksExplosion[i].GetComponent<VoxelChunksObjectChunk>();
                                if (chunkObject == null) continue;
                                var path = folder + "/" + string.Format("{0}_{1}_explosion_mesh{2}.asset", explosionObject.gameObject.name, chunkObject.chunkName, j);
                                path = path.Replace(Application.dataPath, "Assets");
                                AssetDatabase.CreateAsset(Mesh.Instantiate(explosionObject.chunksExplosion[i].meshes[j].mesh), path);
                                explosionObject.chunksExplosion[i].meshes[j].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                            }
                        }
                        #endregion
                    }
                }

                #region Material
                if (explosionObject.materials != null)
                {
                    for (int index = 0; index < explosionObject.materials.Count; index++)
                    {
                        if (explosionObject.materials[index] == null) continue;
                        if (AssetDatabase.Contains(explosionObject.materials[index])) continue;
                        var path = folder + "/" + string.Format("{0}_explosion_mat{1}.mat", explosionObject.gameObject.name, index);
                        path = path.Replace(Application.dataPath, "Assets");
                        AssetDatabase.CreateAsset(Material.Instantiate(explosionObject.materials[index]), path);
                        explosionObject.materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                }
                #endregion

            }
            else if (explosionObject.materialMode == VoxelChunksObject.MaterialMode.Individual)
            {
                if (explosionObject.chunksExplosion != null)
                {
                    for (int i = 0; i < explosionObject.chunksExplosion.Length; i++)
                    {
                        if (explosionObject.chunksExplosion[i] == null) continue;
                        var chunkObject = explosionObject.chunksExplosion[i].GetComponent<VoxelChunksObjectChunk>();
                        if (chunkObject == null) continue;
                        #region Mesh
                        for (int j = 0; j < explosionObject.chunksExplosion[i].meshes.Count; j++)
                        {
                            if (explosionObject.chunksExplosion[i].meshes[j] != null && explosionObject.chunksExplosion[i].meshes[j].mesh != null && !AssetDatabase.Contains(explosionObject.chunksExplosion[i].meshes[j].mesh))
                            {
                                var path = folder + "/" + string.Format("{0}_{1}_explosion_mesh{2}.asset", explosionObject.gameObject.name, chunkObject.chunkName, j);
                                path = path.Replace(Application.dataPath, "Assets");
                                AssetDatabase.CreateAsset(Mesh.Instantiate(explosionObject.chunksExplosion[i].meshes[j].mesh), path);
                                explosionObject.chunksExplosion[i].meshes[j].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                            }
                        }
                        #endregion

                        #region Material
                        if (explosionObject.chunksExplosion[i].materials != null)
                        {
                            for (int index = 0; index < explosionObject.chunksExplosion[i].materials.Count; index++)
                            {
                                if (explosionObject.chunksExplosion[i].materials[index] == null) continue;
                                if (AssetDatabase.Contains(explosionObject.chunksExplosion[i].materials[index])) continue;
                                var path = folder + "/" + string.Format("{0}_{1}_explosion_mat{2}.mat", explosionObject.gameObject.name, chunkObject.chunkName, index);
                                path = path.Replace(Application.dataPath, "Assets");
                                AssetDatabase.CreateAsset(Material.Instantiate(explosionObject.chunksExplosion[i].materials[index]), path);
                                explosionObject.chunksExplosion[i].materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                            }
                        }
                        #endregion
                    }
                }
            }
            else
            {
                Assert.IsTrue(false);
            }

            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelChunksObjectExplosion/Reset All Assets")]
        private static void ResetAllSavedAssets(MenuCommand menuCommand)
        {
            var explosionObject = menuCommand.context as VoxelChunksObjectExplosion;
            if (explosionObject == null) return;

            var explosionCore = new VoxelChunksObjectExplosionCore(explosionObject);

            Undo.RecordObject(explosionObject, "Reset All Assets");
            if (explosionObject.chunksExplosion != null)
                Undo.RecordObjects(explosionObject.chunksExplosion, "Reset All Assets");

            #region Mesh
            if (explosionObject.chunksExplosion != null)
            {
                for (int i = 0; i < explosionObject.chunksExplosion.Length; i++)
                {
                    if (explosionObject.chunksExplosion[i] == null) continue;
                    explosionObject.chunksExplosion[i].meshes = null;
                    explosionObject.chunksExplosion[i].materials = null;
                }
            }
            #endregion

            #region Material
            explosionObject.materials = null;
            #endregion

            explosionCore.Generate();
            InternalEditorUtility.RepaintAllViews();
        }
    }
}

