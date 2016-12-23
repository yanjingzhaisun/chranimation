using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    [AddComponentMenu("Voxel Importer/Voxel Frame Animation Object")]
    [ExecuteInEditMode, RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
    public class VoxelFrameAnimationObject : VoxelBase
    {
        [Serializable]
        public class FrameData
        {
#if UNITY_EDITOR
            public string voxelFilePath;
            public UnityEngine.Object voxelFileObject;
            public FileType fileType;
            public Vector3 localOffset;

            [NonSerialized]
            public VoxelData voxelData;

            [NonSerialized]
            public Texture2D icon;
            
            public List<MaterialData> materialData;
            public List<int> materialIndexes;
#endif
            public Mesh mesh;
        }

        public List<FrameData> frames;

        public Mesh mesh;
        public List<Material> materials;
        public Texture2D atlasTexture;

        public Material playMaterial0;
        public Material playMaterial1;
        public Material playMaterial2;
        public Material playMaterial3;
        public Material playMaterial4;
        public Material playMaterial5;
        public Material playMaterial6;
        public Material playMaterial7;

        //Play
        private MeshFilter meshFilterCache;
        private Mesh filterMesh;
        private MeshRenderer meshRendererCache;
        private Material[] rendererMaterials;
        private Material rendererMaterial0;
        private Material rendererMaterial1;
        private Material rendererMaterial2;
        private Material rendererMaterial3;
        private Material rendererMaterial4;
        private Material rendererMaterial5;
        private Material rendererMaterial6;
        private Material rendererMaterial7;

        void Awake()
        {
            meshFilterCache = GetComponent<MeshFilter>();
            filterMesh = null;
            meshRendererCache = GetComponent<MeshRenderer>();
            rendererMaterials = null;
        }

        void LateUpdate()
        {
            #region Mesh
            if(filterMesh != mesh)
                meshFilterCache.sharedMesh = filterMesh = mesh;
            #endregion

            #region Material
            if (rendererMaterial0 != playMaterial0 ||
                rendererMaterial1 != playMaterial1 ||
                rendererMaterial2 != playMaterial2 ||
                rendererMaterial3 != playMaterial3 ||
                rendererMaterial4 != playMaterial4 ||
                rendererMaterial5 != playMaterial5 ||
                rendererMaterial6 != playMaterial6 ||
                rendererMaterial7 != playMaterial7)
            {
                int count = 0;
                if (playMaterial0 != null) count++;
                if (playMaterial1 != null) count++;
                if (playMaterial2 != null) count++;
                if (playMaterial3 != null) count++;
                if (playMaterial4 != null) count++;
                if (playMaterial5 != null) count++;
                if (playMaterial6 != null) count++;
                if (playMaterial7 != null) count++;
                //
                if (rendererMaterials == null || rendererMaterials.Length != count)
                    rendererMaterials = new Material[count];
                //
                int i = 0;
                if (playMaterial0 != null) rendererMaterials[i++] = playMaterial0;
                if (playMaterial1 != null) rendererMaterials[i++] = playMaterial1;
                if (playMaterial2 != null) rendererMaterials[i++] = playMaterial2;
                if (playMaterial3 != null) rendererMaterials[i++] = playMaterial3;
                if (playMaterial4 != null) rendererMaterials[i++] = playMaterial4;
                if (playMaterial5 != null) rendererMaterials[i++] = playMaterial5;
                if (playMaterial6 != null) rendererMaterials[i++] = playMaterial6;
                if (playMaterial7 != null) rendererMaterials[i++] = playMaterial7;
                //
                if (rendererMaterials.Length == 0)
                    meshRendererCache.sharedMaterial = null;
                else
                    meshRendererCache.sharedMaterials = rendererMaterials;
                //
                rendererMaterial0 = playMaterial0;
                rendererMaterial1 = playMaterial1;
                rendererMaterial2 = playMaterial2;
                rendererMaterial3 = playMaterial3;
                rendererMaterial4 = playMaterial4;
                rendererMaterial5 = playMaterial5;
                rendererMaterial6 = playMaterial6;
                rendererMaterial7 = playMaterial7;
            }
            #endregion
        }

#if UNITY_EDITOR
        public bool edit_animationFoldout = true;

        public int edit_frameIndex = -1;

        public bool edit_frameEnable { get { return frames != null && edit_frameIndex >= 0 && edit_frameIndex < frames.Count && frames[edit_frameIndex] != null; } }
        public FrameData edit_currentFrame { get { return edit_frameEnable ? frames[edit_frameIndex] : null; } }

        public enum Edit_CameraMode
        {
            forward,
            back,
            up,
            down,
            right,
            left,
        }
        public Edit_CameraMode edit_previewCameraMode;

        public float edit_frameIconSize = 64f;

        public void Edit_SetFrameCurrentVoxelMaterialData()
        {
            if (!edit_frameEnable) return;

            voxelData = edit_currentFrame.voxelData;
            materialData = edit_currentFrame.materialData;
            materialIndexes = edit_currentFrame.materialIndexes;
        }

        public void Edit_ClearPlayMaterials()
        {
            playMaterial0 = null;
            playMaterial1 = null;
            playMaterial2 = null;
            playMaterial3 = null;
            playMaterial4 = null;
            playMaterial5 = null;
            playMaterial6 = null;
            playMaterial7 = null;
        }
        public void Edit_SetPlayMaterials(Material[] mats)
        {
            Edit_ClearPlayMaterials();
            if (mats.Length > 0) playMaterial0 = mats[0];
            if (mats.Length > 1) playMaterial1 = mats[1];
            if (mats.Length > 2) playMaterial2 = mats[2];
            if (mats.Length > 3) playMaterial3 = mats[3];
            if (mats.Length > 4) playMaterial4 = mats[4];
            if (mats.Length > 5) playMaterial5 = mats[5];
            if (mats.Length > 6) playMaterial6 = mats[6];
            if (mats.Length > 7) playMaterial7 = mats[7];
            if (mats.Length > 8)
            {
                Debug.LogWarningFormat("<color=green>[VoxelCharacteImporter]</color> Play material count over. <color=red>{0}/{1}</color>", mats.Length, 8);
            }
        }

        #region Undo
        public class RefreshCheckerFrameAnimation : RefreshChecker
        {
            public RefreshCheckerFrameAnimation(VoxelFrameAnimationObject voxelObject) : base(voxelObject)
            {
                controllerFrameAnimation = voxelObject;
            }

            public VoxelFrameAnimationObject controllerFrameAnimation;

            public override void Save()
            {
                base.Save();
            }
            public override bool Check()
            {
                if (base.Check())
                    return true;

                return false;
            }
        }
        #endregion
#endif
    }
}
