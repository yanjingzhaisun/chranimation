using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    [AddComponentMenu("Voxel Importer/Extra/Explosion/Voxel Frame Animation Object Explosion")]
    [ExecuteInEditMode, RequireComponent(typeof(VoxelFrameAnimationObject))]
    public class VoxelFrameAnimationObjectExplosion : VoxelBaseExplosion
    {
        protected VoxelFrameAnimationObject voxelObject { get; private set; }

        public List<MeshData> meshes;
        public List<Material> materials;

        protected override void Awake()
        {
            base.Awake();

            voxelObject = GetComponent<VoxelFrameAnimationObject>();
        }

        protected override void DrawMesh()
        {
            if (materials != null && meshes != null)
            {
                var world = transformCache.localToWorldMatrix;
                for (int i = 0; i < meshes.Count; i++)
                {
                    for (int j = 0; j < meshes[i].materialIndexes.Count; j++)
                    {
                        if (j < meshes[i].mesh.subMeshCount)
                            Graphics.DrawMesh(meshes[i].mesh, world, materials[meshes[i].materialIndexes[j]], 0, null, j, materialPropertyBlock);
                    }
                }
            }
        }
    }
}

