#define PreviewObjectHide

using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace VoxelImporter
{
    public class VoxelEditorCommon
    {
        public VoxelBase objectTarget { get; private set; }
        public VoxelBaseCore objectCore { get; private set; }

        public Mesh arrow { get; private set; }
        public Material vertexColorMaterial { get; private set; }
        public Material vertexColorTransparentMaterial { get; private set; }
        public Material vertexColorTransparentZwriteMaterial { get; private set; }
        public Material unlitColorMaterial { get; private set; }
        public Material unlitTextureMaterial { get; private set; }
        public Texture2D blackTransparentTexture { get; private set; }
        public List<Rect> editorRectList { get; private set; }

        #region GUIStyle
        public GUIStyle guiStyleAlphaBox { get; private set; }
        public GUIStyle guiStyleToggleLeft { get; private set; }
        public GUIStyle guiStyleToggleRight { get; private set; }
        public GUIStyle guiStyleLabel { get; private set; }
        public GUIStyle guiStyleActiveButton { get; private set; }
        #endregion

        //Animation
        private float animationTime;
        public float AnimationPower { get { return 1f - ((Mathf.Cos((Time.realtimeSinceStartup - animationTime) * Mathf.PI * 2f) + 1f) / 2f); } }

        #region SelectionRect
        public struct SelectionRect
        {
            public void Reset()
            {
                Enable = false;
                start = IntVector2.zero;
                end = IntVector2.zero;
            }
            public void SetStart(IntVector2 add)
            {
                Enable = true;
                start = add;
                end = add;
            }
            public void SetEnd(IntVector2 add)
            {
                end = add;
            }
            public bool Enable { get; private set; }
            public int SizeX { get { return max.x - min.x + 1; } }
            public int SizeY { get { return max.y - min.y + 1; } }
            public IntVector2 min { get { return IntVector2.Min(start, end); } }
            public IntVector2 max { get { return IntVector2.Max(start, end); } }
            public Rect rect { get { return new Rect(min.x, min.y, max.x - min.x, max.y - min.y); } }

            public IntVector2 start;
            public IntVector2 end;
        }
        public SelectionRect selectionRect;
        #endregion

        //Tool
        public static Tool lastTool;

        //Fill
        public long fillVoxelDataLastTimeTicks;
        public DataTable3<List<IntVector3>> fillVoxelTable;
        public DataTable3<VoxelData.FaceAreaTable> fillVoxelFaceAreaTable;

        //Mesh
        public Mesh[] previewMesh;
        public Mesh[] cursorMesh;

        #region Preview & Icon
        public GameObject previewRoot;
        public RenderTexture previewTexture;
        public GameObject previewModel;
        public MeshRenderer previewModelRenderer;
        public GameObject previewCamera;
        public Camera previewCameraCamera;
        public Vector3 previewCameraLookAt;
        //
        public GameObject iconRoot;
        public RenderTexture iconTexture;
        public GameObject iconModel;
        public MeshRenderer iconModelRenderer;
        public GameObject iconCamera;
        public Camera iconCameraCamera;
        //
        public int previewFrameIndexOld = -2;
        public bool previewRepaint;
        public bool previewMouseDown;
        #endregion

        public VoxelEditorCommon(VoxelBase objectTarget, VoxelBaseCore objectCore)
        {
            this.objectTarget = objectTarget;
            this.objectCore = objectCore;

            arrow = CreateArrowMesh();

            vertexColorMaterial = new Material(Shader.Find("Voxel Importer/VertexColor"));
            vertexColorMaterial.hideFlags = HideFlags.DontSave;

            vertexColorTransparentMaterial = new Material(Shader.Find("Voxel Importer/VertexColor-Transparent"));
            vertexColorTransparentMaterial.hideFlags = HideFlags.DontSave;

            vertexColorTransparentZwriteMaterial = new Material(Shader.Find("Voxel Importer/VertexColor-Transparent-Zwrite"));
            vertexColorTransparentZwriteMaterial.hideFlags = HideFlags.DontSave;

            unlitColorMaterial = new Material(Shader.Find("Voxel Importer/Unlit/Color"));
            unlitColorMaterial.hideFlags = HideFlags.DontSave;

            unlitTextureMaterial = new Material(Shader.Find("Voxel Importer/Unlit/Transparent"));
            unlitTextureMaterial.hideFlags = HideFlags.DontSave;

            blackTransparentTexture = CreateColorTexture(new Color(0, 0, 0, 0.3f));

            editorRectList = new List<Rect>();

            animationTime = Time.realtimeSinceStartup;
        }

        public Mesh CreateArrowMesh()
        {
            var arrow = new Mesh();
            arrow.hideFlags = HideFlags.DontSave;
            Vector3[] lines = new Vector3[]
            {
                    new Vector3(0, 0, 0),

                    new Vector3(0, 0.1f, 0.1f),
                    new Vector3(0.09f, -0.05f, 0.1f),
                    new Vector3(-0.09f, -0.05f, 0.1f),

                    new Vector3(0, 0, 1),
            };
            int[] indices = new int[]
            {
                    0, 1,
                    0, 2,
                    0, 3,

                    1, 2,
                    2, 3,
                    3, 1,

                    4, 1,
                    4, 2,
                    4, 3,
            };
            arrow.vertices = lines;
            arrow.SetIndices(indices, MeshTopology.Lines, 0);
            arrow.RecalculateBounds();
            return arrow;
        }

        public Texture2D CreateColorTexture(Color color)
        {
            Texture2D tex = new Texture2D(4, 4);
            tex.hideFlags = HideFlags.DontSave;
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return tex;
        }

        public void ClearPreviewMesh()
        {
            if (previewMesh != null)
            {
                for (int i = 0; i < previewMesh.Length; i++)
                {
                    MonoBehaviour.DestroyImmediate(previewMesh[i]);
                }
                previewMesh = null;
            }
        }
        public void ClearCursorMesh()
        {
            if (cursorMesh != null)
            {
                for (int i = 0; i < cursorMesh.Length; i++)
                {
                    MonoBehaviour.DestroyImmediate(cursorMesh[i]);
                }
                cursorMesh = null;
            }
        }

        public bool CheckMousePositionEditorRects()
        {
            for (int i = 0; i < editorRectList.Count; i++)
            {
                if (editorRectList[i].Contains(Event.current.mousePosition))
                {
                    return false;
                }
            }
            return true;
        }

        public IntVector3? GetMousePositionVoxel()
        {
            IntVector3? result = null;

            if (objectTarget.voxelData == null || objectTarget.voxelData.voxels == null)
                return result;
            if (!CheckMousePositionEditorRects())
                return result;

            Ray localRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            {
                Matrix4x4 mat = objectTarget.transform.worldToLocalMatrix;
                localRay.direction = mat.MultiplyVector(localRay.direction);
                localRay.origin = mat.MultiplyPoint(localRay.origin);
            }

            {
                var boundsBase = objectCore.GetVoxelBounds(IntVector3.zero);
                float lengthMin = float.MaxValue;
                for (int i = 0; i < objectTarget.voxelData.voxels.Length; i++)
                {
                    if (objectTarget.voxelData.voxels[i].visible == 0)
                        continue;

                    Vector3 position = new Vector3(objectTarget.voxelData.voxels[i].x, objectTarget.voxelData.voxels[i].y, objectTarget.voxelData.voxels[i].z);
                    Vector3 offset = Vector3.Scale(position, objectTarget.importScale);
                    var bounds = boundsBase;
                    bounds.center += offset;
                    if (bounds.IntersectRay(localRay))
                    {
                        float length = (bounds.center - localRay.origin).magnitude;
                        if (!result.HasValue || length < lengthMin)
                        {
                            result = objectTarget.voxelData.voxels[i].position;
                            lengthMin = length;
                        }
                    }
                }
            }

            return result;
        }

        private void CheckFillVoxelTableCheck()
        {
            if (fillVoxelDataLastTimeTicks != objectTarget.voxelData.updateVoxelTableLastTimeTicks)
            {
                fillVoxelTable = null;
                fillVoxelFaceAreaTable = null;
                fillVoxelDataLastTimeTicks = objectTarget.voxelData.updateVoxelTableLastTimeTicks;
            }
        }
        public List<IntVector3> GetFillVoxel(IntVector3 pos)
        {
            if (objectTarget.voxelData == null) return null;
            if (objectTarget.voxelData.VoxelTableContains(pos) < 0) return null;

            CheckFillVoxelTableCheck();
            if (fillVoxelTable == null)
            {
                fillVoxelTable = new DataTable3<List<IntVector3>>(objectTarget.voxelData.voxelSize.x, objectTarget.voxelData.voxelSize.y, objectTarget.voxelData.voxelSize.z);
            }
            if (!fillVoxelTable.Contains(pos))
            {
                List<IntVector3> searchList = new List<IntVector3>();
                for (int i = 0; i < objectTarget.voxelData.voxels.Length; i++)
                {
                    int posPalette = 0;
                    var doneTable = new FlagTable3(objectTarget.voxelData.voxelSize.x, objectTarget.voxelData.voxelSize.y, objectTarget.voxelData.voxelSize.z);
                    {
                        var p = objectTarget.voxelData.voxels[i].position;
                        if (fillVoxelTable.Get(p) != null) continue;
                        var index = objectTarget.voxelData.VoxelTableContains(p);
                        posPalette = objectTarget.voxelData.voxels[index].palette;
                        searchList.Clear();
                        searchList.Add(p);
                        doneTable.Set(p, true);
                    }
                    var result = new List<IntVector3>();
                    for (int j = 0; j < searchList.Count; j++)
                    {
                        var p = searchList[j];
                        var index = objectTarget.voxelData.VoxelTableContains(p);
                        if (index < 0) continue;
                        if (objectTarget.voxelData.voxels[index].palette == posPalette)
                        {
                            result.Add(p);
                            for (int x = p.x - 1; x <= p.x + 1; x++)
                            {
                                for (int y = p.y - 1; y <= p.y + 1; y++)
                                {
                                    for (int z = p.z - 1; z <= p.z + 1; z++)
                                    {
                                        if (x >= 0 && y >= 0 && z >= 0 &&
                                            x < objectTarget.voxelData.voxelSize.x && y < objectTarget.voxelData.voxelSize.y && z < objectTarget.voxelData.voxelSize.z &&
                                            !doneTable.Get(x, y, z))
                                        {
                                            doneTable.Set(x, y, z, true);
                                            var indexTmp = objectTarget.voxelData.VoxelTableContains(x, y, z);
                                            if (indexTmp >= 0 && objectTarget.voxelData.voxels[indexTmp].palette == posPalette)
                                                searchList.Add(new IntVector3(x, y, z));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    for (int j = 0; j < result.Count; j++)
                    {
                        fillVoxelTable.Set(result[j], result);
                    }
                }
            }
            var fillVoxel = fillVoxelTable.Get(pos);

            return fillVoxel;
        }
        public VoxelData.FaceAreaTable GetFillVoxelFaceAreaTable(IntVector3 pos)
        {
            if (objectTarget.voxelData == null) return null;
            if (objectTarget.voxelData.VoxelTableContains(pos) < 0) return null;

            CheckFillVoxelTableCheck();
            if (fillVoxelFaceAreaTable == null)
            {
                fillVoxelFaceAreaTable = new DataTable3<VoxelData.FaceAreaTable>(objectTarget.voxelData.voxelSize.x, objectTarget.voxelData.voxelSize.y, objectTarget.voxelData.voxelSize.z);
            }
            if (!fillVoxelFaceAreaTable.Contains(pos))
            {
                var list = GetFillVoxel(pos);
                if (list == null) return null;
                var voxels = new List<VoxelData.Voxel>();
                for (int i = 0; i < list.Count; i++)
                {
                    var index = objectTarget.voxelData.VoxelTableContains(list[i]);
                    var voxel = objectTarget.voxelData.voxels[index];
                    voxel.palette = -1;
                    voxels.Add(voxel);
                }
                var faceAreaTable = objectCore.Edit_CreateMeshOnly_FaceArea(voxels, true);
                for (int i = 0; i < list.Count; i++)
                {
                    fillVoxelFaceAreaTable.Set(list[i], faceAreaTable);
                }
            }
            var fillVoxelFaceArea = fillVoxelFaceAreaTable.Get(pos);

            return fillVoxelFaceArea;
        }

        public struct VertexPower
        {
            public IntVector3 position;
            public float power;
        }
        public List<VertexPower> GetMousePositionVertex(float radius)
        {
            List<VertexPower> result = new List<VertexPower>();
            if (objectTarget.voxelData == null || objectTarget.voxelData.voxels == null)
                return result;
            if (!CheckMousePositionEditorRects())
                return result;

            var boundsList = new List<Bounds>();
            var vertexList = new List<VertexPower>();
            {
                Ray ray = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(new Vector3(Event.current.mousePosition.x, SceneView.currentDrawingSceneView.camera.pixelHeight - Event.current.mousePosition.y, SceneView.currentDrawingSceneView.camera.nearClipPlane));
                ray.origin = objectTarget.transform.worldToLocalMatrix.MultiplyPoint3x4(ray.origin);
                ray.direction = objectTarget.transform.worldToLocalMatrix.MultiplyVector(ray.direction);
                bool[,,] doneTable = new bool[objectTarget.voxelData.voxelSize.x + 1, objectTarget.voxelData.voxelSize.y + 1, objectTarget.voxelData.voxelSize.z + 1];
                {
                    Ray rayRadius = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(new Vector3(Event.current.mousePosition.x + radius, SceneView.currentDrawingSceneView.camera.pixelHeight - Event.current.mousePosition.y, SceneView.currentDrawingSceneView.camera.nearClipPlane));
                    rayRadius.origin = objectTarget.transform.worldToLocalMatrix.MultiplyPoint3x4(rayRadius.origin);
                    rayRadius.direction = objectTarget.transform.worldToLocalMatrix.MultiplyVector(rayRadius.direction);

                    Func<IntVector3, bool> AddVertex = (pos) =>
                    {
                        if (doneTable[pos.x, pos.y, pos.z])
                            return true;
                        doneTable[pos.x, pos.y, pos.z] = true;

                        var posL = objectCore.GetVoxelRatePosition(pos, Vector3.zero);
                        var posP = ray.origin + ray.direction * Vector3.Dot(posL - ray.origin, ray.direction);
                        var posR = rayRadius.origin + rayRadius.direction * (Vector3.Dot(posP - rayRadius.origin, rayRadius.direction));
                        var distanceL = (posL - posP).sqrMagnitude;
                        var distanceR = (posR - posP).sqrMagnitude;
                        if (distanceL < distanceR)
                        {
                            vertexList.Add(new VertexPower() { position = pos, power = distanceL / distanceR });
                            return true;
                        }
                        return false;
                    };
                    for (int i = 0; i < objectTarget.voxelData.voxels.Length; i++)
                    {
                        if (objectTarget.voxelData.voxels[i].visible == 0) continue;

                        var pos = objectTarget.voxelData.voxels[i].position;
                        bool enable = false;

                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.left | VoxelBase.Face.down | VoxelBase.Face.back)) != 0)
                            if (AddVertex(new IntVector3(pos.x, pos.y, pos.z))) enable = true;
                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.right | VoxelBase.Face.down | VoxelBase.Face.back)) != 0)
                            if (AddVertex(new IntVector3(pos.x + 1, pos.y, pos.z))) enable = true;
                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.left | VoxelBase.Face.up | VoxelBase.Face.back)) != 0)
                            if (AddVertex(new IntVector3(pos.x, pos.y + 1, pos.z))) enable = true;
                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.left | VoxelBase.Face.down | VoxelBase.Face.forward)) != 0)
                            if (AddVertex(new IntVector3(pos.x, pos.y, pos.z + 1))) enable = true;
                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.right | VoxelBase.Face.up | VoxelBase.Face.back)) != 0)
                            if (AddVertex(new IntVector3(pos.x + 1, pos.y + 1, pos.z))) enable = true;
                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.right | VoxelBase.Face.down | VoxelBase.Face.forward)) != 0)
                            if (AddVertex(new IntVector3(pos.x + 1, pos.y, pos.z + 1))) enable = true;
                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.left | VoxelBase.Face.up | VoxelBase.Face.forward)) != 0)
                            if (AddVertex(new IntVector3(pos.x, pos.y + 1, pos.z + 1))) enable = true;
                        if ((objectTarget.voxelData.voxels[i].visible & (VoxelBase.Face.right | VoxelBase.Face.up | VoxelBase.Face.forward)) != 0)
                            if (AddVertex(new IntVector3(pos.x + 1, pos.y + 1, pos.z + 1))) enable = true;
                        if (enable)
                        {
                            if ((ray.direction.x < 0f && ((objectTarget.voxelData.voxels[i].visible & VoxelBase.Face.right) != 0)) ||
                                (ray.direction.x > 0f && ((objectTarget.voxelData.voxels[i].visible & VoxelBase.Face.left) != 0)) ||
                                (ray.direction.y < 0f && ((objectTarget.voxelData.voxels[i].visible & VoxelBase.Face.up) != 0)) ||
                                (ray.direction.y > 0f && ((objectTarget.voxelData.voxels[i].visible & VoxelBase.Face.down) != 0)) ||
                                (ray.direction.z < 0f && ((objectTarget.voxelData.voxels[i].visible & VoxelBase.Face.forward) != 0)) ||
                                (ray.direction.z > 0f && ((objectTarget.voxelData.voxels[i].visible & VoxelBase.Face.back) != 0)))
                            {
                                boundsList.Add(objectCore.GetVoxelBounds(pos));
                            }
                        }
                    }
                }
            }

            {
                for (int i = 0; i < vertexList.Count; i++)
                {
                    var pos = objectCore.GetVoxelRatePosition(vertexList[i].position, Vector3.zero);
                    Ray ray = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(objectTarget.transform.localToWorldMatrix.MultiplyPoint3x4(pos)));
                    ray.origin = objectTarget.transform.worldToLocalMatrix.MultiplyPoint3x4(ray.origin);
                    ray.direction = objectTarget.transform.worldToLocalMatrix.MultiplyVector(ray.direction);
                    float length = (pos - ray.origin).magnitude - 0.1f;
                    bool enable = true;
                    for (int j = 0; j < boundsList.Count; j++)
                    {
                        float distance;
                        if (boundsList[j].IntersectRay(ray, out distance))
                        {
                            if (distance < length)
                            {
                                enable = false;
                                break;
                            }
                        }
                    }
                    if (enable)
                    {
                        result.Add(vertexList[i]);
                    }
                }
            }

            return result;
        }

        public List<IntVector3> GetSelectionRectVoxel()
        {
            List<IntVector3> result = new List<IntVector3>();
            if (selectionRect.Enable &&
                objectTarget.voxelData != null && objectTarget.voxelData.voxels != null)
            {
                var camera = SceneView.currentDrawingSceneView.camera;
                var localToWorldMatrix = objectTarget.transform.localToWorldMatrix;
                for (int i = 0; i < objectTarget.voxelData.voxels.Length; i++)
                {
                    var local = objectCore.GetVoxelCenterPosition(objectTarget.voxelData.voxels[i].position);
                    var world = localToWorldMatrix.MultiplyPoint(local);
                    var screen = camera.WorldToScreenPoint(world);
                    screen.y = camera.pixelHeight - screen.y;
                    if (selectionRect.rect.Contains(screen))
                    {
                        result.Add(objectTarget.voxelData.voxels[i].position);
                    }
                }
            }
            return result;
        }
        public List<IntVector3> GetSelectionRectVertex()
        {
            List<IntVector3> result = new List<IntVector3>();
            if (selectionRect.Enable &&
                objectTarget.voxelData != null && objectTarget.voxelData.vertexList != null)
            {
                var camera = SceneView.currentDrawingSceneView.camera;
                var localToWorldMatrix = objectTarget.transform.localToWorldMatrix;
                for (int i = 0; i < objectTarget.voxelData.vertexList.Count; i++)
                {
                    var local = objectCore.GetVoxelRatePosition(objectTarget.voxelData.vertexList[i], Vector3.zero);
                    var world = localToWorldMatrix.MultiplyPoint3x4(local);
                    var screen = camera.WorldToScreenPoint(world);
                    screen.y = camera.pixelHeight - screen.y;
                    if (selectionRect.rect.Contains(screen))
                    {
                        result.Add(objectTarget.voxelData.vertexList[i]);
                    }
                }
            }
            return result;
        }

        public void GUIStyleReady()
        {
            if (guiStyleAlphaBox == null)
                guiStyleAlphaBox = new GUIStyle(GUI.skin.box);
            guiStyleAlphaBox.normal.textColor = Color.white;
            guiStyleAlphaBox.fontStyle = FontStyle.Bold;
            guiStyleAlphaBox.alignment = TextAnchor.UpperCenter;
            guiStyleAlphaBox.normal.background = blackTransparentTexture;
            if (guiStyleToggleLeft == null)
                guiStyleToggleLeft = new GUIStyle(GUI.skin.toggle);
            guiStyleToggleLeft.normal.textColor = Color.white;
            guiStyleToggleLeft.onNormal.textColor = Color.white;
            guiStyleToggleLeft.hover.textColor = Color.white;
            guiStyleToggleLeft.onHover.textColor = Color.white;
            guiStyleToggleLeft.focused.textColor = Color.white;
            guiStyleToggleLeft.onFocused.textColor = Color.white;
            guiStyleToggleLeft.active.textColor = Color.white;
            guiStyleToggleLeft.onActive.textColor = Color.white;
            if (guiStyleToggleRight == null)
                guiStyleToggleRight = new GUIStyle(GUI.skin.toggle);
            guiStyleToggleRight.normal.textColor = Color.white;
            guiStyleToggleRight.onNormal.textColor = Color.white;
            guiStyleToggleRight.hover.textColor = Color.white;
            guiStyleToggleRight.onHover.textColor = Color.white;
            guiStyleToggleRight.focused.textColor = Color.white;
            guiStyleToggleRight.onFocused.textColor = Color.white;
            guiStyleToggleRight.active.textColor = Color.white;
            guiStyleToggleRight.onActive.textColor = Color.white;
            guiStyleToggleRight.padding.left = 2;
            guiStyleToggleRight.overflow.left = -149;
            if (guiStyleLabel == null)
                guiStyleLabel = new GUIStyle(GUI.skin.label);
            guiStyleLabel.normal.textColor = Color.white;
            if (guiStyleActiveButton == null)
                guiStyleActiveButton = new GUIStyle(GUI.skin.button);
            guiStyleActiveButton.normal = guiStyleActiveButton.active;
        }

        #region Preview & Icon
        public void InitializePreview()
        {
            const string PreviewRootObjectName = "Tmp#VoxelImporterPreview";
            {
                var objects = Resources.FindObjectsOfTypeAll<GameObject>();
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].name == PreviewRootObjectName)
                        previewRoot = objects[i];
                }
            }

            if (previewRoot == null)
            {
#if PreviewObjectHide
                previewRoot = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(PreviewRootObjectName, HideFlags.HideAndDontSave);
#else
                previewRoot = new GameObject(PreviewRootObjectName);
                previewRoot.hideFlags = HideFlags.DontSave;
#endif
            }
            previewRoot.transform.position = new Vector3(0, 10000, 0);
            previewRoot.SetActive(false);

            int blankLayer;
            for (blankLayer = 31; blankLayer > 0; blankLayer--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(blankLayer)))
                    break;
            }
            if (blankLayer < 0)
                blankLayer = 31;
            previewRoot.layer = blankLayer;

            previewTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            previewTexture.hideFlags = HideFlags.DontSave;
            previewTexture.Create();

            CreatePreviewObject();
        }
        public void InitializeIcon()
        {
            const string IconRootObjectName = "Tmp#VoxelImporterIcon";

            {
                var objects = Resources.FindObjectsOfTypeAll<GameObject>();
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].name == IconRootObjectName)
                        iconRoot = objects[i];
                }
            }

            if (iconRoot == null)
            {
#if PreviewObjectHide
                iconRoot = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(IconRootObjectName, HideFlags.HideAndDontSave);
#else
                iconRoot = new GameObject(IconRootObjectName);
                iconRoot.hideFlags = HideFlags.DontSave;
#endif
            }
            iconRoot.transform.position = new Vector3(0, -10000, 0);

            int blankLayer;
            for (blankLayer = 31; blankLayer > 0; blankLayer--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(blankLayer)))
                    break;
            }
            if (blankLayer < 0)
                blankLayer = 31;
            iconRoot.layer = blankLayer;
            iconRoot.SetActive(false);

            iconTexture = new RenderTexture(128, 128, 16, RenderTextureFormat.ARGB32);
            iconTexture.hideFlags = HideFlags.DontSave;
            iconTexture.Create();

            CreateIconObject();
        }

        public bool CreatePreviewObject(Mesh mesh = null, Material[] materials = null)
        {
            if (previewRoot == null) return false;

            {
                const string PreviewModelName = "PreviewModel";

                var previewModelTransform = previewRoot.transform.FindChild(PreviewModelName);
                if (previewModelTransform != null)
                    previewModel = previewModelTransform.gameObject;
                if (previewModel == null)
                {
#if PreviewObjectHide
                    previewModel = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(PreviewModelName, HideFlags.HideAndDontSave);
#else
                    previewModel = new GameObject(PreviewModelName);
                    previewModel.hideFlags = HideFlags.DontSave;
#endif
                }
                previewModel.transform.SetParent(previewRoot.transform);
                previewModel.transform.localPosition = Vector3.zero;
                previewModel.layer = previewRoot.layer;
                MeshFilter meshFilter = previewModel.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = previewModel.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
                previewModelRenderer = previewModel.GetComponent<MeshRenderer>();
                if (previewModelRenderer == null)
                    previewModelRenderer = previewModel.AddComponent<MeshRenderer>();
                if (materials != null)
                    previewModelRenderer.sharedMaterials = materials;
            }
            {
                const string PreviewCameraName = "PreviewCamera";
                var previewCameraTransform = previewRoot.transform.FindChild(PreviewCameraName);
                if (previewCameraTransform != null)
                    previewCamera = previewCameraTransform.gameObject;
                if (previewCamera == null)
                {
#if PreviewObjectHide
                    previewCamera = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(PreviewCameraName, HideFlags.HideAndDontSave);
#else
                    previewCamera = new GameObject(PreviewCameraName);
                    previewCamera.hideFlags = HideFlags.DontSave;
#endif
                    previewCameraCamera = previewCamera.AddComponent<Camera>();
                }
                else
                {
                    previewCameraCamera = previewCamera.GetComponent<Camera>();
                }

                previewCamera.transform.SetParent(previewRoot.transform);
                previewCamera.layer = previewRoot.layer;
                if (mesh != null)
                {
                    var rot = Quaternion.AngleAxis(180f, Vector3.up);
                    previewCamera.transform.localRotation = rot;
                    previewCamera.transform.localPosition = mesh.bounds.center + Vector3.forward * mesh.bounds.size.z * 5f;
                    previewCameraLookAt = mesh.bounds.center;
                }
                else
                {
                    previewCamera.transform.localPosition = Vector3.zero;
                    previewCamera.transform.localRotation = Quaternion.identity;
                    previewCameraLookAt = Vector3.zero;
                }
                previewCameraCamera.orthographic = true;
                if (mesh != null)
                {
                    previewCameraCamera.orthographicSize = Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z)) * 0.6f;
                    previewCameraCamera.farClipPlane = Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z)) * 5f;
                }
                previewCameraCamera.clearFlags = CameraClearFlags.Color;
                previewCameraCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                previewCameraCamera.cullingMask = 1 << previewRoot.layer;
                previewCameraCamera.targetTexture = previewTexture;
            }
            return true;
        }
        public void CreateIconObject(Mesh mesh = null, Material[] materials = null)
        {
            {
                const string IconModelName = "IconModel";

                var iconModelTransform = iconRoot.transform.FindChild(IconModelName);
                if (iconModelTransform != null)
                    iconModel = iconModelTransform.gameObject;
                if (iconModel == null)
                {
#if PreviewObjectHide
                    iconModel = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(IconModelName, HideFlags.HideAndDontSave);
#else
                    iconModel = new GameObject(IconModelName);
                    iconModel.hideFlags = HideFlags.DontSave;
#endif
                }
                iconModel.transform.SetParent(iconRoot.transform);
                iconModel.transform.localPosition = new Vector3(-0.5f, -0.5f, -0.5f);
                iconModel.layer = iconRoot.layer;
                MeshFilter meshFilter = iconModel.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = iconModel.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
                iconModelRenderer = iconModel.GetComponent<MeshRenderer>();
                if (iconModelRenderer == null)
                    iconModelRenderer = iconModel.AddComponent<MeshRenderer>();
                if (materials != null)
                    iconModelRenderer.sharedMaterials = materials;
            }
            {
                const string IconCameraName = "IconCamera";

                var iconCameraTransform = iconRoot.transform.FindChild(IconCameraName);
                if (iconCameraTransform != null)
                    iconCamera = iconCameraTransform.gameObject;
                if (iconCamera == null)
                {
#if PreviewObjectHide
                    iconCamera = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(IconCameraName, HideFlags.HideAndDontSave);
#else
                    iconCamera = new GameObject(IconCameraName);
                    iconCamera.hideFlags = HideFlags.DontSave;
#endif
                    iconCameraCamera = iconCamera.AddComponent<Camera>();
                }
                else
                {
                    iconCameraCamera = iconCamera.GetComponent<Camera>();
                }
                iconCamera.transform.SetParent(iconRoot.transform);
                iconCamera.layer = iconRoot.layer;
                if (mesh != null)
                {
                    var rot = Quaternion.AngleAxis(180f, Vector3.up);
                    iconCamera.transform.localRotation = rot;
                    iconCamera.transform.localPosition = mesh.bounds.center + iconCamera.transform.worldToLocalMatrix.MultiplyVector(iconCamera.transform.forward) * mesh.bounds.size.z * 5f;
                }
                else
                {
                    iconCamera.transform.localPosition = Vector3.zero;
                    iconCamera.transform.localRotation = Quaternion.identity;
                }
                iconCameraCamera.orthographic = true;
                if (mesh != null)
                {
                    iconCameraCamera.orthographicSize = Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z)) * 0.6f;
                    iconCameraCamera.farClipPlane = Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z)) * 5f;
                }
                iconCameraCamera.clearFlags = CameraClearFlags.Color;
                iconCameraCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                iconCameraCamera.cullingMask = 1 << iconRoot.layer;
                iconCameraCamera.targetTexture = iconTexture;

            }
        }

        public bool IsReadyPreview()
        {
            return previewRoot != null && previewModelRenderer != null && previewModelRenderer.sharedMaterials != null;
        }
        public bool IsReadyIcon()
        {
            return iconRoot != null && iconModelRenderer != null && iconModelRenderer.sharedMaterials != null;
        }

        public void PreviewObjectRender()
        {
            if (previewRoot == null) return;
            previewRoot.SetActive(true);
            previewCameraCamera.Render();
            previewRoot.SetActive(false);
        }
        public Texture2D IconObjectRender()
        {
            if (iconRoot == null) return null;

            Texture2D tex = null;

            iconRoot.SetActive(true);
            iconCameraCamera.Render();
            {
                RenderTexture save = RenderTexture.active;
                RenderTexture.active = iconTexture;
                tex = new Texture2D(iconTexture.width, iconTexture.height, TextureFormat.ARGB32, iconTexture.useMipMap);
                tex.hideFlags = HideFlags.DontSave;
                tex.ReadPixels(new Rect(0, 0, iconTexture.width, iconTexture.height), 0, 0);
                tex.Apply();
                RenderTexture.active = save;
            }
            iconRoot.SetActive(false);

            return tex;
        }

        #endregion
    }
}