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
    public class VoxelFrameAnimationListWindow : EditorWindow
    {
        public static VoxelFrameAnimationListWindow instance;

        public VoxelFrameAnimationObject objectTarget { get; private set; }

        public event Action frameIndexChanged;
        public event Action previewCameraModeChanged;

        private GUIStyle guiStyleButton;
        private GUIStyle guiStyleActiveButton;
        private GUIStyle guiStyleNameLabel;

        public static void Create(VoxelFrameAnimationObject objectTarget)
        {
            if (instance == null)
            {
                instance = CreateInstance<VoxelFrameAnimationListWindow>();
            }

            instance.Initialize(objectTarget);
            
            instance.ShowUtility();
        }
        public static void Destroy()
        {
            if (instance != null)
            {
                instance.Close();
            }
        }

        void OnEnable()
        {
            InternalEditorUtility.RepaintAllViews();
        }
        void OnDisable()
        {
            instance = null;

            InternalEditorUtility.RepaintAllViews();
        }
        void OnDestroy()
        {
            OnDisable();
        }

        void OnSelectionChange()
        {
            var go = Selection.activeGameObject;
            if (go != objectTarget)
            {
                Close();
            }
        }

        private void Initialize(VoxelFrameAnimationObject objectTarget)
        {
            this.objectTarget = objectTarget;

            UpdateTitle();
        }

        void Update()
        {
            if (instance == null)
            {
                Close();
            }
        }

        void OnGUI()
        {
            #region GUIStyle
            if (guiStyleButton == null)
                guiStyleButton = new GUIStyle(GUI.skin.button);
            guiStyleButton.margin = new RectOffset(0, 0, 0, 0);
            guiStyleButton.overflow = new RectOffset(0, 0, 0, 0);
            guiStyleButton.padding = new RectOffset(0, 0, 0, 0);
            if (guiStyleActiveButton == null)
                guiStyleActiveButton = new GUIStyle(GUI.skin.button);
            guiStyleActiveButton.margin = new RectOffset(0, 0, 0, 0);
            guiStyleActiveButton.overflow = new RectOffset(0, 0, 0, 0);
            guiStyleActiveButton.padding = new RectOffset(0, 0, 0, 0);
            guiStyleActiveButton.normal = guiStyleActiveButton.active;
            if (guiStyleNameLabel == null)
                guiStyleNameLabel = new GUIStyle(GUI.skin.label);
            guiStyleNameLabel.alignment = TextAnchor.LowerCenter;
            #endregion

            float x = 2;
            float y = 2;

            EditorGUILayout.BeginHorizontal();
            {
                #region PreviewCameraMode
                {
                    EditorGUI.BeginChangeCheck();
                    var edit_previewCameraMode = (VoxelFrameAnimationObject.Edit_CameraMode)EditorGUILayout.EnumPopup(objectTarget.edit_previewCameraMode, GUILayout.Width(64));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(objectTarget, "Camera Mode");
                        objectTarget.edit_previewCameraMode = edit_previewCameraMode;
                        if (previewCameraModeChanged != null)
                            previewCameraModeChanged.Invoke();
                        InternalEditorUtility.RepaintAllViews();
                    }
                }
                #endregion
                EditorGUILayout.Space();
                #region Size
                {
                    EditorGUI.BeginChangeCheck();
                    var edit_frameIconSize = EditorGUILayout.Slider(objectTarget.edit_frameIconSize, 32f, 128f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(objectTarget, "Preview Icon Size");
                        objectTarget.edit_frameIconSize = edit_frameIconSize;
                    }
                }
                #endregion
                y += 20;
            }
            EditorGUILayout.EndHorizontal();

            {
                int count = Math.Max(1, Mathf.FloorToInt(position.width / objectTarget.edit_frameIconSize));
                for (int i = 0; i < objectTarget.frames.Count; i++)
                {
                    if (GUI.Button(new Rect(x, y, objectTarget.edit_frameIconSize, objectTarget.edit_frameIconSize), objectTarget.frames[i].icon, i != objectTarget.edit_frameIndex ? guiStyleButton : guiStyleActiveButton))
                    {
                        Undo.RecordObject(objectTarget, "Select Frame");
                        objectTarget.edit_frameIndex = i;
                        if (frameIndexChanged != null)
                            frameIndexChanged.Invoke();
                        UpdateTitle();
                        InternalEditorUtility.RepaintAllViews();
                    }
                    GUI.Label(new Rect(x, y, objectTarget.edit_frameIconSize, objectTarget.edit_frameIconSize), objectTarget.frames[i].voxelFileObject != null ? objectTarget.frames[i].voxelFileObject.name : Path.GetFileNameWithoutExtension(objectTarget.frames[i].voxelFilePath), guiStyleNameLabel);


                    x += objectTarget.edit_frameIconSize + 2;
                    if(i % count == count - 1)
                    {
                        x = 2;
                        y += objectTarget.edit_frameIconSize + 2;
                    }
                }
            }
        }

        public void FrameIndexChanged()
        {
            UpdateTitle();
            Repaint();
        }

        private void UpdateTitle()
        {
            if (objectTarget.edit_frameEnable)
            {
                var frame = objectTarget.edit_currentFrame;
                instance.titleContent = new GUIContent(string.Format("Frame List ({0}) - ({1} / {2})", frame.voxelFileObject != null ? frame.voxelFileObject.name : Path.GetFileNameWithoutExtension(frame.voxelFilePath), objectTarget.edit_frameIndex, objectTarget.frames.Count));
            }
            else
            {
                instance.titleContent = new GUIContent("Frame List");
            }
        }
    }
}
