using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace VoxelImporter
{
    public abstract class VoxelBaseExplosionCore
    {
        public VoxelBaseExplosion explosionBase { get; protected set; }

        public VoxelBase voxelBase { get; protected set; }
        public VoxelBaseCore voxelBaseCore { get; protected set; }

        public VoxelBaseExplosionCore(VoxelBaseExplosion target)
        {
            explosionBase = target;
            voxelBase = target.GetComponent<VoxelBase>();
        }

        public abstract void Generate();

        protected void CreateBasicCube(out Vector3 cubeCenter, out List<Vector3> cubeVertices, out List<Vector3> cubeNormals, out List<int> cubeTriangles)
        {
            cubeVertices = new List<Vector3>();
            cubeNormals = new List<Vector3>();
            cubeTriangles = new List<int>();
            {
                var offsetPosition = voxelBase.localOffset + voxelBase.importOffset;
                cubeCenter = Vector3.Scale(voxelBase.importScale, offsetPosition) + voxelBase.importScale / 2f;
                #region forward
                if ((voxelBase.enableFaceFlags & VoxelBase.Face.forward) != 0)
                {
                    var pOffset = Vector3.Scale(voxelBase.importScale, offsetPosition);
                    var vOffset = cubeVertices.Count;
                    cubeVertices.Add(new Vector3(0, voxelBase.importScale.y, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(0, 0, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, 0, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, voxelBase.importScale.y, voxelBase.importScale.z) + pOffset);
                    cubeTriangles.Add(vOffset + 0); cubeTriangles.Add(vOffset + 1); cubeTriangles.Add(vOffset + 2);
                    cubeTriangles.Add(vOffset + 0); cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 3);
                    for (int j = 0; j < 4; j++)
                    {
                        cubeNormals.Add(Vector3.forward);
                    }
                }
                #endregion
                #region up
                if ((voxelBase.enableFaceFlags & VoxelBase.Face.up) != 0)
                {
                    var pOffset = Vector3.Scale(voxelBase.importScale, offsetPosition);
                    var vOffset = cubeVertices.Count;
                    cubeVertices.Add(new Vector3(0, voxelBase.importScale.y, 0) + pOffset);
                    cubeVertices.Add(new Vector3(0, voxelBase.importScale.y, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, voxelBase.importScale.y, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, voxelBase.importScale.y, 0) + pOffset);
                    cubeTriangles.Add(vOffset + 0); cubeTriangles.Add(vOffset + 1); cubeTriangles.Add(vOffset + 2);
                    cubeTriangles.Add(vOffset + 0); cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 3);
                    for (int j = 0; j < 4; j++)
                    {
                        cubeNormals.Add(Vector3.up);
                    }
                }
                #endregion
                #region right
                if ((voxelBase.enableFaceFlags & VoxelBase.Face.right) != 0)
                {
                    var pOffset = Vector3.Scale(voxelBase.importScale, offsetPosition);
                    var vOffset = cubeVertices.Count;
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, 0, 0) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, voxelBase.importScale.y, 0) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, voxelBase.importScale.y, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, 0, voxelBase.importScale.z) + pOffset);
                    cubeTriangles.Add(vOffset + 0); cubeTriangles.Add(vOffset + 1); cubeTriangles.Add(vOffset + 2);
                    cubeTriangles.Add(vOffset + 0); cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 3);
                    for (int j = 0; j < 4; j++)
                    {
                        cubeNormals.Add(Vector3.right);
                    }
                }
                #endregion
                #region left
                if ((voxelBase.enableFaceFlags & VoxelBase.Face.left) != 0)
                {
                    var pOffset = Vector3.Scale(voxelBase.importScale, offsetPosition);
                    var vOffset = cubeVertices.Count;
                    cubeVertices.Add(new Vector3(0, 0, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(0, 0, 0) + pOffset);
                    cubeVertices.Add(new Vector3(0, voxelBase.importScale.y, 0) + pOffset);
                    cubeVertices.Add(new Vector3(0, voxelBase.importScale.y, voxelBase.importScale.z) + pOffset);
                    cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 1); cubeTriangles.Add(vOffset + 0);
                    cubeTriangles.Add(vOffset + 3); cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 0);
                    for (int j = 0; j < 4; j++)
                    {
                        cubeNormals.Add(Vector3.left);
                    }
                }
                #endregion
                #region down
                if ((voxelBase.enableFaceFlags & VoxelBase.Face.down) != 0)
                {
                    var pOffset = Vector3.Scale(voxelBase.importScale, offsetPosition);
                    var vOffset = cubeVertices.Count;
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, 0, 0) + pOffset);
                    cubeVertices.Add(new Vector3(0, 0, 0) + pOffset);
                    cubeVertices.Add(new Vector3(0, 0, voxelBase.importScale.z) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, 0, voxelBase.importScale.z) + pOffset);
                    cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 1); cubeTriangles.Add(vOffset + 0);
                    cubeTriangles.Add(vOffset + 3); cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 0);
                    for (int j = 0; j < 4; j++)
                    {
                        cubeNormals.Add(Vector3.down);
                    }
                }
                #endregion
                #region back
                if ((voxelBase.enableFaceFlags & VoxelBase.Face.back) != 0)
                {
                    var pOffset = Vector3.Scale(voxelBase.importScale, offsetPosition);
                    var vOffset = cubeVertices.Count;
                    cubeVertices.Add(new Vector3(0, 0, 0) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, 0, 0) + pOffset);
                    cubeVertices.Add(new Vector3(voxelBase.importScale.x, voxelBase.importScale.y, 0) + pOffset);
                    cubeVertices.Add(new Vector3(0, voxelBase.importScale.y, 0) + pOffset);
                    cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 1); cubeTriangles.Add(vOffset + 0);
                    cubeTriangles.Add(vOffset + 3); cubeTriangles.Add(vOffset + 2); cubeTriangles.Add(vOffset + 0);
                    for (int j = 0; j < 4; j++)
                    {
                        cubeNormals.Add(Vector3.back);
                    }
                }
                #endregion
            }
        }

        public abstract void SetExplosionCenter();

        public abstract void CopyMaterialProperties();
        public void SetMaterialProperties()
        {
            CopyMaterialProperties();
            SetExplosionCenter();
            explosionBase.SetExplosionRate(explosionBase.edit_explosionRate);
            explosionBase.SetExplosionRotate(explosionBase.edit_explosionRotate);
        }
    }
}
