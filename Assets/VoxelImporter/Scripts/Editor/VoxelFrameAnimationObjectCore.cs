using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace VoxelImporter
{
    public class VoxelFrameAnimationObjectCore : VoxelBaseCore
    {
        public VoxelFrameAnimationObjectCore(VoxelBase target) : base(target)
        {
            voxelObject = target as VoxelFrameAnimationObject;
        }

        public VoxelFrameAnimationObject voxelObject { get; protected set; }

        #region AtlasRects
        protected Rect[] atlasRects;
        protected AtlasRectTable atlasRectTable;
        #endregion

        #region Chunk
        protected struct ChunkArea
        {
            public IntVector3 min;
            public IntVector3 max;

            public Vector3 minf { get { return new Vector3(min.x, min.y, min.z); } }
            public Vector3 maxf { get { return new Vector3(max.x, max.y, max.z); } }
            public Vector3 centerf { get { return Vector3.Lerp(minf, maxf, 0.5f); } }
        }
        protected class ChunkData
        {
            public int voxelBegin;
            public int voxelEnd;

            public ChunkArea area;

            public VoxelData.FaceAreaTable faceAreaTable;
        }
        protected List<ChunkData> chunkDataList;
        #endregion

        #region CreateVoxel
        public override bool ReCreate()
        {
            ClearFramesIcon();

            return base.ReCreate();
        }
        public override void Reset(string path, UnityEngine.Object obj)
        {
            base.Reset(path, obj);

            voxelObject.frames = new List<VoxelFrameAnimationObject.FrameData>();
            bool done = false;
            if (obj != null && obj is Texture2D)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.spriteImportMode == SpriteImportMode.Multiple)
                    {
                        for (int i = 0; i < sprites.Length; i++)
                        {
                            if (sprites[i] is Sprite)
                                voxelObject.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path, voxelFileObject = sprites[i] });
                        }
                        done = true;
                    }
                }
            }
            if(!done)
            {
                voxelObject.frames.Add(new VoxelFrameAnimationObject.FrameData() { voxelFilePath = path, voxelFileObject = obj });
            }

            voxelObject.edit_frameIndex = 0;
        }
        public override bool IsVoxelFileExists()
        {
            var fileExists = true;
            if (voxelObject.frames != null)
            {
                for (int i = 0; i < voxelObject.frames.Count; i++)
                {
                    bool tmp = false;
                    if (!string.IsNullOrEmpty(voxelObject.frames[i].voxelFilePath))
                    {
                        tmp = File.Exists(voxelObject.frames[i].voxelFilePath);
                    }
                    if (!tmp)
                    {
                        if (voxelObject.frames[i].voxelFileObject != null && AssetDatabase.Contains(voxelObject.frames[i].voxelFileObject))
                        {
                            tmp = true;
                        }
                    }
                    if (!tmp)
                        fileExists = false;
                }
            }
            else
            {
                fileExists = false;
            }
            return fileExists;
        }
        protected override bool LoadVoxelData()
        {
            bool result = true;
            if(voxelObject.frames != null && voxelObject.frames.Count > 0)
            {
                for (int i = 0; i < voxelObject.frames.Count; i++)
                {
                    voxelObject.voxelFilePath = voxelObject.frames[i].voxelFilePath;
                    voxelObject.voxelFileObject = voxelObject.frames[i].voxelFileObject;

                    if (!base.LoadVoxelData())
                        result = false;

                    voxelObject.frames[i].fileType = voxelObject.fileType;
                    voxelObject.frames[i].localOffset = voxelObject.localOffset;
                    voxelObject.frames[i].voxelData = voxelObject.voxelData;
                }
                voxelObject.voxelFilePath = voxelObject.frames[0].voxelFilePath;
                voxelObject.voxelFileObject = voxelObject.frames[0].voxelFileObject;
                voxelObject.fileType = voxelObject.frames[0].fileType;
                voxelObject.localOffset = voxelObject.frames[0].localOffset;
                voxelObject.voxelData = voxelObject.frames[0].voxelData;
            }
            else
            {
                result = false;
            }
            return result;
        }
        public override string GetDefaultPath()
        {
            var path = base.GetDefaultPath();
            if (voxelObject != null)
            {
                if (voxelObject.materials != null)
                {
                    for (int i = 0; i < voxelObject.materials.Count; i++)
                    {
                        if (AssetDatabase.Contains(voxelObject.materials[i]))
                        {
                            var assetPath = AssetDatabase.GetAssetPath(voxelObject.materials[i]);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                path = Path.GetDirectoryName(assetPath);
                            }
                        }
                    }
                }
                if (voxelObject.atlasTexture != null && AssetDatabase.Contains(voxelObject.atlasTexture))
                {
                    var assetPath = AssetDatabase.GetAssetPath(voxelObject.atlasTexture);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        path = Path.GetDirectoryName(assetPath);
                    }
                }
                if (voxelObject.frames != null)
                {
                    for (int i = 0; i < voxelObject.frames.Count; i++)
                    {
                        if (voxelObject.frames[i].mesh != null && AssetDatabase.Contains(voxelObject.frames[i].mesh))
                        {
                            var assetPath = AssetDatabase.GetAssetPath(voxelObject.frames[i].mesh);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                path = Path.GetDirectoryName(assetPath);
                            }
                        }
                    }
                }
            }
            return path;
        }
        #endregion

        #region CreateMesh
        protected override bool CreateMesh()
        {
            #region ProgressBar
            const float MaxProgressCount = 7f;
            float ProgressCount = 0;
            Action<string> DisplayProgressBar = (info) =>
            {
                if (!EditorApplication.isPlaying && voxelBase.voxelData.voxels.Length > 10000)
                    EditorUtility.DisplayProgressBar("Create Mesh...", string.Format("{0} / {1}", ProgressCount, MaxProgressCount), (ProgressCount++ / MaxProgressCount));
            };
            #endregion

            DisplayProgressBar("");

            #region Combine VoxelData
            {
                voxelBase.voxelData = new VoxelData();
                voxelBase.voxelData.chunkTable = new DataTable3<IntVector3>(voxelBase.voxelData.voxelSize.x, voxelBase.voxelData.voxelSize.y, voxelBase.voxelData.voxelSize.z);

                chunkDataList = new List<ChunkData>(voxelObject.frames.Count);
                List<VoxelData.Voxel> voxels = new List<VoxelData.Voxel>();
                IntVector3 voxelSize = IntVector3.zero;
                Dictionary<Color, int> paletteTable = new Dictionary<Color, int>();
                int offset = 0;
                for (int i = 0; i < voxelObject.frames.Count; i++)
                {
                    chunkDataList.Add(new ChunkData());
                    chunkDataList[i].voxelBegin = voxels.Count;
                    for (int j = 0; j < voxelObject.frames[i].voxelData.voxels.Length; j++)
                    {
                        var voxel = voxelObject.frames[i].voxelData.voxels[j];
                        var color = voxelObject.frames[i].voxelData.palettes[voxel.palette];
                        if (!paletteTable.ContainsKey(color))
                            paletteTable.Add(color, paletteTable.Count);
                        voxel.palette = paletteTable[color];
                        voxel.z += offset;
                        voxels.Add(voxel);
                        voxelBase.voxelData.chunkTable.Set(voxel.position, new IntVector3(i, 0, 0));
                    }
                    chunkDataList[i].voxelEnd = voxels.Count;
                    chunkDataList[i].area = new ChunkArea() { min = new IntVector3(0, 0, offset), max = new IntVector3(voxelObject.frames[i].voxelData.voxelSize.x, voxelObject.frames[i].voxelData.voxelSize.y, offset + voxelObject.frames[i].voxelData.voxelSize.z) };
                    voxelSize = IntVector3.Max(voxelSize, new IntVector3(voxelObject.frames[i].voxelData.voxelSize.x, voxelObject.frames[i].voxelData.voxelSize.y, offset + voxelObject.frames[i].voxelData.voxelSize.z));
                    offset += voxelObject.frames[i].voxelData.voxelSize.z + 1;
                }
                #region Create
                voxelBase.localOffset = Vector3.zero;

                voxelBase.fileType = VoxelBase.FileType.vox;

                voxelBase.voxelData.voxels = voxels.ToArray();
                voxelBase.voxelData.palettes = new Color[paletteTable.Count];
                foreach (var pair in paletteTable)
                    voxelBase.voxelData.palettes[pair.Value] = pair.Key;
                voxelBase.voxelData.voxelSize = voxelSize;

                voxelBase.voxelData.CreateVoxelTable();
                UpdateVisibleFlags();
                #endregion
            }
            #endregion

            DisplayProgressBar("");

            #region Combine MaterialData
            {
                #region Erase
                for (int i = 0; i < voxelObject.frames.Count; i++)
                {
                    if (voxelObject.frames[i].materialData == null) continue;
                    for (int j = 0; j < voxelObject.frames[i].materialData.Count; j++)
                    {
                        List<IntVector3> removeList = new List<IntVector3>();
                        voxelObject.frames[i].materialData[j].AllAction((pos) =>
                        {
                            if (voxelObject.frames[i].voxelData.VoxelTableContains(pos) < 0)
                            {
                                removeList.Add(pos);
                            }
                        });
                        for (int k = 0; k < removeList.Count; k++)
                        {
                            voxelObject.frames[i].materialData[j].RemoveMaterial(removeList[k]);
                        }
                    }
                }
                #endregion
                voxelObject.materialData = new List<MaterialData>();
                int materialCount = 1;
                for (int i = 0; i < voxelObject.frames.Count; i++)
                {
                    if (voxelObject.frames[i].materialData != null)
                        materialCount = Math.Max(materialCount, voxelObject.frames[i].materialData.Count);
                }
                for (int i = 0; i < voxelObject.frames.Count; i++)
                {
                    if (voxelObject.frames[i].materialData == null)
                        voxelObject.frames[i].materialData = new List<MaterialData>();
                    for (int j = voxelObject.frames[i].materialData.Count; j < materialCount; j++)
                    {
                        voxelObject.frames[i].materialData.Add(new MaterialData());
                    }
                }
                for (int i = 0; i < materialCount; i++)
                {
                    voxelObject.materialData.Add(new MaterialData());
                    voxelObject.materialData[i].name = voxelObject.frames[0].materialData[i].name;
                    voxelObject.materialData[i].transparent = voxelObject.frames[0].materialData[i].transparent;
                    for (int j = 0; j < voxelObject.frames.Count; j++)
                    {
                        if (voxelObject.frames[j].materialData[i] == null) continue;
                        voxelObject.frames[j].materialData[i].AllAction((pos) =>
                        {
                            voxelObject.materialData[i].SetMaterial(chunkDataList[j].area.min + pos);
                        });
                    }
                }
            }
            #endregion

            DisplayProgressBar("");

            #region Material
            {
                if (voxelBase.materialData == null)
                    voxelBase.materialData = new List<MaterialData>();
                if (voxelBase.materialData.Count == 0)
                    voxelBase.materialData.Add(null);
                for (int i = 0; i < voxelBase.materialData.Count; i++)
                {
                    if (voxelBase.materialData[i] == null)
                        voxelBase.materialData[i] = new MaterialData();
                }
                if (voxelObject.materials == null)
                    voxelObject.materials = new List<Material>();
                if (voxelObject.materials.Count < voxelObject.materialData.Count)
                {
                    for (int i = voxelObject.materials.Count; i < voxelObject.materialData.Count; i++)
                        voxelObject.materials.Add(null);
                }
                else if (voxelObject.materials.Count > voxelObject.materialData.Count)
                {
                    voxelObject.materials.RemoveRange(voxelObject.materialData.Count, voxelObject.materials.Count - voxelObject.materialData.Count);
                }
            }
            voxelBase.CreateMaterialIndexTable();
            #endregion

            CalcDataCreate(voxelBase.voxelData.voxels);

            #region UpdateVoxelVisibleFlag
            {
                for (int i = 0; i < voxelBase.voxelData.voxels.Length; i++)
                {
                    int index;
                    VoxelBase.Face faceFlags = (VoxelBase.Face)0;
                    index = voxelBase.voxelData.VoxelTableContains(voxelBase.voxelData.voxels[i].x, voxelBase.voxelData.voxels[i].y, voxelBase.voxelData.voxels[i].z + 1);
                    if (index < 0)
                        faceFlags |= VoxelBase.Face.forward;
                    index = voxelBase.voxelData.VoxelTableContains(voxelBase.voxelData.voxels[i].x, voxelBase.voxelData.voxels[i].y + 1, voxelBase.voxelData.voxels[i].z);
                    if (index < 0)
                        faceFlags |= VoxelBase.Face.up;
                    index = voxelBase.voxelData.VoxelTableContains(voxelBase.voxelData.voxels[i].x + 1, voxelBase.voxelData.voxels[i].y, voxelBase.voxelData.voxels[i].z);
                    if (index < 0)
                        faceFlags |= VoxelBase.Face.right;
                    index = voxelBase.voxelData.VoxelTableContains(voxelBase.voxelData.voxels[i].x - 1, voxelBase.voxelData.voxels[i].y, voxelBase.voxelData.voxels[i].z);
                    if (index < 0)
                        faceFlags |= VoxelBase.Face.left;
                    index = voxelBase.voxelData.VoxelTableContains(voxelBase.voxelData.voxels[i].x, voxelBase.voxelData.voxels[i].y - 1, voxelBase.voxelData.voxels[i].z);
                    if (index < 0)
                        faceFlags |= VoxelBase.Face.down;
                    index = voxelBase.voxelData.VoxelTableContains(voxelBase.voxelData.voxels[i].x, voxelBase.voxelData.voxels[i].y, voxelBase.voxelData.voxels[i].z - 1);
                    if (index < 0)
                        faceFlags |= VoxelBase.Face.back;
                    voxelBase.voxelData.voxels[i].visible = faceFlags;
                }
            }
            #endregion

            #region CreateFaceAreaTable
            {
                for (int i = 0; i < chunkDataList.Count; i++)
                {
                    VoxelData.Voxel[] voxels = new VoxelData.Voxel[chunkDataList[i].voxelEnd - chunkDataList[i].voxelBegin];
                    Array.Copy(voxelBase.voxelData.voxels, chunkDataList[i].voxelBegin, voxels, 0, voxels.Length);
                    chunkDataList[i].faceAreaTable = CreateFaceArea(voxels);
                }
            }
            #endregion

            #region CreateTexture
            {
                var tmpFaceAreaTable = new VoxelData.FaceAreaTable();
                for (int i = 0; i < chunkDataList.Count; i++)
                {
                    tmpFaceAreaTable.Merge(chunkDataList[i].faceAreaTable);
                }
                {
                    var atlasTextureTmp = voxelObject.atlasTexture;
                    if (!CreateTexture(tmpFaceAreaTable, voxelBase.voxelData.palettes, ref atlasRectTable, ref atlasTextureTmp, ref atlasRects))
                    {
                        EditorUtility.ClearProgressBar();
                        return false;
                    }
                    voxelObject.atlasTexture = atlasTextureTmp;
                    {
                        if (voxelObject.materialData == null)
                            voxelObject.materialData = new List<MaterialData>();
                        if (voxelObject.materialData.Count == 0)
                            voxelObject.materialData.Add(null);
                        for (int i = 0; i < voxelObject.materialData.Count; i++)
                        {
                            if (voxelObject.materialData[i] == null)
                                voxelObject.materialData[i] = new MaterialData();
                        }
                        if (voxelObject.materials == null)
                            voxelObject.materials = new List<Material>();
                        if (voxelObject.materials.Count < voxelObject.materialData.Count)
                        {
                            for (int i = voxelObject.materials.Count; i < voxelObject.materialData.Count; i++)
                                voxelObject.materials.Add(null);
                        }
                        else if (voxelObject.materials.Count > voxelObject.materialData.Count)
                        {
                            voxelObject.materials.RemoveRange(voxelObject.materialData.Count, voxelObject.materials.Count - voxelObject.materialData.Count);
                        }
                        for (int i = 0; i < voxelObject.materials.Count; i++)
                        {
                            if (voxelObject.materials[i] == null)
                                voxelObject.materials[i] = new Material(Shader.Find("Standard"));
                            if (voxelBase.updateMaterialTexture)
                            {
                                voxelObject.materials[i].mainTexture = voxelObject.atlasTexture;
                            }
                        }
                    }
                }
            }
            #endregion

            #region CreateMesh
            DisplayProgressBar("");
            if (voxelObject.importMode == VoxelBase.ImportMode.LowPoly)
            {
                int forward = 0;
                int up = 0;
                int right = 0;
                int left = 0;
                int down = 0;
                int back = 0;
                for (int i = 0; i < chunkDataList.Count; i++)
                {
                    AtlasRectTable atlasRectTableTmp = new AtlasRectTable();
                    {
                        atlasRectTableTmp.forward = atlasRectTable.forward.GetRange(forward, chunkDataList[i].faceAreaTable.forward.Count);
                        forward += chunkDataList[i].faceAreaTable.forward.Count;
                        atlasRectTableTmp.up = atlasRectTable.up.GetRange(up, chunkDataList[i].faceAreaTable.up.Count);
                        up += chunkDataList[i].faceAreaTable.up.Count;
                        atlasRectTableTmp.right = atlasRectTable.right.GetRange(right, chunkDataList[i].faceAreaTable.right.Count);
                        right += chunkDataList[i].faceAreaTable.right.Count;
                        atlasRectTableTmp.left = atlasRectTable.left.GetRange(left, chunkDataList[i].faceAreaTable.left.Count);
                        left += chunkDataList[i].faceAreaTable.left.Count;
                        atlasRectTableTmp.down = atlasRectTable.down.GetRange(down, chunkDataList[i].faceAreaTable.down.Count);
                        down += chunkDataList[i].faceAreaTable.down.Count;
                        atlasRectTableTmp.back = atlasRectTable.back.GetRange(back, chunkDataList[i].faceAreaTable.back.Count);
                        back += chunkDataList[i].faceAreaTable.back.Count;
                    }
                    var extraOffset = new Vector3(0, 0f, -chunkDataList[i].area.min.z);
                    voxelBase.localOffset = voxelObject.frames[i].localOffset;
                    voxelObject.frames[i].mesh = CreateMeshOnly(voxelObject.frames[i].mesh, chunkDataList[i].faceAreaTable, voxelObject.atlasTexture, atlasRects, atlasRectTableTmp, extraOffset, out voxelObject.frames[i].materialIndexes);
                }
            }
            else
            {
                for (int i = 0; i < chunkDataList.Count; i++)
                {
                    var extraOffset = new Vector3(0, 0f, -chunkDataList[i].area.min.z);
                    voxelBase.localOffset = voxelObject.frames[i].localOffset;
                    voxelObject.frames[i].mesh = CreateMeshOnly(voxelObject.frames[i].mesh, chunkDataList[i].faceAreaTable, voxelObject.atlasTexture, atlasRects, atlasRectTable, extraOffset, out voxelObject.frames[i].materialIndexes);
                }
            }
            #endregion

            DisplayProgressBar("");
            if (voxelBase.generateLightmapUVs)
            {
                for (int i = 0; i < chunkDataList.Count; i++)
                {
                    if (voxelObject.frames[i].mesh.uv.Length > 0)
                        Unwrapping.GenerateSecondaryUVSet(voxelObject.frames[i].mesh);
                }
            }
            
            DisplayProgressBar("");

            SetRendererCompornent();

            CreateMeshAfterFree();

            RefreshCheckerSave();

            EditorUtility.ClearProgressBar();

            voxelObject.Edit_SetFrameCurrentVoxelMaterialData();

            return true;
        }
        protected override void CreateMeshAfterFree()
        {
            base.CreateMeshAfterFree();

            chunkDataList = null;

            GC.Collect();
        }
        public override void SetRendererCompornent()
        {
            if (voxelBase.updateMaterialTexture)
            {
                if (voxelObject.materials != null)
                {
                    for (int i = 0; i < voxelObject.materials.Count; i++)
                    {
                        if (voxelObject.materials[i] != null)
                        {
                            Undo.RecordObject(voxelObject.materials[i], "Inspector");
                            voxelObject.materials[i].mainTexture = voxelObject.atlasTexture;
                        }
                    }
                }
            }
            SetCurrentMesh();
        }
        public void SetCurrentMesh()
        {
            Undo.RecordObject(voxelObject, "Inspector");
            if (!voxelObject.edit_frameEnable)
                voxelObject.mesh = null;
            else
                voxelObject.mesh = voxelObject.edit_currentFrame.mesh;

            {
                var meshFilter = voxelBase.GetComponent<MeshFilter>();
                Undo.RecordObject(meshFilter, "Inspector");
                meshFilter.sharedMesh = voxelObject.mesh;
            }
            if (voxelBase.updateMeshRendererMaterials)
            {
                var renderer = voxelBase.GetComponent<Renderer>();
                Undo.RecordObject(renderer, "Inspector");
                if (voxelObject.materials != null && voxelObject.edit_frameEnable)
                {
                    Material[] tmps = new Material[voxelObject.edit_currentFrame.materialIndexes.Count];
                    for (int i = 0; i < voxelObject.edit_currentFrame.materialIndexes.Count; i++)
                    {
                        tmps[i] = voxelObject.materials[voxelObject.edit_currentFrame.materialIndexes[i]];
                    }
                    voxelObject.Edit_SetPlayMaterials(tmps);
                    renderer.sharedMaterials = tmps;
                }
                else
                {
                    voxelObject.Edit_ClearPlayMaterials();
                    renderer.sharedMaterial = null;
                }
            }
        }
        public override Mesh[] Edit_CreateMesh(List<VoxelData.Voxel> voxels, List<Edit_VerticesInfo> dstList = null, bool combine = true)
        {
            return new Mesh[1] { Edit_CreateMeshOnly(voxels, null, dstList, combine) };
        }
        #endregion

        #region Preview & Icon
        public void ClearFramesIcon()
        {
            if (voxelObject.frames == null) return;
            for (int i = 0; i < voxelObject.frames.Count; i++)
            {
                if (voxelObject.frames[i] == null) continue;
                voxelObject.frames[i].icon = null;
            }
        }
        #endregion

        #region Undo
        protected override void RefreshCheckerCreate() { voxelObject.refreshChecker = new VoxelFrameAnimationObject.RefreshCheckerFrameAnimation(voxelObject); }
        #endregion
    }
}
