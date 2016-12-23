using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    public abstract class VoxelBaseEditor : EditorCommon
    {
        public VoxelBase baseTarget { get; protected set; }
        public VoxelBaseCore baseCore { get; protected set; }

        protected ReorderableList materialList;

        protected VoxelEditorCommon editorCommon;

        protected bool drawEditorMesh = true;
        protected FlagTable3 editVoxelList = new FlagTable3();

        #region GUIStyle
        protected GUIStyle guiStyleMagentaBold;
        protected GUIStyle guiStyleRedBold;
        protected GUIStyle guiStyleFoldoutBold;
        protected GUIStyle guiStyleBoldActiveButton;
        protected GUIStyle guiStyleDropDown;
        protected GUIStyle guiStyleLabelMiddleLeftItalic;
        protected GUIStyle guiStyleTextFieldMiddleLeft;
        #endregion

        #region strings
        public static readonly string[] Edit_MaterialModeString =
        {
            VoxelBase.Edit_MaterialMode.Add.ToString(),
            VoxelBase.Edit_MaterialMode.Remove.ToString(),
        };
        public static readonly string[] Edit_MaterialTypeModeString =
        {
            VoxelBase.Edit_MaterialTypeMode.Voxel.ToString(),
            VoxelBase.Edit_MaterialTypeMode.Fill.ToString(),
            VoxelBase.Edit_MaterialTypeMode.Rect.ToString(),
        };
        #endregion

        #region Prefab
        protected PrefabType prefabType { get { return PrefabUtility.GetPrefabType(baseTarget.gameObject); } }
        protected bool prefabEnable { get { var type = prefabType; return type == PrefabType.Prefab || type == PrefabType.PrefabInstance || type == PrefabType.DisconnectedPrefabInstance; } }
        #endregion

        protected virtual void OnEnable()
        {
            baseTarget = target as VoxelBase;
            if (baseTarget == null) return;

            Undo.undoRedoPerformed -= EditorUndoRedoPerformed;
            Undo.undoRedoPerformed += EditorUndoRedoPerformed;
        }
        protected virtual void OnDisable()
        {
            if (baseTarget == null) return;

            AfterRefresh();

            EditEnableMeshDestroy();

            baseCore.SetSelectedWireframeHidden(false);

            Undo.undoRedoPerformed -= EditorUndoRedoPerformed;
        }
        protected virtual void OnDestroy()
        {
            OnDisable();
        }

        protected virtual void OnEnableInitializeSet()
        {
            baseCore.Initialize();

            editorCommon = new VoxelEditorCommon(baseTarget, baseCore);

            UpdateMaterialList();
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                UpdateMaterialEnableMesh();
        }

        protected virtual void GUIStyleReady()
        {
            //Styles
            if (guiStyleMagentaBold == null)
                guiStyleMagentaBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleMagentaBold.normal.textColor = Color.magenta;
            if (guiStyleRedBold == null)
                guiStyleRedBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleRedBold.normal.textColor = Color.red;
            if (guiStyleFoldoutBold == null)
                guiStyleFoldoutBold = new GUIStyle(EditorStyles.foldout);
            guiStyleFoldoutBold.fontStyle = FontStyle.Bold;
            if (guiStyleBoldActiveButton == null)
                guiStyleBoldActiveButton = new GUIStyle(GUI.skin.button);
            guiStyleBoldActiveButton.normal = guiStyleBoldActiveButton.active;
            if (guiStyleDropDown == null)
                guiStyleDropDown = new GUIStyle("DropDown");
            guiStyleDropDown.alignment = TextAnchor.MiddleCenter;
            if (guiStyleLabelMiddleLeftItalic == null)
                guiStyleLabelMiddleLeftItalic = new GUIStyle(EditorStyles.label);
            guiStyleLabelMiddleLeftItalic.alignment = TextAnchor.MiddleLeft;
            guiStyleLabelMiddleLeftItalic.fontStyle = FontStyle.Italic;
            if (guiStyleTextFieldMiddleLeft == null)
                guiStyleTextFieldMiddleLeft = new GUIStyle(EditorStyles.textField);
            guiStyleTextFieldMiddleLeft.alignment = TextAnchor.MiddleLeft;
        }

        public override void OnInspectorGUI()
        {
            if (baseTarget == null || editorCommon == null)
            {
                DrawDefaultInspector();
                return;
            }

            baseCore.AutoSetSelectedWireframeHidden();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(PrefabUtility.GetPrefabType(baseTarget) == PrefabType.Prefab);

            InspectorGUI();

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void InspectorGUI()
        {
            GUIStyleReady();
            editorCommon.GUIStyleReady();
        }

        protected void InspectorGUI_Import()
        {
            Event e = Event.current;

            baseTarget.edit_importFoldout = EditorGUILayout.Foldout(baseTarget.edit_importFoldout, "Import", guiStyleFoldoutBold);
            if (baseTarget.edit_importFoldout)
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                EditorGUILayout.BeginVertical();
                #region Voxel File
                {
                    bool fileExists = baseCore.IsVoxelFileExists();
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (string.IsNullOrEmpty(baseTarget.voxelFilePath))
                            EditorGUILayout.LabelField("Voxel File", guiStyleMagentaBold);
                        else if (!fileExists)
                            EditorGUILayout.LabelField("Voxel File", guiStyleRedBold);
                        else
                            EditorGUILayout.LabelField("Voxel File", EditorStyles.boldLabel);

                        Action<string, UnityEngine.Object> OpenFile = (path, obj) =>
                        {
                            if (!baseCore.IsEnableFile(path))
                                return;
                            if(obj == null && path.Contains(Application.dataPath))
                            {
                                var assetPath = path.Replace(Application.dataPath, "Assets");
                                obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                            }
                            UndoRecordObject("Open Voxel File", true);
                            baseCore.Reset(path, obj);
                            baseCore.Create(path, obj);
                        };

                        var rect = GUILayoutUtility.GetRect(new GUIContent("Open"), guiStyleDropDown, GUILayout.Width(64));
                        if (GUI.Button(rect, "Open", guiStyleDropDown))
                        {
                            InspectorGUI_ImportOpenBefore();
                            GenericMenu menu = new GenericMenu();
                            #region vox
                            menu.AddItem(new GUIContent("MagicaVoxel (*.vox)"), false, () =>
                            {
                                var path = EditorUtility.OpenFilePanel("Open MagicaVoxel File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "vox");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    OpenFile(path, null);
                                }
                            });
                            #endregion
                            #region qb
                            menu.AddItem(new GUIContent("Qubicle Binary (*.qb)"), false, () =>
                            {
                                var path = EditorUtility.OpenFilePanel("Open Qubicle Binary File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "qb");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    OpenFile(path, null);
                                }
                            });
                            #endregion
                            #region png
                            menu.AddItem(new GUIContent("Pixel Art (*.png)"), false, () =>
                            {
                                var path = EditorUtility.OpenFilePanel("Open Pixel Art File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "png");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    OpenFile(path, null);
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
                                if (DragAndDrop.paths.Length != 1) break;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                                if (e.type == EventType.DragPerform)
                                {
                                    string path = DragAndDrop.paths[0];
                                    if (Path.GetPathRoot(path) == "")
                                        path = Application.dataPath + DragAndDrop.paths[0].Remove(0, "Assets".Length);
                                    OpenFile(path, DragAndDrop.objectReferences.Length > 0 ? DragAndDrop.objectReferences[0] : null);
                                    e.Use();
                                }
                                break;
                            }
                        }
                        #endregion
                        EditorGUILayout.EndHorizontal();
                    }
                    {
                        EditorGUI.indentLevel++;
                        {
                            if (baseTarget.voxelFileObject == null)
                            {
                                EditorGUILayout.LabelField(Path.GetFileName(baseTarget.voxelFilePath));
                            }
                            else
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.ObjectField(baseTarget.voxelFileObject, typeof(UnityEngine.Object), false);
                                EditorGUI.EndDisabledGroup();
                            }
                            if (!fileExists)
                            {
                                EditorGUILayout.HelpBox("Voxel file not found. Please open file.", MessageType.Error);
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                #endregion
                #region Settings
                {
                    EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    {
                        #region Import Mode
                        {
                            EditorGUI.BeginChangeCheck();
                            var importMode = (VoxelObject.ImportMode)EditorGUILayout.EnumPopup("Import Mode", baseTarget.importMode);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector");
                                baseTarget.importMode = importMode;
                                Refresh();
                            }
                        }
                        #endregion
                        #region Import Flag
                        {
                            EditorGUI.BeginChangeCheck();
                            var importFlags = (VoxelObject.ImportFlag)EditorGUILayout.EnumMaskField("Import Flag", baseTarget.importFlags);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector", true);
                                baseTarget.importFlags = importFlags;
                                Refresh();
                            }
                        }
                        #endregion
                        #region Import Scale
                        {
                            EditorGUI.BeginChangeCheck();
                            var importScale = EditorGUILayout.Vector3Field("Import Scale", baseTarget.importScale);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector", true);
                                baseTarget.importScale = importScale;
                                Refresh();
                            }
                        }
                        #endregion
                        #region Import Offset
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUI.BeginChangeCheck();
                                var importOffset = EditorGUILayout.Vector3Field("Import Offset", baseTarget.importOffset);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    UndoRecordObject("Inspector", true);
                                    baseTarget.importOffset = importOffset;
                                    Refresh();
                                }
                            }
                            /* next version
                            {
                                if(GUILayout.Button("Set", guiStyleDropDown, GUILayout.Width(40), GUILayout.Height(14)))
                                {
                                    GenericMenu menu = new GenericMenu();
                                    #region Reset
                                    menu.AddItem(new GUIContent("Reset"), false, () =>
                                    {
                                        UndoRecordObject("Inspector", true);
                                        baseTarget.importOffset = Vector3.zero;
                                        Refresh();
                                    });
                                    #endregion
                                    InspectorGUI_ImportOffsetSetExtra(menu);
                                    menu.ShowAsContext();
                                }
                            }*/
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
                        #region Enable Face
                        {
                            EditorGUI.BeginChangeCheck();
                            var enableFaceFlags = (VoxelBase.Face)EditorGUILayout.EnumMaskField("Enable Face", baseTarget.enableFaceFlags);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector");
                                baseTarget.enableFaceFlags = enableFaceFlags;
                                Refresh();
                            }
                        }
                        #endregion
                        InspectorGUI_ImportSettingsExtra();
                    }
                    EditorGUI.indentLevel--;
                }
                #endregion
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }
        protected virtual void UndoRecordObject(string text, bool reset = false)
        {
            if (baseTarget != null)
                Undo.RecordObject(baseTarget, text);
        }
        protected virtual void InspectorGUI_ImportOpenBefore() { }
        protected virtual void InspectorGUI_ImportSettingsExtra() { }
        protected virtual void InspectorGUI_ImportOffsetSetExtra(GenericMenu menu) { }
        protected virtual void InspectorGUI_Refresh()
        {
            if (GUILayout.Button("Refresh"))
            {
                UndoRecordObject("Inspector");
                Refresh();
            }
        }

        protected virtual void OnSceneGUI()
        {
            if (baseTarget == null || editorCommon == null) return;

            GUIStyleReady();
            editorCommon.GUIStyleReady();

            Event e = Event.current;
            bool repaint = false;

            #region Configure Material
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
            {
                if (baseTarget.materialData != null && materialList != null &&
                    baseTarget.edit_configureMaterialIndex >= 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
                {
                    #region Event
                    {
                        Tools.current = Tool.None;
                        switch (e.type)
                        {
                        case EventType.MouseMove:
                            editVoxelList.Clear();
                            editorCommon.selectionRect.Reset();
                            editorCommon.ClearPreviewMesh();
                            UpdateCursorMesh();
                            break;
                        case EventType.MouseDown:
                            if (editorCommon.CheckMousePositionEditorRects())
                            {
                                if (!e.alt && e.button == 0)
                                {
                                    editorCommon.ClearCursorMesh();
                                    EventMouseDrag(true);
                                }
                                else if (!e.alt && e.button == 1)
                                {
                                    ClearMakeAddData();
                                }
                            }
                            break;
                        case EventType.MouseDrag:
                            {
                                if (!e.alt && e.button == 0)
                                {
                                    EventMouseDrag(false);
                                }
                            }
                            break;
                        case EventType.MouseUp:
                            if (!e.alt && e.button == 0)
                            {
                                EventMouseApply();
                            }
                            ClearMakeAddData();
                            UpdateCursorMesh();
                            repaint = true;
                            break;
                        case EventType.Layout:
                            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                            break;
                        }
                        switch (e.type)
                        {
                        case EventType.KeyDown:
                            if (!e.alt)
                            {
                                if (e.keyCode == KeyCode.F5)
                                {
                                    Refresh();
                                }
                                else if (e.keyCode == KeyCode.Space)
                                {
                                    drawEditorMesh = false;
                                }
                            }
                            break;
                        case EventType.KeyUp:
                            {
                                if (e.keyCode == KeyCode.Space)
                                {
                                    drawEditorMesh = true;
                                }
                            }
                            break;
                        }
                    }
                    #endregion

                    if (drawEditorMesh)
                    {
                        DrawBaseMesh();

                        #region MaterialMesh
                        if (baseTarget.edit_enableMesh != null)
                        {
                            for (int i = 0; i < baseTarget.edit_enableMesh.Length; i++)
                            {
                                if (baseTarget.edit_enableMesh[i] == null) continue;
                                if (baseTarget.edit_MaterialPreviewMode == VoxelBase.Edit_MaterialPreviewMode.Transparent)
                                {
                                    editorCommon.vertexColorTransparentMaterial.color = new Color(1, 0, 0, 0.75f);
                                    editorCommon.vertexColorTransparentMaterial.SetPass(0);
                                }
                                else
                                {
                                    editorCommon.vertexColorMaterial.color = new Color(1, 0, 0, 1);
                                    editorCommon.vertexColorMaterial.SetPass(0);
                                }
                                Graphics.DrawMeshNow(baseTarget.edit_enableMesh[i], baseTarget.transform.localToWorldMatrix);
                            }
                        }
                        #endregion
                    }

                    if (SceneView.currentDrawingSceneView == SceneView.lastActiveSceneView)
                    {
                        #region Preview Mesh
                        if (editorCommon.previewMesh != null)
                        {
                            Color color = Color.white;
                            if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Add)
                            {
                                color = new Color(1, 0, 0, 1);
                            }
                            else if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Remove)
                            {
                                color = new Color(0, 0, 1, 1);
                            }
                            color.a = 0.5f + 0.5f * (1f - editorCommon.AnimationPower);
                            for (int i = 0; i < editorCommon.previewMesh.Length; i++)
                            {
                                if (editorCommon.previewMesh[i] == null) continue;
                                editorCommon.vertexColorTransparentMaterial.color = color;
                                editorCommon.vertexColorTransparentMaterial.SetPass(0);
                                Graphics.DrawMeshNow(editorCommon.previewMesh[i], baseTarget.transform.localToWorldMatrix);
                            }
                            repaint = true;
                        }
                        #endregion

                        #region Cursor Mesh
                        {
                            float color = 0.2f + 0.4f * (1f - editorCommon.AnimationPower);
                            if (editorCommon.cursorMesh != null)
                            {
                                for (int i = 0; i < editorCommon.cursorMesh.Length; i++)
                                {
                                    if (editorCommon.cursorMesh[i] == null) continue;
                                    editorCommon.vertexColorTransparentMaterial.color = new Color(1, 1, 1, color);
                                    editorCommon.vertexColorTransparentMaterial.SetPass(0);
                                    Graphics.DrawMeshNow(editorCommon.cursorMesh[i], baseTarget.transform.localToWorldMatrix);
                                }
                            }
                            repaint = true;
                        }
                        #endregion

                        #region Selection Rect
                        if (baseTarget.edit_materialTypeMode == VoxelBase.Edit_MaterialTypeMode.Rect)
                        {
                            if (editorCommon.selectionRect.Enable)
                            {
                                Handles.BeginGUI();
                                GUI.Box(editorCommon.selectionRect.rect, "", "SelectionRect");
                                Handles.EndGUI();
                                repaint = true;
                            }
                        }
                        #endregion

                        #region Tool
                        if (baseTarget.edit_configureMaterialIndex > 0)
                        {
                            Handles.BeginGUI();
                            {
                                var editorBoxRect = new Rect(2, 2, 204, 124);
                                GUI.Box(editorBoxRect, "Material Editor", editorCommon.guiStyleAlphaBox);
                                editorCommon.editorRectList.Add(editorBoxRect);
                                #region "?"
                                {
                                    if (GUI.Button(new Rect(editorBoxRect.x + editorBoxRect.width - 16, editorBoxRect.y, 16, 16), "?", baseTarget.edit_helpEnable ? editorCommon.guiStyleActiveButton : GUI.skin.button))
                                    {
                                        Undo.RecordObject(baseTarget, "Help Enable");
                                        baseTarget.edit_helpEnable = !baseTarget.edit_helpEnable;
                                    }
                                }
                                #endregion
                            }
                            float x = 4;
                            float y = 20;
                            #region MaterialMode
                            {
                                EditorGUI.BeginChangeCheck();
                                var edit_materialMode = (VoxelBase.Edit_MaterialMode)GUI.Toolbar(new Rect(x, y, 200, 20), (int)baseTarget.edit_materialMode, Edit_MaterialModeString);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(baseTarget, "Material Mode");
                                    baseTarget.edit_materialMode = edit_materialMode;
                                    ShowNotification();
                                }
                            }
                            y += 24;
                            #endregion
                            #region MaterialTypeMode
                            {
                                EditorGUI.BeginChangeCheck();
                                var edit_materialTypeMode = (VoxelBase.Edit_MaterialTypeMode)GUI.Toolbar(new Rect(x, y, 200, 20), (int)baseTarget.edit_materialTypeMode, Edit_MaterialTypeModeString);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(baseTarget, "Material Type Mode");
                                    baseTarget.edit_materialTypeMode = edit_materialTypeMode;
                                    ShowNotification();
                                }
                            }
                            y += 24;
                            #endregion
                            #region Transparent
                            {
                                EditorGUI.BeginChangeCheck();
                                var transparent = GUI.Toggle(new Rect(x, y, 200, 16), baseTarget.materialData[baseTarget.edit_configureMaterialIndex].transparent, "Transparent", editorCommon.guiStyleToggleRight);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(baseTarget, "Transparent");
                                    baseTarget.materialData[baseTarget.edit_configureMaterialIndex].transparent = transparent;
                                    baseTarget.edit_afterRefresh = true;
                                }
                            }
                            y += 20;
                            #endregion
                            #region MaterialPreviewMode
                            {
                                {
                                    EditorGUI.LabelField(new Rect(x, y, 100, 16), "Preview", editorCommon.guiStyleLabel);
                                }
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var edit_MaterialPreviewMode = (VoxelBase.Edit_MaterialPreviewMode)EditorGUI.EnumPopup(new Rect(x + 100, y, 200 - 100, 16), baseTarget.edit_MaterialPreviewMode);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(baseTarget, "Material Preview Mode");
                                        baseTarget.edit_MaterialPreviewMode = edit_MaterialPreviewMode;
                                    }
                                }
                            }
                            y += 20;
                            #endregion
                            #region WeightClear
                            {
                                if (GUI.Button(new Rect(x, y, 200, 16), "Clear"))
                                {
                                    Undo.RecordObject(baseTarget, "Clear");
                                    baseTarget.materialData[baseTarget.edit_configureMaterialIndex].ClearMaterial();
                                    UpdateMaterialEnableMesh();
                                    baseTarget.edit_afterRefresh = true;
                                }
                            }
                            y += 20;
                            #endregion
                            #region Help
                            if (baseTarget.edit_helpEnable)
                            {
                                var editHelpBoxRect = new Rect(2, y, 204, 32);
                                GUI.Box(editHelpBoxRect, "", editorCommon.guiStyleAlphaBox);
                                editorCommon.editorRectList.Add(editHelpBoxRect);
                                editorCommon.guiStyleLabel.normal.textColor = Color.white;
                                EditorGUI.LabelField(new Rect(x, y, 202, 16), "F5 Key - Refresh", editorCommon.guiStyleLabel);
                                y += 16;
                                EditorGUI.LabelField(new Rect(x, y, 202, 16), "Press Space Key - Hide Preview", editorCommon.guiStyleLabel);
                            }
                            #endregion
                            Handles.EndGUI();
                        }
                        #endregion
                    }
                }
            }
            #endregion

            if (repaint)
            {
                SceneView.currentDrawingSceneView.Repaint();
            }
        }

        protected abstract void DrawBaseMesh();

        protected void UpdatePreviewMesh()
        {
            editorCommon.ClearPreviewMesh();

            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material &&
                baseTarget.edit_configureMaterialIndex > 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
            {
                List<VoxelData.Voxel> voxels = new List<VoxelData.Voxel>();
                editVoxelList.AllAction((x, y, z) =>
                {
                    var index = baseTarget.voxelData.VoxelTableContains(x, y, z);
                    if (index < 0) return;
                    var voxel = baseTarget.voxelData.voxels[index];
                    voxel.palette = -1;
                    voxels.Add(voxel);
                });
                if (voxels.Count > 0)
                {
                    editorCommon.previewMesh = baseCore.Edit_CreateMesh(voxels, null, false);
                }
            }
        }
        protected void UpdateCursorMesh()
        {
            editorCommon.ClearCursorMesh();

            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material &&
                baseTarget.edit_configureMaterialIndex > 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
            {
                switch (baseTarget.edit_materialTypeMode)
                {
                case VoxelBase.Edit_MaterialTypeMode.Voxel:
                    {
                        var result = editorCommon.GetMousePositionVoxel();
                        if (result.HasValue)
                        {
                            editorCommon.cursorMesh = baseCore.Edit_CreateMesh(new List<VoxelData.Voxel>() { new VoxelData.Voxel() { position = result.Value, palette = -1 } });
                        }
                    }
                    break;
                case VoxelBase.Edit_MaterialTypeMode.Fill:
                    {
                        var pos = editorCommon.GetMousePositionVoxel();
                        if (pos.HasValue)
                        {
                            var faceAreaTable = editorCommon.GetFillVoxelFaceAreaTable(pos.Value);
                            if (faceAreaTable != null)
                                editorCommon.cursorMesh = new Mesh[1] { baseCore.Edit_CreateMeshOnly_Mesh(faceAreaTable, null, null) };
                        }
                    }
                    break;
                }
            }
        }

        protected void ClearMakeAddData()
        {
            editVoxelList.Clear();
            editorCommon.selectionRect.Reset();
            editorCommon.ClearPreviewMesh();
            editorCommon.ClearCursorMesh();
        }

        private void EventMouseDrag(bool first)
        {
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
            {
                UpdateCursorMesh();
                switch (baseTarget.edit_materialTypeMode)
                {
                case VoxelBase.Edit_MaterialTypeMode.Voxel:
                    {
                        var result = editorCommon.GetMousePositionVoxel();
                        if (result.HasValue)
                        {
                            editVoxelList.Set(result.Value, true);
                            UpdatePreviewMesh();
                        }
                    }
                    break;
                case VoxelBase.Edit_MaterialTypeMode.Fill:
                    {
                        var pos = editorCommon.GetMousePositionVoxel();
                        if (pos.HasValue)
                        {
                            var result = editorCommon.GetFillVoxel(pos.Value);
                            if (result != null)
                            {
                                for (int i = 0; i < result.Count; i++)
                                    editVoxelList.Set(result[i], true);
                                UpdatePreviewMesh();
                            }
                        }
                    }
                    break;
                case VoxelBase.Edit_MaterialTypeMode.Rect:
                    {
                        var pos = new IntVector2((int)Event.current.mousePosition.x, (int)Event.current.mousePosition.y);
                        if (first) { editorCommon.selectionRect.Reset(); editorCommon.selectionRect.SetStart(pos); }
                        else editorCommon.selectionRect.SetEnd(pos);
                        //
                        editVoxelList.Clear();
                        {
                            var list = editorCommon.GetSelectionRectVoxel();
                            for (int i = 0; i < list.Count; i++)
                                editVoxelList.Set(list[i], true);
                        }
                        UpdatePreviewMesh();
                    }
                    break;
                }
            }
        }
        private void EventMouseApply()
        {
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
            {
                Undo.RecordObject(baseTarget, "Material");

                bool update = false;
                if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Add)
                {
                    editVoxelList.AllAction((x, y, z) =>
                    {
                        if (!update)
                            DisconnectPrefabInstance();

                        for (int i = 0; i < baseTarget.materialData.Count; i++)
                        {
                            if (i == baseTarget.edit_configureMaterialIndex) continue;
                            if (baseTarget.materialData[i].GetMaterial(new IntVector3(x, y, z)))
                            {
                                baseTarget.materialData[i].RemoveMaterial(new IntVector3(x, y, z));
                            }
                        }
                        baseTarget.materialData[baseTarget.edit_configureMaterialIndex].SetMaterial(new IntVector3(x, y, z));
                        update = true;
                    });
                }
                else if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Remove)
                {
                    editVoxelList.AllAction((x, y, z) =>
                    {
                        if (baseTarget.materialData[baseTarget.edit_configureMaterialIndex].GetMaterial(new IntVector3(x, y, z)))
                        {
                            if (!update)
                                DisconnectPrefabInstance();

                            baseTarget.materialData[baseTarget.edit_configureMaterialIndex].RemoveMaterial(new IntVector3(x, y, z));
                            update = true;
                        }
                    });
                }
                else
                {
                    Assert.IsTrue(false);
                }
                if (update)
                {
                    UpdateMaterialEnableMesh();
                    baseTarget.edit_afterRefresh = true;
                }
                editVoxelList.Clear();
            }
        }

        private void ShowNotification()
        {
            SceneView.currentDrawingSceneView.ShowNotification(new GUIContent(string.Format("{0} - {1}", baseTarget.edit_materialMode, baseTarget.edit_materialTypeMode)));
        }

        protected abstract List<Material> GetMaterialListMaterials();
        protected virtual void AddMaterialData(string name)
        {
            baseTarget.materialData.Add(new MaterialData() { name = name });
        }
        protected virtual void RemoveMaterialData(int index)
        {
            baseTarget.materialData.RemoveAt(index);
        }
        protected void UpdateMaterialList()
        {
            materialList = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty("materialData"),
                false, true, true, true
            );
            materialList.elementHeight = 20;
            materialList.drawHeaderCallback = (rect) =>
            {
                Rect r = rect;
                EditorGUI.LabelField(r, "Name", EditorStyles.boldLabel);
                r.x = 182;
                var materials = GetMaterialListMaterials();
                if (materials != null)
                    EditorGUI.LabelField(r, "Material", EditorStyles.boldLabel);
                r.x = 182;
            };
            materialList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.yMin += 2;
                rect.yMax -= 2;
                if (index < baseTarget.materialData.Count)
                {
                    #region Name
                    {
                        Rect r = rect;
                        r.width = 144;
                        if (index == 0)
                        {
                            EditorGUI.LabelField(r, "default", guiStyleLabelMiddleLeftItalic);
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            string name = EditorGUI.TextField(r, baseTarget.materialData[index].name, guiStyleTextFieldMiddleLeft);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector");
                                baseTarget.materialData[index].name = name;
                            }
                        }
                    }
                    #endregion
                    #region Material
                    var materials = GetMaterialListMaterials();
                    if (materials != null && index < materials.Count)
                    {
                        {
                            Rect r = rect;
                            r.xMin = 182;
                            r.width = rect.width - r.xMin - 64;
                            if (materials[index] != null && !AssetDatabase.Contains(materials[index]))
                                r.width -= 48;
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUI.ObjectField(r, materials[index], typeof(Material), false);
                            EditorGUI.EndDisabledGroup();
                        }
                        if (materials[index] != null)
                        {
                            Rect r = rect;
                            r.xMin += rect.width - 46;
                            r.width = 48;
                            {
                                if (GUI.Button(r, "Reset"))
                                {
                                    #region Reset Material
                                    UndoRecordObject("Reset Material");
                                    materials[index] = null;
                                    Refresh();
                                    #endregion
                                }
                            }
                            if (!AssetDatabase.Contains(materials[index]))
                            {
                                r.xMin -= 52;
                                r.width = 48;
                                if (GUI.Button(r, "Save"))
                                {
                                    #region Create Material
                                    string path = EditorUtility.SaveFilePanel("Save material", baseCore.GetDefaultPath(), string.Format("{0}_mat{1}.mat", baseTarget.gameObject.name, index), "mat");
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        if (path.IndexOf(Application.dataPath) < 0)
                                        {
                                            SaveInsideAssetsFolderDisplayDialog();
                                        }
                                        else
                                        {
                                            UndoRecordObject("Save Material");
                                            path = path.Replace(Application.dataPath, "Assets");
                                            AssetDatabase.CreateAsset(Material.Instantiate(materials[index]), path);
                                            materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                                            baseCore.SetRendererCompornent();
                                        }
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                    #endregion
                }
            };
            materialList.onSelectCallback = (list) =>
            {
                UndoRecordObject("Inspector");
                baseTarget.edit_configureMaterialIndex = list.index;
                if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                    UpdateMaterialEnableMesh();
                InternalEditorUtility.RepaintAllViews();
            };
            materialList.onAddCallback = (list) =>
            {
                UndoRecordObject("Inspector");
                AddMaterialData(baseTarget.materialData.Count.ToString());
                var materials = GetMaterialListMaterials();
                if (materials != null)
                    materials.Add(null);
                Refresh();
                baseTarget.edit_configureMaterialIndex = list.count;
                list.index = baseTarget.edit_configureMaterialIndex;
                InternalEditorUtility.RepaintAllViews();
            };
            materialList.onRemoveCallback = (list) =>
            {
                if (list.index > 0 && list.index < baseTarget.materialData.Count)
                {
                    UndoRecordObject("Inspector");
                    RemoveMaterialData(list.index);
                    var materials = GetMaterialListMaterials();
                    if (materials != null)
                        materials.RemoveAt(list.index);
                    Refresh();
                    baseTarget.edit_configureMaterialIndex = -1;
                    if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                        UpdateMaterialEnableMesh();
                    InternalEditorUtility.RepaintAllViews();
                }
            };
            if (baseTarget.edit_configureMaterialIndex >= 0 && baseTarget.materialData != null && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
                materialList.index = baseTarget.edit_configureMaterialIndex;
            else
                baseTarget.edit_configureMaterialIndex = 0;
        }

        protected void UpdateMaterialEnableMesh()
        {
            if (baseTarget.materialData == null || baseTarget.voxelData == null)
            {
                EditEnableMeshDestroy();
                return;
            }

            UndoRecordObject("Inspector");

            List<VoxelData.Voxel> voxels = new List<VoxelData.Voxel>(baseTarget.voxelData.voxels.Length);
            if (baseTarget.edit_configureMaterialIndex == 0)
            {
                for (int i = 0; i < baseTarget.voxelData.voxels.Length; i++)
                {
                    {
                        bool enable = true;
                        for (int j = 0; j < baseTarget.materialData.Count; j++)
                        {
                            if (baseTarget.materialData[j].GetMaterial(baseTarget.voxelData.voxels[i].position))
                            {
                                enable = false;
                                break;
                            }
                        }
                        if (!enable) continue;
                    }
                    var voxel = baseTarget.voxelData.voxels[i];
                    voxel.palette = -1;
                    voxels.Add(voxel);
                }
            }
            else if (baseTarget.edit_configureMaterialIndex >= 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
            {
                baseTarget.materialData[baseTarget.edit_configureMaterialIndex].AllAction((pos) =>
                {
                    var index = baseTarget.voxelData.VoxelTableContains(pos);
                    if (index < 0) return;

                    var voxel = baseTarget.voxelData.voxels[index];
                    voxel.palette = -1;
                    voxels.Add(voxel);
                });
            }
            baseTarget.edit_enableMesh = baseCore.Edit_CreateMesh(voxels);
        }

        public void EditEnableMeshDestroy()
        {
            if (baseTarget.edit_enableMesh != null)
            {
                UndoRecordObject("Inspector");

                for (int i = 0; i < baseTarget.edit_enableMesh.Length; i++)
                {
                    MonoBehaviour.DestroyImmediate(baseTarget.edit_enableMesh[i]);
                }
                baseTarget.edit_enableMesh = null;
            }
        }

        protected void AfterRefresh()
        {
            if (AnimationMode.InAnimationMode())
            {
                return;
            }

            if (baseTarget.edit_afterRefresh)
                Refresh();
        }
        protected virtual void Refresh()
        {
            baseCore.ReCreate();
            baseTarget.edit_afterRefresh = false;

            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                UpdateMaterialEnableMesh();
        }

        protected void DisconnectPrefabInstance()
        {
            if (PrefabUtility.GetPrefabType(baseTarget) == PrefabType.PrefabInstance)
            {
                PrefabUtility.DisconnectPrefabInstance(baseTarget);
            }
        }

        protected virtual void EditorUndoRedoPerformed()
        {
            if (AnimationMode.InAnimationMode())
            {
                baseTarget.edit_afterRefresh = true;
                return;
            }

            if (baseTarget != null && baseCore != null)
            {
                if (baseCore.RefreshCheckerCheck())
                {
                    Refresh();
                }
                else
                {
                    baseCore.SetRendererCompornent();
                }
            }
            Repaint();
        }
    }
}
