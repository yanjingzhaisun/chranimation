using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Skele
{
    public class HotFix
    {
        public static void FixRotation(Transform tr)
        {
#if UNITY_5_4_OR_NEWER
            Vector3 ea = (Vector3)RCall.CallMtd("UnityEngine.Transform", "GetLocalEulerAngles", tr, 4);
            RCall.CallMtd("UnityEngine.Transform", "SetLocalEulerHint", tr, ea);
            //tr.SetLocalEulerHint(tr.GetLocalEulerAngles(_rotateOrder));
            if (tr.parent != null)
            {
                RCall.CallMtd("UnityEngine.Transform", "SendTransformChangedScale", tr);
                //tr.SendTransformChangedScale();
            }
#endif
        }
        public static void FixRotation(IList<Transform> trs)
        {
#if UNITY_5_4_OR_NEWER
            foreach(Transform tr in trs)
            {
                FixRotation(tr);
            }
#endif
        }

    }
}
