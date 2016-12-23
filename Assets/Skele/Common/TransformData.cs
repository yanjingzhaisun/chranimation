using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// used to record the transform info,
    /// default take local info,
    /// the -W postfix functions take the world info.
    /// 
    /// NOTE: scale are always taken by local
    /// </summary>
    [Serializable]
	public class XformData
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;

        public static XformData Create(Transform srcTr)
        {
            XformData trData = new XformData();
            trData.CopyFrom(srcTr);
            return trData;
        }

        public static XformData CreateW(Transform srcTr)
        {
            XformData trData = new XformData();
            trData.CopyFromW(srcTr);
            return trData;
        }

        public void CopyFrom(Transform tr)
        {
            pos = tr.localPosition;
            rot = tr.localRotation;
            scale = tr.localScale;
        }

        public void CopyFromW(Transform tr)
        {
            pos = tr.position;
            rot = tr.rotation;
            scale = tr.localScale;
        }

        public void CopyFrom(XformData data)
        {
            pos = data.pos;
            rot = data.rot;
            scale = data.scale;
        }

        public void Apply(Transform tr)
        {
            tr.localPosition = pos;
            tr.localRotation = rot;
            tr.localScale = scale;
        }

        public void ApplyW(Transform tr)
        {
            tr.position = pos;
            tr.rotation = rot;
            tr.localScale = scale;
        }

        public void Clear()
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            scale = Vector3.zero;
        }
    }
}
