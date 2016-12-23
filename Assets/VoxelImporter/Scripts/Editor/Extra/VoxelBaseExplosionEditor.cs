using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    public abstract class VoxelBaseExplosionEditor : EditorCommon
    {
        public VoxelBaseExplosion explosionBase { get; protected set; }
        public VoxelBaseExplosionCore explosionCore { get; protected set; }

        #region Textures
        private Texture2D playIcon;
        #endregion

        #region GUIStyle
        protected GUIStyle guiStyleMagentaBold;
        protected GUIStyle guiStyleRedBold;
        protected GUIStyle guiStyleFoldoutBold;
        protected GUIStyle guiStylePlayButton;
        protected GUIStyle guiStylePlayButtonActive;
        #endregion

        #region Prefab
        protected PrefabType prefabType { get { return PrefabUtility.GetPrefabType(explosionBase.gameObject); } }
        protected bool prefabEnable { get { var type = prefabType; return type == PrefabType.Prefab || type == PrefabType.PrefabInstance || type == PrefabType.DisconnectedPrefabInstance; } }
        #endregion

        protected virtual void OnEnable()
        {
            explosionBase = target as VoxelBaseExplosion;
            if (explosionBase == null) return;

            #region Textures
            playIcon = EditorGUIUtility.Load("icons/animation.play.png") as Texture2D;
            #endregion

            if (!EditorApplication.isPlaying)
            {
                explosionBase.SetEnableExplosionObject(true);
            }
        }

        protected virtual void OnDisable()
        {
            if (explosionBase == null) return;

            if (!EditorApplication.isPlaying)
            {
                explosionBase.SetEnableExplosionObject(false);
            }
        }

        protected virtual void GUIStyleReady()
        {
            if (guiStyleMagentaBold == null)
                guiStyleMagentaBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleMagentaBold.normal.textColor = Color.magenta;
            if (guiStyleRedBold == null)
                guiStyleRedBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleRedBold.normal.textColor = Color.red;
            if (guiStyleFoldoutBold == null)
                guiStyleFoldoutBold = new GUIStyle(EditorStyles.foldout);
            if (guiStyleFoldoutBold == null)
                guiStyleFoldoutBold = new GUIStyle(EditorStyles.foldout);
            guiStyleFoldoutBold.fontStyle = FontStyle.Bold;
            if (guiStylePlayButton == null)
                guiStylePlayButton = new GUIStyle(GUI.skin.button);
            guiStylePlayButton.padding = new RectOffset();
            if (guiStylePlayButtonActive == null)
                guiStylePlayButtonActive = new GUIStyle(GUI.skin.button);
            guiStylePlayButtonActive.padding = new RectOffset();
            guiStylePlayButtonActive.normal = guiStylePlayButtonActive.active;
        }

        public override void OnInspectorGUI()
        {
            if (explosionBase == null || explosionCore == null)
            {
                DrawDefaultInspector();
                return;
            }

            GUIStyleReady();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(PrefabUtility.GetPrefabType(explosionBase) == PrefabType.Prefab);

            #region Generate
            {
                explosionBase.edit_generateFoldout = EditorGUILayout.Foldout(explosionBase.edit_generateFoldout, "Generate", guiStyleFoldoutBold);
                if (explosionBase.edit_generateFoldout)
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    EditorGUILayout.BeginVertical();
                    {
                        #region Settings
                        {
                            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                            {
                                EditorGUI.indentLevel++;
                                #region BirthRate
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var edit_birthRate = EditorGUILayout.Slider("Birth Rate", explosionBase.edit_birthRate, 0f, 1f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(explosionBase, "Inspector");
                                        explosionBase.edit_birthRate = edit_birthRate;
                                    }
                                }
                                #endregion
                                #region VisibleOnly
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var edit_visibleOnly = EditorGUILayout.Toggle("Visible Only", explosionBase.edit_visibleOnly);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(explosionBase, "Inspector");
                                        explosionBase.edit_visibleOnly = edit_visibleOnly;
                                    }
                                }
                                #endregion
                                #region Velocity
                                {
                                    {
                                        var min = explosionBase.edit_velocityMin;
                                        var max = explosionBase.edit_velocityMax;
                                        EditorGUI.BeginChangeCheck();
                                        EditorGUILayout.MinMaxSlider(new GUIContent("Velocity"), ref min, ref max, 0f, 300f);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            Undo.RecordObject(explosionBase, "Inspector");
                                            explosionBase.edit_velocityMin = min;
                                            explosionBase.edit_velocityMax = max;
                                        }
                                    }
                                    EditorGUI.indentLevel++;
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var edit_velocityMin = EditorGUILayout.FloatField("Min", explosionBase.edit_velocityMin);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            Undo.RecordObject(explosionBase, "Inspector");
                                            edit_velocityMin = Mathf.Clamp(edit_velocityMin, 0f, 300f);
                                            edit_velocityMin = Mathf.Min(edit_velocityMin, explosionBase.edit_velocityMax);
                                            explosionBase.edit_velocityMin = edit_velocityMin;
                                        }
                                    }
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var edit_velocityMax = EditorGUILayout.FloatField("Max", explosionBase.edit_velocityMax);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            Undo.RecordObject(explosionBase, "Inspector");
                                            edit_velocityMax = Mathf.Clamp(edit_velocityMax, 0f, 300f);
                                            edit_velocityMax = Mathf.Max(edit_velocityMax, explosionBase.edit_velocityMin);
                                            explosionBase.edit_velocityMax = edit_velocityMax;
                                        }
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                                EditorGUI.indentLevel--;
                            }
                        }
                        #endregion

                        Inspector_MeshMaterial();

                        EditorGUILayout.Space();

                        #region Generate
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("Generate"))
                            {
                                Undo.RecordObject(explosionBase, "Generate Voxel Explosion");
                                explosionCore.Generate();
                            }
                            {
                                EditorGUI.BeginChangeCheck();
                                var edit_autoGenerate = EditorGUILayout.ToggleLeft("Auto", explosionBase.edit_autoGenerate, GUILayout.Width(48f));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(explosionBase, "Inspector");
                                    explosionBase.edit_autoGenerate = edit_autoGenerate;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }
            #endregion
            #region Bake
            Inspector_Bake();
            #endregion
            #region Settings
            {
                explosionBase.edit_settingsFoldout = EditorGUILayout.Foldout(explosionBase.edit_settingsFoldout, "Settings", guiStyleFoldoutBold);
                if (explosionBase.edit_settingsFoldout)
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    EditorGUILayout.BeginVertical();
                    {
                        #region Auto Set Explosion Center
                        {
                            EditorGUI.BeginChangeCheck();
                            var edit_autoSetExplosionCenter = EditorGUILayout.Toggle("Auto Set Explosion Center", explosionBase.edit_autoSetExplosionCenter);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(explosionBase, "Inspector");
                                explosionBase.edit_autoSetExplosionCenter = edit_autoSetExplosionCenter;
                                explosionCore.SetExplosionCenter();
                                ForceRepaint();
                            }
                        }
                        #endregion
                        #region Explosion Center
                        if (!explosionBase.edit_autoSetExplosionCenter)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUI.BeginChangeCheck();
                            var edit_explosionCenter = EditorGUILayout.Vector3Field("Explosion Center", explosionBase.edit_explosionCenter);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(explosionBase, "Inspector");
                                explosionBase.edit_explosionCenter = edit_explosionCenter;
                                explosionCore.SetExplosionCenter();
                                ForceRepaint();
                            }
                            EditorGUI.indentLevel--;
                        }
                        #endregion
                        #region Explosion Rotate
                        {
                            EditorGUI.BeginChangeCheck();
                            var edit_explosionRotate = EditorGUILayout.Slider("Explosion Rotate", explosionBase.edit_explosionRotate, 0f, 1f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(explosionBase, "Inspector");
                                explosionBase.edit_explosionRotate = edit_explosionRotate;
                                explosionBase.SetExplosionRotate(explosionBase.edit_explosionRotate);
                                ForceRepaint();
                            }
                        }
                        #endregion
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }
            #endregion
            #region Preview
            {
                explosionBase.edit_previewFoldout = EditorGUILayout.Foldout(explosionBase.edit_previewFoldout, "Preview", guiStyleFoldoutBold);
                if (explosionBase.edit_previewFoldout)
                {
                    EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUI.BeginChangeCheck();
                        var edit_explosionLifeTime = EditorGUILayout.Slider("Life Time", explosionBase.edit_explosionLifeTime, 0f, 10f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(explosionBase, "Inspector");
                            explosionBase.edit_explosionLifeTime = edit_explosionLifeTime;
                            explosionBase.edit_explosionPlay = false;
                            explosionBase.SetExplosionRate(explosionBase.edit_explosionRate);
                            ForceRepaint();
                        }
                    }
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUI.BeginChangeCheck();
                            var edit_explosionTime = EditorGUILayout.Slider("Time Line", explosionBase.edit_explosionTime, 0f, explosionBase.edit_explosionLifeTime);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(explosionBase, "Inspector");
                                explosionBase.edit_explosionTime = edit_explosionTime;
                                explosionBase.edit_explosionPlay = false;
                                ForceRepaint();
                            }
                        }
                        {
                            if(GUILayout.Button(playIcon, explosionBase.edit_explosionPlay ? guiStylePlayButtonActive : guiStylePlayButton))
                            {
                                explosionBase.edit_explosionPlay = !explosionBase.edit_explosionPlay;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                }

                if (!EditorApplication.isPlaying)
                {
                    explosionBase.SetExplosionRate(explosionBase.edit_explosionRate);
                }
            }
            #endregion

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void Inspector_MeshMaterial() { }

        protected virtual void Inspector_Bake() { }

        protected virtual void OnSceneGUI()
        {
            if (explosionBase == null || explosionCore == null) return;

            #region Auto Generate
            if (explosionBase.edit_autoGenerate)
            {
                if (explosionBase.edit_fileRefreshLastTimeTicks != explosionCore.voxelBase.fileRefreshLastTimeTicks)
                {
                    explosionCore.Generate();
                    //unity bug???
                    EditorApplication.delayCall += () =>
                    {
                        explosionCore.SetMaterialProperties();
                    };
                }
            }
            #endregion
            
            #region Play
            if (explosionBase.edit_explosionPlay)
            {
                if (explosionBase.edit_explosionTime < explosionBase.edit_explosionLifeTime)
                {
                    explosionBase.edit_explosionTime += Time.fixedDeltaTime;
                    explosionBase.edit_explosionTime = Mathf.Min(explosionBase.edit_explosionTime, explosionBase.edit_explosionLifeTime);
                }
                else
                {
                    explosionBase.edit_explosionTime = 0f;
                    explosionBase.edit_explosionPlay = false;
                }
                explosionBase.SetExplosionRate(explosionBase.edit_explosionRate);
                ForceRepaint();
                Repaint();
            }
            #endregion
        }

        private void ForceRepaint()
        {
            //unity bug???
            {
                explosionBase.enabled = !explosionBase.enabled;
                explosionBase.enabled = !explosionBase.enabled;
            }
        }
    }
}
