using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    [AddComponentMenu("Voxel Importer/Extra/Explosion/Voxel Chunks Object Explosion")]
    [ExecuteInEditMode, RequireComponent(typeof(VoxelChunksObject))]
    public class VoxelChunksObjectExplosion : VoxelBaseExplosion
    {
#if UNITY_EDITOR
        protected VoxelChunksObject voxelObject { get; private set; }
#endif

        public VoxelChunksObjectChunkExplosion[] chunksExplosion;

        public List<Material> materials;
        public VoxelChunksObject.MaterialMode materialMode;

        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            voxelObject = GetComponent<VoxelChunksObject>();

            UpdatedChunks();
            voxelObject.updatedChunks += UpdatedChunks;
#endif
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

#if UNITY_EDITOR
            voxelObject.updatedChunks -= UpdatedChunks;
#endif
        }
#if UNITY_EDITOR
        private void UpdatedChunks()
        {
            chunksExplosion = new VoxelChunksObjectChunkExplosion[voxelObject.chunks.Length];
            for (int i = 0; i < voxelObject.chunks.Length; i++)
            {
                if (voxelObject.chunks[i] == null) continue;
                chunksExplosion[i] = voxelObject.chunks[i].GetComponent<VoxelChunksObjectChunkExplosion>();
            }
        }
#endif

        public override void SetEnableExplosionObject(bool enable)
        {
            enabled = enable;
            if(chunksExplosion != null)
            {
                for (int i = 0; i < chunksExplosion.Length; i++)
                {
                    if (chunksExplosion[i] == null) continue;
                    chunksExplosion[i].enabled = enable;
                }
            }
        }
        public override void SetEnableRenderer(bool enable)
        {
            if (chunksExplosion != null)
            {
                for (int i = 0; i < chunksExplosion.Length; i++)
                {
                    if (chunksExplosion[i] == null) continue;
                    chunksExplosion[i].SetEnableRenderer(enable);
                }
            }
        }
    }
}

