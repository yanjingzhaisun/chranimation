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
    [CustomEditor(typeof(VoxelFrameAnimationObject))]
    public class VoxelFrameAnimationObjectEditor : VoxelBaseEditor
    {
        public VoxelFrameAnimationObject objectTarget { get; protected set; }
        public VoxelFrameAnimationObjectCore objectCore { get; protected set; }

        protected ReorderableList frameList;

        protected override void OnEnable()
        {
            base.OnEnable();

            objectTarget = target as VoxelFrameAnimationObject;
            if (objectTarget == null) return;
            baseCore = objectCore = new VoxelFrameAnimationObjectCore(objectTarget);
            OnEnableInitializeSet();

            editorCommon.InitializeIcon();
            UpdateFrameList();

            AnimationUtility.onCurveWasModified -= EditorOnCurveWasModified;
            AnimationUtility.onCurveWasModified += EditorOnCurveWasModified;
        }
        protected override void OnDisable()
        {
            base.OnDisable();

            AnimationUtility.onCurveWasModified -= EditorOnCurveWasModified;
        }

        protected override void InspectorGUI()
        {
            base.InspectorGUI();

            Event e = Event.current;

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
                    #region Texture
                    {
                        TypeTitle(objectTarget.atlasTexture, "Texture");
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
                            HashSet<string> helpList = new HashSet<string>();
                            {
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
                                else
                                {
                                    helpList.Add("Material");
                                }
                                if (!AssetDatabase.Contains(objectTarget.atlasTexture))
                                    helpList.Add("Texture");
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

            #region Animation
            if (!string.IsNullOrEmpty(baseTarget.voxelFilePath))
            {
                objectTarget.edit_animationFoldout = EditorGUILayout.Foldout(objectTarget.edit_animationFoldout, "Animation", guiStyleFoldoutBold);
                if (objectTarget.edit_animationFoldout)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            {
                                if (objectTarget.frames == null || objectTarget.frames.Count == 0)
                                    EditorGUILayout.LabelField("Frame", guiStyleMagentaBold);
                                else
                                {
                                    bool contains = true;
                                    for (int i = 0; i < objectTarget.frames.Count; i++)
                                    {
                                        if (objectTarget.frames[i] == null || objectTarget.frames[i].mesh == null || !AssetDatabase.Contains(objectTarget.frames[i].mesh))
                                        {
                                            contains = false;
                                            break;
                                        }
                                    }
                                    EditorGUILayout.LabelField("Frame", contains ? EditorStyles.boldLabel : guiStyleRedBold);
                                }
                            }

                            Action<string> OpenFile = (path) =>
                            {
                                if (!baseCore.IsEnableFile(path))
                                    return;
                                UndoRecordObject("Open Voxel File", true);
                                UnityEngine.Object obj = null;
                                if (path.Contains(Application.dataPath))
                                {
                                    var assetPath = path.Replace(Application.dataPath, "Assets");
                                    var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                                    obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                                    if (obj != null)
                                    {
                                        bool done = false;
                                        if (obj is Texture2D)
                                        {
                                            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                                            if (importer != null && importer.spriteImportMode == SpriteImportMode.Multiple)
                                            {
                                                for (int j = 0; j < sprites.Length; j++)
                                                {
                                                    if (sprites[j] is Sprite)
                                                        objectTarget.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path, voxelFileObject = sprites[j] });
                                                }
                                                done = true;
                                            }
                                        }
                                        if (!done)
                                        {
                                            objectTarget.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path, voxelFileObject = obj });
                                        }
                                    }
                                }
                                else
                                {
                                    objectTarget.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path, voxelFileObject = obj });
                                }
                                objectCore.ReCreate();
                            };

                            var rect = GUILayoutUtility.GetRect(new GUIContent("Open"), guiStyleDropDown, GUILayout.Width(64));
                            if (GUI.Button(rect, "Open", guiStyleDropDown))
                            {
                                GenericMenu menu = new GenericMenu();
                                #region vox
                                menu.AddItem(new GUIContent("MagicaVoxel (*.vox)"), false, () =>
                                {
                                    var path = EditorUtility.OpenFilePanel("Open MagicaVoxel File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "vox");
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        OpenFile(path);
                                    }
                                });
                                #endregion
                                #region qb
                                menu.AddItem(new GUIContent("Qubicle Binary (*.qb)"), false, () =>
                                {
                                    var path = EditorUtility.OpenFilePanel("Open Qubicle Binary File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "qb");
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        OpenFile(path);
                                    }
                                });
                                #endregion
                                #region png
                                menu.AddItem(new GUIContent("Pixel Art (*.png)"), false, () =>
                                {
                                    var path = EditorUtility.OpenFilePanel("Open Pixel Art File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "png");
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        OpenFile(path);
                                    }
                                });
                                #endregion
                                menu.ShowAsContext();
                            }
                            #region Drag&Drop
                            {
                                switch (e.type)
                                {
                                case EventType.DragUpdated:
                                case EventType.DragPerform:
                                    if (!rect.Contains(e.mousePosition)) break;
                                    if (DragAndDrop.paths.Length == 0) break;
                                    DragAndDrop.AcceptDrag();
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                                    if (e.type == EventType.DragPerform)
                                    {
                                        UndoRecordObject("Open Voxel File", true);
                                        if (DragAndDrop.objectReferences.Length > 0)
                                        {
                                            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                                            {
                                                var obj = DragAndDrop.objectReferences[i];
                                                var assetPath = AssetDatabase.GetAssetPath(obj);
                                                var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                                                string path = assetPath;
                                                if (!baseCore.IsEnableFile(path))
                                                    continue;
                                                if (Path.GetPathRoot(path) == "")
                                                    path = Application.dataPath + assetPath.Remove(0, "Assets".Length);
                                                bool done = false;
                                                if (obj is Texture2D)
                                                {
                                                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                                                    if (importer != null && importer.spriteImportMode == SpriteImportMode.Multiple)
                                                    {
                                                        for (int j = 0; j < sprites.Length; j++)
                                                        {
                                                            if (sprites[j] is Sprite)
                                                                objectTarget.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path, voxelFileObject = sprites[j] });
                                                        }
                                                        done = true;
                                                    }
                                                }
                                                if (!done)
                                                {
                                                    objectTarget.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path, voxelFileObject = obj });
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int i = 0; i < DragAndDrop.paths.Length; i++)
                                            {
                                                string path = DragAndDrop.paths[i];
                                                if (Path.GetPathRoot(path) == "")
                                                    path = Application.dataPath + DragAndDrop.paths[i].Remove(0, "Assets".Length);
                                                if (baseCore.IsEnableFile(path))
                                                    objectTarget.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path });
                                            }
                                        }
                                        objectCore.ReCreate();
                                        e.Use();
                                    }
                                    break;
                                }
                            }
                            #endregion
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    {
                        EditorGUI.indentLevel++;
                        {
                            if (frameList != null)
                            {
                                frameList.DoLayoutList();
                            }
                            #region Frame List Window
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.Space();
                                if (GUILayout.Button("Frame List Window", VoxelFrameAnimationListWindow.instance == null ? GUI.skin.button : guiStyleBoldActiveButton))
                                {
                                    if (VoxelFrameAnimationListWindow.instance == null)
                                    {
                                        VoxelFrameAnimationListWindow.Create(objectTarget);
                                        VoxelFrameAnimationListWindow.instance.frameIndexChanged += () =>
                                        {
                                            if (objectTarget.edit_frameEnable)
                                                frameList.index = objectTarget.edit_frameIndex;
                                            else
                                                objectTarget.edit_frameIndex = -1;

                                            objectTarget.Edit_SetFrameCurrentVoxelMaterialData();
                                            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                                                UpdateMaterialEnableMesh();
                                            objectCore.SetCurrentMesh();
                                        };
                                        VoxelFrameAnimationListWindow.instance.previewCameraModeChanged += () =>
                                        {
                                            objectCore.ClearFramesIcon();
                                        };
                                    }
                                    else
                                    {
                                        VoxelFrameAnimationListWindow.instance.Close();
                                    }
                                }
                                EditorGUILayout.Space();
                                EditorGUILayout.EndHorizontal();
                            }
                            #endregion
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.Space();
                    #region HelpBox
                    {
                        {
                            HashSet<string> helpList = new HashSet<string>();
                            {
                                if (objectTarget.frames != null)
                                {
                                    for (int i = 0; i < objectTarget.frames.Count; i++)
                                    {
                                        if (objectTarget.frames[i] == null || objectTarget.frames[i].mesh == null || !AssetDatabase.Contains(objectTarget.frames[i].mesh))
                                        {
                                            helpList.Add("Mesh");
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    helpList.Add("Mesh");
                                }
                            }
                            if (helpList.Count > 0)
                            {
                                EditorGUILayout.HelpBox(GetHelpStrings("Frame animation", new List<string>(helpList)), MessageType.Error);
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
        
        protected override void DrawBaseMesh()
        {
            if (objectTarget.mesh != null)
            {
                editorCommon.unlitColorMaterial.color = new Color(0, 0, 0, 1f);
                editorCommon.unlitColorMaterial.SetPass(0);
                Graphics.DrawMeshNow(objectTarget.mesh, baseTarget.transform.localToWorldMatrix);
            }
        }

        protected override List<Material> GetMaterialListMaterials()
        {
            return objectTarget.materials;
        }
        protected override void AddMaterialData(string name)
        {
            for (int i = 0; i < objectTarget.frames.Count; i++)
            {
                objectTarget.frames[i].materialData.Add(new MaterialData() { name = name });
            }
        }
        protected override void RemoveMaterialData(int index)
        {
            for (int i = 0; i < objectTarget.frames.Count; i++)
            {
                objectTarget.frames[i].materialData.RemoveAt(index);
            }
        }

        protected void SetPreviewCameraTransform(Transform transform, Bounds bounds)
        {
            switch (objectTarget.edit_previewCameraMode)
            {
            case VoxelFrameAnimationObject.Edit_CameraMode.forward:
                {
                    var rot = Quaternion.AngleAxis(180f, Vector3.up);
                    transform.localRotation = rot;
                    transform.localPosition = bounds.center + Vector3.forward * bounds.size.z * 5f;
                }
                break;
            case VoxelFrameAnimationObject.Edit_CameraMode.back:
                {
                    transform.localRotation = Quaternion.identity;
                    transform.localPosition = bounds.center + Vector3.back * bounds.size.z * 5f;
                }
                break;
            case VoxelFrameAnimationObject.Edit_CameraMode.up:
                {
                    var rot = Quaternion.AngleAxis(90f, Vector3.right);
                    transform.localRotation = rot;
                    transform.localPosition = bounds.center + Vector3.up * bounds.size.z * 5f;
                }
                break;
            case VoxelFrameAnimationObject.Edit_CameraMode.down:
                {
                    var rot = Quaternion.AngleAxis(-90f, Vector3.right);
                    transform.localRotation = rot;
                    transform.localPosition = bounds.center + Vector3.down * bounds.size.z * 5f;
                }
                break;
            case VoxelFrameAnimationObject.Edit_CameraMode.right:
                {
                    var rot = Quaternion.AngleAxis(-90f, Vector3.up);
                    transform.localRotation = rot;
                    transform.localPosition = bounds.center + Vector3.right * bounds.size.z * 5f;
                }
                break;
            case VoxelFrameAnimationObject.Edit_CameraMode.left:
                {
                    var rot = Quaternion.AngleAxis(90f, Vector3.up);
                    transform.localRotation = rot;
                    transform.localPosition = bounds.center + Vector3.left * bounds.size.z * 5f;
                }
                break;
            }
        }

        protected void UpdateFrameList()
        {
            frameList = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty("frames"),
                true, true, false, true
            );
            frameList.elementHeight = 40;
            frameList.drawHeaderCallback = (rect) =>
            {
                Rect r = rect;
                {
                    r.x -= 16;
                    r.width = 80;
                    EditorGUI.BeginChangeCheck();
                    var edit_previewCameraMode = (VoxelFrameAnimationObject.Edit_CameraMode)EditorGUI.EnumPopup(r, objectTarget.edit_previewCameraMode);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(objectTarget, "Camera Mode");
                        objectTarget.edit_previewCameraMode = edit_previewCameraMode;
                        objectCore.ClearFramesIcon();
                        if (VoxelFrameAnimationListWindow.instance != null)
                            VoxelFrameAnimationListWindow.instance.Repaint();
                    }
                }
                r.x += 16 + frameList.elementHeight + 12;
                r.width = rect.width - r.width;
                EditorGUI.LabelField(r, "Object & Mesh", EditorStyles.boldLabel);
            };
            frameList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.yMin += 2;
                rect.yMax -= 2;
                if (index < objectTarget.frames.Count && objectTarget.frames[index] != null)
                {
                    Rect r = rect;
                    #region Icon
                    r.width = frameList.elementHeight - 2;
                    r.height = frameList.elementHeight - 2;
                    #region IconRender
                    if(objectTarget.frames[index].icon == null && objectTarget.frames[index].materialIndexes != null)
                    {
                        Material[] materials = new Material[objectTarget.frames[index].materialIndexes.Count];
                        for (int j = 0; j < objectTarget.frames[index].materialIndexes.Count; j++)
                        {
                            var mindex = objectTarget.frames[index].materialIndexes[j];
                            if (objectTarget.materials == null || mindex >= objectTarget.materials.Count) continue;
                            materials[j] = objectTarget.materials[mindex];
                        }
                        var mesh = objectTarget.frames[index].mesh;
                        if (mesh != null)
                        {
                            editorCommon.CreateIconObject(mesh, materials);
                            SetPreviewCameraTransform(editorCommon.iconCamera.transform, mesh.bounds);
                            objectTarget.frames[index].icon = editorCommon.IconObjectRender();
                        }
                    }
                    #endregion
                    if (objectTarget.frames[index].icon != null)
                        GUI.DrawTexture(r, objectTarget.frames[index].icon);
                    r.x += r.width + 2;
                    #endregion
                    #region Object
                    {
                        r.width = rect.width - (r.x - rect.x);
                        r.height = 16;
                        if (objectTarget.frames[index].voxelFileObject != null)
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUI.ObjectField(r, objectTarget.frames[index].voxelFileObject, typeof(UnityEngine.Object), false);
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {
                            EditorGUI.LabelField(r, Path.GetFileName(objectTarget.frames[index].voxelFilePath));
                        }
                    }
                    #endregion
                    #region Mesh
                    {
                        r.y += 18 + 2;
                        r.width -= 104;
                        if(objectTarget.frames[index].mesh != null && AssetDatabase.Contains(objectTarget.frames[index].mesh))
                            r.width += 48;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.ObjectField(r, objectTarget.frames[index].mesh, typeof(Mesh), false);
                        EditorGUI.EndDisabledGroup();
                        r.x += r.width + 4;
                        if (objectTarget.frames[index].mesh != null && AssetDatabase.Contains(objectTarget.frames[index].mesh))
                            r.x -= 48;
                        if (objectTarget.frames[index].mesh != null)
                        {
                            r.width = 48;
                            r.height = 16;
                            if (!AssetDatabase.Contains(objectTarget.frames[index].mesh))
                            {
                                if (GUI.Button(r, "Save"))
                                {
                                    #region Create Mesh
                                    var name = objectTarget.frames[index].voxelFileObject != null ? objectTarget.frames[index].voxelFileObject.name : Path.GetFileNameWithoutExtension(objectTarget.frames[index].voxelFilePath);
                                    var path = EditorUtility.SaveFilePanel("Save mesh", objectCore.GetDefaultPath(), string.Format("{0}_mesh_{1}.asset", baseTarget.gameObject.name, name), "asset");
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
                                            var oldObj = objectTarget.frames[index].mesh;
                                            AssetDatabase.CreateAsset(Mesh.Instantiate(objectTarget.frames[index].mesh), path);
                                            objectTarget.frames[index].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                                            objectCore.SwapAnimationObjectReference(oldObj, objectTarget.frames[index].mesh);
                                            objectCore.SetRendererCompornent();
                                            InternalEditorUtility.RepaintAllViews();
                                        }
                                    }
                                    #endregion
                                }
                            }
                            r.x += r.width + 4;
                            if (GUI.Button(r, "Reset"))
                            {
                                #region Reset Mesh
                                UndoRecordObject("Reset Mesh");
                                var oldObj = objectTarget.frames[index].mesh;
                                objectTarget.frames[index].mesh = null;
                                Refresh();
                                objectCore.SwapAnimationObjectReference(oldObj, objectTarget.frames[index].mesh);
                                InternalEditorUtility.RepaintAllViews();
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            };
            frameList.onSelectCallback = (list) =>
            {
                objectTarget.edit_frameIndex = list.index;
                objectTarget.Edit_SetFrameCurrentVoxelMaterialData();
                if (VoxelFrameAnimationListWindow.instance != null)
                    VoxelFrameAnimationListWindow.instance.FrameIndexChanged();
                if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                    UpdateMaterialEnableMesh();
                objectCore.SetCurrentMesh();
            };
            frameList.onChangedCallback = (list) =>
            {
                objectCore.ClearFramesIcon();
                if (VoxelFrameAnimationListWindow.instance != null)
                    VoxelFrameAnimationListWindow.instance.Repaint();
            };
            frameList.onRemoveCallback = (list) =>
            {
                if (list.index > 0)
                {
                    UndoRecordObject("Remove Frame");
                    objectTarget.frames.RemoveAt(list.index);
                    if (list.index < objectTarget.edit_frameIndex)
                        objectTarget.edit_frameIndex--;
                    editorCommon.previewFrameIndexOld = -1;
                    if (VoxelFrameAnimationListWindow.instance != null)
                        VoxelFrameAnimationListWindow.instance.Repaint();
                }
            };

            if (objectTarget.edit_frameEnable)
                frameList.index = objectTarget.edit_frameIndex;
            else
                objectTarget.edit_frameIndex = -1;
        }

        private int editorOnCurveWasModifiedCount;
        private void EditorOnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType deleted)
        {
            if (editorOnCurveWasModifiedCount++ == 0)
            {
                if (deleted == AnimationUtility.CurveModifiedType.CurveModified)
                {
                    if (binding.type == typeof(MeshRenderer))
                    {
                        //AnimationUtility.SetObjectReferenceCurve(clip, binding, null);    So it will be back in this useless.
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, new ObjectReferenceKeyframe[0]);
                    }
                }
            }
            editorOnCurveWasModifiedCount--;
        }

        [MenuItem("CONTEXT/VoxelFrameAnimationObject/Save All Unsaved Assets")]
        private static void ContextSaveAllUnsavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelFrameAnimationObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelFrameAnimationObjectCore(objectTarget);

            var folder = EditorUtility.OpenFolderPanel("Save all", objectCore.GetDefaultPath(), null);
            if (string.IsNullOrEmpty(folder)) return;
            if (folder.IndexOf(Application.dataPath) < 0)
            {
                SaveInsideAssetsFolderDisplayDialog();
                return;
            }

            Undo.RecordObject(objectTarget, "Save All Unsaved Assets");

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

            #region Mesh
            if (objectTarget.frames != null)
            {
                for (int i = 0; i < objectTarget.frames.Count; i++)
                {
                    if (objectTarget.frames[i].mesh != null && !AssetDatabase.Contains(objectTarget.frames[i].mesh))
                    {
                        var name = objectTarget.frames[i].voxelFileObject != null ? objectTarget.frames[i].voxelFileObject.name : Path.GetFileNameWithoutExtension(objectTarget.frames[i].voxelFilePath);
                        var path = folder + "/" + string.Format("{0}_mesh_{1}.asset", objectTarget.gameObject.name, name);
                        path = path.Replace(Application.dataPath, "Assets");
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                        var oldObj = objectTarget.frames[i].mesh;
                        AssetDatabase.CreateAsset(Mesh.Instantiate(objectTarget.frames[i].mesh), path);
                        objectTarget.frames[i].mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        objectCore.SwapAnimationObjectReference(oldObj, objectTarget.frames[i].mesh);
                    }
                }
            }
            #endregion

            objectCore.SetRendererCompornent();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelFrameAnimationObject/Reset All Assets")]
        private static void ResetAllSavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelFrameAnimationObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelFrameAnimationObjectCore(objectTarget);

            Undo.RecordObject(objectTarget, "Reset All Assets");

            #region Material
            objectTarget.materials = null;
            #endregion

            #region Texture
            objectTarget.atlasTexture = null;
            #endregion

            #region Mesh
            List<UnityEngine.Object> oldFramesMesh = null;
            if (objectTarget.frames != null)
            {
                oldFramesMesh = new List<UnityEngine.Object>(objectTarget.frames.Count);
                for (int i = 0; i < objectTarget.frames.Count; i++)
                {
                    oldFramesMesh.Add(objectTarget.frames[i].mesh);
                    objectTarget.frames[i].mesh = null;
                }
            }
            #endregion

            objectCore.ReCreate();
            
            #region Mesh
            if(oldFramesMesh != null)
            {
                for (int i = 0; i < oldFramesMesh.Count; i++)
                {
                    objectCore.SwapAnimationObjectReference(oldFramesMesh[i], objectTarget.frames[i].mesh);
                }
            }
            #endregion

            InternalEditorUtility.RepaintAllViews();
        }
    }
}

