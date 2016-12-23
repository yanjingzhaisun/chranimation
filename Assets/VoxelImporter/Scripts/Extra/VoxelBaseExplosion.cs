using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    public abstract class VoxelBaseExplosion : MonoBehaviour
    {
        protected VoxelBase voxelBase { get; private set; }
        protected Transform transformCache { get; private set; }
        protected Renderer rendererCache { get; private set; }

        protected int spidExplosionRate;
        protected int spidExplosionCenter;
        protected int spidExplosionRotate;

        public MaterialPropertyBlock materialPropertyBlock;

        public enum ExplosionMode
        {
            Stop,
            Play,
            Reverse,
        }
        protected ExplosionMode explosionMode;
        protected float explosionTime;
        protected float explosionLifeTime;
        protected Action explosionDone;

        [Serializable]
        public class MeshData
        {
            public Mesh mesh;
            public List<int> materialIndexes = new List<int>();
        }

#if UNITY_EDITOR
        public bool edit_autoGenerate = true;
        public long edit_fileRefreshLastTimeTicks;

        public bool edit_generateFoldout = true;
        public bool edit_settingsFoldout = true;
        public bool edit_previewFoldout = true;

        public float edit_birthRate = 1f;
        public bool edit_visibleOnly = true;
        public float edit_velocityMin = 10f;
        public float edit_velocityMax = 30f;
        [NonSerialized]
        public bool edit_explosionPlay;
        public float edit_explosionTime;
        public float edit_explosionLifeTime = 3f;
        public float edit_explosionRotate = 0.3f;
        public bool edit_autoSetExplosionCenter = true;
        public Vector3 edit_explosionCenter;

        public float edit_explosionRate
        {
            get
            {
                if (edit_explosionLifeTime > 0f)
                    return Mathf.Clamp(edit_explosionTime / edit_explosionLifeTime, 0f, 1f);
                else
                    return 0f;
            }
        }
#endif

        protected virtual void Awake()
        {
            voxelBase = GetComponent<VoxelBase>();
            transformCache = transform;
            rendererCache = GetComponent<Renderer>();

            spidExplosionRate = Shader.PropertyToID("_ExplosionRate");
            spidExplosionCenter = Shader.PropertyToID("_ExplosionCenter");
            spidExplosionRotate = Shader.PropertyToID("_ExplosionRotate");

            SetEnableExplosionObject(false);
        }
        protected virtual void OnDestroy()
        {
        }

        protected void Update()
        {
#if UNITY_EDITOR
            if(!UnityEditor.EditorApplication.isPlaying)
            {
                DrawMesh();
                return;
            }
#endif
            if (explosionMode == ExplosionMode.Play)
            {
                explosionTime += Time.deltaTime;
                if(explosionTime >= explosionLifeTime)
                {
                    explosionTime = explosionLifeTime;
                    SetEnableExplosionObject(false);
                    if (explosionDone != null)
                        explosionDone.Invoke();
                }
                SetExplosionRate(explosionTime / explosionLifeTime);
                SetEnableRenderer(false);

                if (explosionTime < explosionLifeTime)
                    DrawMesh();
            }
            else if (explosionMode == ExplosionMode.Reverse)
            {
                explosionTime -= Time.deltaTime;
                if (explosionTime < 0f)
                {
                    explosionTime = 0f;
                    SetEnableExplosionObject(false);
                    SetEnableRenderer(true);
                    if (explosionDone != null)
                        explosionDone.Invoke();
                }
                SetExplosionRate(explosionTime / explosionLifeTime);

                if (explosionTime > 0f)
                    DrawMesh();
            }
        }

        public void ExplosionPlay(float lifeTime, Action doneAction = null)
        {
            explosionMode = ExplosionMode.Play;
            explosionTime = 0;
            explosionLifeTime = lifeTime;
            explosionDone = doneAction;
            SetEnableExplosionObject(true);
        }

        public void ExplosionReversePlay(float lifeTime, Action doneAction = null)
        {
            explosionMode = ExplosionMode.Reverse;
            explosionTime = lifeTime;
            explosionLifeTime = lifeTime;
            explosionDone = doneAction;
            SetEnableExplosionObject(true);
        }

        public virtual void SetEnableExplosionObject(bool enable)
        {
            enabled = enable;
        }
        public virtual void SetEnableRenderer(bool enable)
        {
            if(rendererCache != null && rendererCache.enabled != enable)
                rendererCache.enabled = enable;
        }

        protected virtual void DrawMesh() { }

        public void SetExplosionRate(float rate)
        {
            if (materialPropertyBlock == null)
                materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.SetFloat(spidExplosionRate, rate);
        }
        public void SetExplosionCenter(Vector3 center)
        {
            if (materialPropertyBlock == null)
                materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.SetVector(spidExplosionCenter, center);
        }
        public void SetExplosionRotate(float rotate)
        {
            if (materialPropertyBlock == null)
                materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.SetFloat(spidExplosionRotate, rotate);
        }
    }
}

