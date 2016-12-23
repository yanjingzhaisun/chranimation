using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEditorInternal;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    [CustomEditor(typeof(VoxelSkinnedAnimationObject))]
    public class VoxelSkinnedAnimationObjectEditor : VoxelObjectEditor
    {
        public VoxelSkinnedAnimationObject animationTarget { get; private set; }
        public VoxelSkinnedAnimationObjectCore animationCore { get; protected set; }

        public override Mesh mesh { get { return animationTarget.mesh; } set { animationTarget.mesh = value; } }
        public override List<Material> materials { get { return animationTarget.materials; } set { animationTarget.materials = value; } }
        public override Texture2D atlasTexture { get { return animationTarget.atlasTexture; } set { animationTarget.atlasTexture = value; } }

        protected override void OnEnable()
        {
            base.OnEnable();

            animationTarget = target as VoxelSkinnedAnimationObject;
            if (animationTarget == null) return;
            baseCore = objectCore = animationCore = new VoxelSkinnedAnimationObjectCore(animationTarget);
            OnEnableInitializeSet();
        }

        protected override void InspectorGUI()
        {
            if (animationTarget == null) return;

            base.InspectorGUI();
            
            Action<UnityEngine.Object, string> TypeTitle = (o, title) =>
            {
                if (o == null)
                    EditorGUILayout.LabelField(title, guiStyleMagentaBold);
                else
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            };

            #region Animation
            if (!string.IsNullOrEmpty(baseTarget.voxelFilePath))
            {
                animationTarget.edit_animationFoldout = EditorGUILayout.Foldout(animationTarget.edit_animationFoldout, "Animation", guiStyleFoldoutBold);
                if (animationTarget.edit_animationFoldout)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        EditorGUILayout.BeginHorizontal();
                        TypeTitle(animationTarget.rootBone, "Bone");
                        {
                            EditorGUI.BeginDisabledGroup(animationTarget.rootBone == null);
                            if (GUILayout.Button("Save as template", GUILayout.Width(128)))
                            {
                                #region Save as template
                                string BoneTemplatesPath = Application.dataPath + "/VoxelImporter/Scripts/Editor/BoneTemplates";
                                string path = EditorUtility.SaveFilePanel("Save as template", BoneTemplatesPath, string.Format("{0}.asset", baseTarget.gameObject.name), "asset");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    if (path.IndexOf(Application.dataPath) < 0)
                                    {
                                        SaveInsideAssetsFolderDisplayDialog();
                                    }
                                    else
                                    {
                                        path = path.Replace(Application.dataPath, "Assets");
                                        var boneTemplate = ScriptableObject.CreateInstance<BoneTemplate>();
                                        boneTemplate.Set(animationTarget.rootBone);
                                        AssetDatabase.CreateAsset(boneTemplate, path);
                                    }
                                }
                                #endregion
                            }
                            EditorGUI.EndDisabledGroup();
                            if (GUILayout.Button("Create", guiStyleDropDown, GUILayout.Width(64)))
                            {
                                #region Create
                                VoxelHumanoidConfigreAvatar.Destroy();

                                Dictionary<string, BoneTemplate> boneTemplates = new Dictionary<string, BoneTemplate>();
                                {
                                    {
                                        var boneTemplate = ScriptableObject.CreateInstance<BoneTemplate>();
                                        boneTemplate.boneInitializeData.Add(new BoneTemplate.BoneInitializeData() { name = "Root" });
                                        boneTemplate.boneInitializeData.Add(new BoneTemplate.BoneInitializeData() { name = "Bone", parentName = "Root", position = new Vector3(0f, 2f, 0f) });
                                        boneTemplates.Add("Default", boneTemplate);
                                    }
                                    {
                                        var guids = AssetDatabase.FindAssets("t:bonetemplate");
                                        for (int i = 0; i < guids.Length; i++)
                                        {
                                            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                                            var boneTemplate = AssetDatabase.LoadAssetAtPath<BoneTemplate>(path);
                                            if (boneTemplate == null) continue;
                                            var name = path.Replace("Assets/", "");
                                            boneTemplates.Add(name, boneTemplate);
                                        }
                                    }
                                }

                                Action<BoneTemplate> MenuCallback = (boneTemplate) =>
                                {
                                    GameObject goRoot = baseTarget.gameObject;
                                    VoxelBase clRoot = baseTarget;

                                    if (prefabType == PrefabType.Prefab)
                                    {
                                        goRoot = (GameObject)PrefabUtility.InstantiatePrefab(baseTarget.gameObject);
                                        clRoot = goRoot.GetComponent<VoxelBase>();
                                    }

                                    {
                                        var bones = clRoot.GetComponentsInChildren<VoxelSkinnedAnimationObjectBone>();
                                        for (int i = 0; i < bones.Length; i++)
                                        {
                                            for (int j = 0; j < bones[i].transform.childCount; j++)
                                            {
                                                var child = bones[i].transform.GetChild(j);
                                                if (child.GetComponent<VoxelSkinnedAnimationObjectBone>() == null)
                                                {
                                                    Undo.SetTransformParent(child, animationTarget.transform, "Create Bone");
                                                    i--;
                                                }
                                            }
                                        }
                                        for (int i = 0; i < bones.Length; i++)
                                        {
                                            if (bones[i] == null || bones[i].gameObject == null) continue;
                                            Undo.DestroyObjectImmediate(bones[i].gameObject);
                                        }
                                    }

                                    {
                                        List<GameObject> createList = new List<GameObject>();
                                        for (int i = 0; i < boneTemplate.boneInitializeData.Count; i++)
                                        {
                                            var tp = boneTemplate.boneInitializeData[i];
                                            GameObject go = new GameObject(tp.name);
                                            Undo.RegisterCreatedObjectUndo(go, "Create Bone");
                                            var bone = Undo.AddComponent<VoxelSkinnedAnimationObjectBone>(go);
                                            {
                                                bone.edit_disablePositionAnimation = tp.disablePositionAnimation;
                                                bone.edit_disableRotationAnimation = tp.disableRotationAnimation;
                                                bone.edit_disableScaleAnimation = tp.disableScaleAnimation;
                                                bone.edit_mirrorSetBoneAnimation = tp.mirrorSetBoneAnimation;
                                                bone.edit_mirrorSetBonePosition = tp.mirrorSetBonePosition;
                                                bone.edit_mirrorSetBoneWeight = tp.mirrorSetBoneWeight;
                                            }
                                            if (string.IsNullOrEmpty(tp.parentName))
                                            {
                                                Undo.SetTransformParent(go.transform, goRoot.transform, "Create Bone");
                                            }
                                            else
                                            {
                                                int parentIndex = createList.FindIndex(a => a.name == tp.parentName);
                                                Debug.Assert(parentIndex >= 0);
                                                GameObject parent = createList[parentIndex];
                                                Assert.IsNotNull(parent);
                                                Undo.SetTransformParent(go.transform, parent.transform, "Create Bone");
                                            }
                                            go.transform.localPosition = tp.position;
                                            go.transform.localRotation = Quaternion.identity;
                                            go.transform.localScale = Vector3.one;
                                            createList.Add(go);
                                        }
                                    }
                                    animationTarget.humanDescription.firstAutomapDone = false;
                                    Refresh();

                                    if (prefabType == PrefabType.Prefab)
                                    {
                                        PrefabUtility.ReplacePrefab(goRoot, PrefabUtility.GetPrefabParent(goRoot), ReplacePrefabOptions.ConnectToPrefab);
                                        DestroyImmediate(goRoot);
                                    }
                                };
                                GenericMenu menu = new GenericMenu();
                                {
                                    var enu = boneTemplates.GetEnumerator();
                                    while (enu.MoveNext())
                                    {
                                        var value = enu.Current.Value;
                                        menu.AddItem(new GUIContent(enu.Current.Key), false, () => { MenuCallback(value); });
                                    }
                                }
                                menu.ShowAsContext();
                                #endregion
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        {
                            EditorGUI.indentLevel++;
                            if (animationTarget.rootBone != null)
                            {
                                #region Root
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        Action FixMissingAnimation = () =>
                                        {
                                            var animator = animationTarget.GetComponent<Animator>();
                                            if (animator != null && animator.runtimeAnimatorController != null)
                                            {
                                                Undo.RecordObject(animator, "Fix Missing Animation");
                                                Undo.RecordObject(animator.runtimeAnimatorController, "Fix Missing Animation");
                                                Undo.RecordObjects(animator.runtimeAnimatorController.animationClips, "Fix Missing Animation");
                                                for (int i = 0; i < animationTarget.bones.Length; i++)
                                                {
                                                    var boneCore = new VoxelSkinnedAnimationObjectBoneCore(animationTarget.bones[i], animationCore);
                                                    boneCore.FixMissingAnimation();
                                                }
                                            }
                                        };

                                        EditorGUILayout.LabelField("Root");
                                        #region Add Root Bone
                                        {
                                            if (GUILayout.Button("Add Root Bone"))
                                            {
                                                var beforeRoot = animationTarget.rootBone.GetComponent<VoxelSkinnedAnimationObjectBone>();
                                                Undo.RecordObject(beforeRoot, "Add Root Bone");
                                                GameObject go = new GameObject("Root");
                                                Undo.RegisterCreatedObjectUndo(go, "Add Root Bone");
                                                Undo.AddComponent<VoxelSkinnedAnimationObjectBone>(go);
                                                Undo.SetTransformParent(go.transform, animationTarget.transform, "Add Root Bone");
                                                go.transform.localPosition = Vector3.zero;
                                                go.transform.localRotation = Quaternion.identity;
                                                go.transform.localScale = Vector3.one;
                                                Undo.SetTransformParent(animationTarget.rootBone, go.transform, "Add Root Bone");
                                                EditorGUIUtility.PingObject(go);
                                                animationCore.UpdateBoneWeight();
                                                FixMissingAnimation();
                                                #region FixBoneWeight
                                                for (int i = 0; i < animationTarget.voxelData.voxels.Length; i++)
                                                {
                                                    var pos = animationTarget.voxelData.voxels[i].position;
                                                    for (var vindex = (VoxelBase.VoxelVertexIndex)0; vindex < VoxelBase.VoxelVertexIndex.Total; vindex++)
                                                    {
                                                        var weight = animationCore.GetBoneWeight(pos, vindex);
                                                        var power = 0f;
                                                        if (weight.boneIndex0 == 0 && weight.weight0 > 0f)
                                                            power = weight.weight0;
                                                        else if (weight.boneIndex1 == 0 && weight.weight1 > 0f)
                                                            power = weight.weight1;
                                                        else if (weight.boneIndex2 == 0 && weight.weight2 > 0f)
                                                            power = weight.weight2;
                                                        else if (weight.boneIndex3 == 0 && weight.weight3 > 0f)
                                                            power = weight.weight3;
                                                        if (power <= 0f) continue;
                                                        var weights = beforeRoot.weightData.GetWeight(pos);
                                                        if (weights == null)
                                                            weights = new WeightData.VoxelWeight();
                                                        weights.SetWeight(vindex, power);
                                                        beforeRoot.weightData.SetWeight(pos, weights);
                                                    }
                                                }
                                                #endregion
                                                Refresh();
                                                InternalEditorUtility.RepaintAllViews();
                                            }
                                        }
                                        #endregion
                                        #region Remove Root Bone
                                        {
                                            bool disabled = false;
                                            {
                                                int count = 0;
                                                for (int i = 0; i < animationTarget.rootBone.childCount; i++)
                                                {
                                                    var child = animationTarget.rootBone.GetChild(i);
                                                    if (child.GetComponent<VoxelSkinnedAnimationObjectBone>() != null)
                                                        count++;
                                                }
                                                disabled = count != 1;
                                            }
                                            EditorGUI.BeginDisabledGroup(disabled);
                                            if (GUILayout.Button("Remove Root Bone"))
                                            {
                                                for (int i = 0; i < animationTarget.rootBone.childCount; i++)
                                                {
                                                    var child = animationTarget.rootBone.GetChild(i);
                                                    if (child.GetComponent<VoxelSkinnedAnimationObjectBone>() != null)
                                                        Undo.RecordObject(animationTarget.rootBone, "Remove Root Bone");
                                                    Undo.SetTransformParent(child, animationTarget.transform, "Remove Root Bone");
                                                    i--;
                                                }
                                                Undo.DestroyObjectImmediate(animationTarget.rootBone.gameObject);
                                                animationCore.UpdateBoneBindposes();
                                                EditorGUIUtility.PingObject(animationTarget.rootBone.gameObject);
                                                FixMissingAnimation();
                                                Refresh();
                                                InternalEditorUtility.RepaintAllViews();
                                            }
                                            EditorGUI.EndDisabledGroup();
                                        }
                                        #endregion
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion
                                #region Reset
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField("Reset");
                                    {
                                        if (GUILayout.Button("All"))
                                        {
                                            for (int i = 0; i < animationTarget.bones.Length; i++)
                                            {
                                                Undo.RecordObject(animationTarget.bones[i].transform, "Reset Bone Transform");
                                                if (animationTarget.bones[i].bonePositionSave)
                                                    animationTarget.bones[i].transform.localPosition = animationTarget.bones[i].bonePosition;
                                                animationTarget.bones[i].transform.localRotation = Quaternion.identity;
                                                animationTarget.bones[i].transform.localScale = Vector3.one;
                                            }
                                        }
                                        if (GUILayout.Button("Position"))
                                        {
                                            for (int i = 0; i < animationTarget.bones.Length; i++)
                                            {
                                                Undo.RecordObject(animationTarget.bones[i].transform, "Reset Bone Position");
                                                if (animationTarget.bones[i].bonePositionSave)
                                                    animationTarget.bones[i].transform.localPosition = animationTarget.bones[i].bonePosition;
                                            }
                                        }
                                        if (GUILayout.Button("Rotation"))
                                        {
                                            for (int i = 0; i < animationTarget.bones.Length; i++)
                                            {
                                                Undo.RecordObject(animationTarget.bones[i].transform, "Reset Bone Rotation");
                                                animationTarget.bones[i].transform.localRotation = Quaternion.identity;
                                            }
                                        }
                                        if (GUILayout.Button("Scale"))
                                        {
                                            for (int i = 0; i < animationTarget.bones.Length; i++)
                                            {
                                                Undo.RecordObject(animationTarget.bones[i].transform, "Reset Bone Scale");
                                                animationTarget.bones[i].transform.localScale = Vector3.one;
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion
                                #region Count
                                {
                                    EditorGUILayout.LabelField("Count", animationTarget.rootBone != null ? animationTarget.bones.Length.ToString() : "");
                                }
                                #endregion
                            }
                            if (animationTarget.mesh != null)
                            {
                                if (animationTarget.rootBone == null)
                                {
                                    EditorGUILayout.HelpBox("Bone not found. Please create bone.", MessageType.Error);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    if (animationTarget.rootBone != null)
                    {
                        EditorGUILayout.LabelField("Rig", EditorStyles.boldLabel);
                        {
                            EditorGUI.indentLevel++;
                            {
                                #region updateMeshFilterMesh
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var updateAnimatorAvatar = EditorGUILayout.Toggle("Update the Animator Avatar", animationTarget.updateAnimatorAvatar);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        UndoRecordObject("Inspector");
                                        animationTarget.updateAnimatorAvatar = updateAnimatorAvatar;
                                        baseCore.SetRendererCompornent();
                                    }
                                }
                                #endregion
                                #region AnimationType
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var rigAnimationType = (VoxelSkinnedAnimationObject.RigAnimationType)EditorGUILayout.EnumPopup("Animation Type", animationTarget.rigAnimationType);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        UndoRecordObject("Inspector");
                                        VoxelHumanoidConfigreAvatar.Destroy();
                                        animationTarget.rigAnimationType = rigAnimationType;
                                        animationTarget.humanDescription.firstAutomapDone = false;
                                        Refresh();
                                    }
                                }
                                #endregion
                                #region Avatar
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        EditorGUI.BeginDisabledGroup(true);
                                        EditorGUILayout.ObjectField("Avatar", animationTarget.avatar, typeof(Avatar), false);
                                        EditorGUI.EndDisabledGroup();
                                    }
                                    if (animationTarget.avatar != null)
                                    {
                                        if (!AssetDatabase.Contains(animationTarget.avatar))
                                        {
                                            if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                                            {
                                                #region Create Avatar
                                                string path = EditorUtility.SaveFilePanel("Save avatar", objectCore.GetDefaultPath(), string.Format("{0}_avatar.asset", baseTarget.gameObject.name), "asset");
                                                if (!string.IsNullOrEmpty(path))
                                                {
                                                    if (path.IndexOf(Application.dataPath) < 0)
                                                    {
                                                        EditorUtility.DisplayDialog("Error!", "Please save a lower than \"Assets\"", "ok");
                                                    }
                                                    else
                                                    {
                                                        UndoRecordObject("Save Avatar");
                                                        path = path.Replace(Application.dataPath, "Assets");
                                                        AssetDatabase.CreateAsset(Avatar.Instantiate(animationTarget.avatar), path);
                                                        animationTarget.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
                                                        objectCore.SetRendererCompornent();
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                        {
                                            if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                                            {
                                                #region Reset Avatar
                                                UndoRecordObject("Reset Avatar");
                                                animationTarget.avatar = null;
                                                animationCore.UpdateBoneWeight();
                                                #endregion
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUI.indentLevel++;
                                    if (animationTarget.avatar != null && !animationTarget.avatar.isValid)
                                    {
                                        EditorGUILayout.HelpBox("Invalid mecanim avatar.\nCheck the bone please.", MessageType.Error);
                                    }
                                    #region HelpBox
                                    if (prefabEnable)
                                    {
                                        if (animationTarget.rigAnimationType != VoxelSkinnedAnimationObject.RigAnimationType.Legacy)
                                        {
                                            if (animationTarget.avatar == null || !AssetDatabase.Contains(animationTarget.avatar))
                                            {
                                                EditorGUILayout.HelpBox("Prefab is need save file.\nPlease save Avatar.", MessageType.Error);
                                            }
                                        }
                                    }
                                    #endregion
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                                #region Configre Avatar
                                if (animationTarget.rigAnimationType == VoxelSkinnedAnimationObject.RigAnimationType.Humanoid)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.Space();
                                    if (GUILayout.Button("Configure Avatar", VoxelHumanoidConfigreAvatar.instance == null ? GUI.skin.button : guiStyleBoldActiveButton))
                                    {
                                        if (VoxelHumanoidConfigreAvatar.instance == null)
                                            VoxelHumanoidConfigreAvatar.Create(animationTarget);
                                        else
                                            VoxelHumanoidConfigreAvatar.instance.Close();
                                    }
                                    EditorGUILayout.Space();
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    if (animationTarget.rootBone != null)
                    {
                        TypeTitle(animationTarget.mesh, "Mesh");
                        {
                            EditorGUI.indentLevel++;
                            {
                                #region skinnedMeshBoundsUpdate
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var skinnedMeshBoundsUpdate = EditorGUILayout.ToggleLeft("Update the Skinned Mesh Renderer Bounds", animationTarget.skinnedMeshBoundsUpdate);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        UndoRecordObject("Inspector");
                                        animationTarget.skinnedMeshBoundsUpdate = skinnedMeshBoundsUpdate;
                                        animationCore.UpdateSkinnedMeshBounds();
                                    }
                                }
                                #endregion
                                #region skinnedMeshBoundsUpdateScale
                                if (animationTarget.skinnedMeshBoundsUpdate)
                                {
                                    EditorGUI.indentLevel++;
                                    EditorGUI.BeginChangeCheck();
                                    var skinnedMeshBoundsUpdateScale = EditorGUILayout.Vector3Field("Scale", animationTarget.skinnedMeshBoundsUpdateScale);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        UndoRecordObject("Inspector");
                                        animationTarget.skinnedMeshBoundsUpdateScale = skinnedMeshBoundsUpdateScale;
                                        animationCore.UpdateSkinnedMeshBounds();
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            #endregion

            base.InspectorGUI_Refresh();
        }
        protected override void InspectorGUI_ImportOpenBefore()
        {
            base.InspectorGUI_ImportOpenBefore();

            VoxelHumanoidConfigreAvatar.Destroy();
        }
        protected override void InspectorGUI_ImportOffsetSetExtra(GenericMenu menu)
        {
            #region Feet
            menu.AddItem(new GUIContent("Feet"), false, () =>
            {
                UndoRecordObject("Inspector", true);
                Vector3 importOffset = Vector3.zero;
                #region Calc
                if(baseTarget.voxelData != null)
                {
                    List<Vector3> positions = new List<Vector3>();
                    for (int i = 0; i < baseTarget.voxelData.voxels.Length; i++)
                    {
                        if (baseTarget.voxelData.voxels[i].y != 0) continue;
                        var tmpOffset = baseTarget.importOffset;
                        var tmpScale = baseTarget.importScale;
                        baseTarget.importOffset = Vector3.zero;
                        baseTarget.importScale = Vector3.one;
                        positions.Add(baseCore.GetVoxelCenterPosition(baseTarget.voxelData.voxels[i].position));
                        baseTarget.importOffset = tmpOffset;
                        baseTarget.importScale = tmpScale;
                    }
                    if(positions.Count > 0)
                    {
                        var tmp = Vector3.zero;
                        for (int i = 0; i < positions.Count; i++)
                            tmp += positions[i];
                        tmp /= positions.Count;
                        importOffset = new Vector3(-tmp.x, 0, -tmp.z);
                    }
                }
                #endregion
                baseTarget.importOffset = importOffset;
                Refresh();
            });
            #endregion
        }
        protected override void InspectorGUI_Refresh() { }

        protected override void SaveAllUnsavedAssets()
        {
            ContextSaveAllUnsavedAssets(new MenuCommand(baseTarget));
        }

        [MenuItem("CONTEXT/VoxelSkinnedAnimationObject/Save All Unsaved Assets")]
        private static void ContextSaveAllUnsavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelSkinnedAnimationObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelSkinnedAnimationObjectCore(objectTarget);

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

            #region Avatar
            if (objectTarget.avatar != null && !AssetDatabase.Contains(objectTarget.avatar))
            {
                var path = folder + "/" + string.Format("{0}_avatar.asset", objectTarget.gameObject.name);
                path = path.Replace(Application.dataPath, "Assets");
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CreateAsset(Avatar.Instantiate(objectTarget.avatar), path);
                objectTarget.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            }
            #endregion

            objectCore.SetRendererCompornent();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/VoxelSkinnedAnimationObject/Reset All Assets")]
        private static void ResetAllSavedAssets(MenuCommand menuCommand)
        {
            var objectTarget = menuCommand.context as VoxelSkinnedAnimationObject;
            if (objectTarget == null) return;

            var objectCore = new VoxelSkinnedAnimationObjectCore(objectTarget);

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

            #region Avatar
            objectTarget.avatar = null;
            #endregion

            objectCore.ReCreate();
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
