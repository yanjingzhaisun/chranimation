using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExtMethods;
using System.Collections;

namespace MH.Skele.CAT
{
    /// <summary>
    /// given a clip, bake the clip frame by frame
    /// </summary>
    public class AnimationBaker : EditorWindow
    {
        // configurable data
        private List<Transform> m_Roots = new List<Transform>(); //when click "Collect", will collect all transform under each root;

        // data
        private List<Transform> m_Trs = new List<Transform>(); //the target transforms, will undo.record them for each frame;
        private Transform[] m_TrsArr;
        private bool m_baking = false;
        private object m_uaw = null; //the unity animation window

        [MenuItem("Window/Skele/AnimationBaker")]
        public static void OpenWindow()
        {
            var wnd = EditorWindow.GetWindow(typeof(AnimationBaker));
            EUtil.SetEditorWindowTitle(wnd, "AnimationBaker");
        }

        void OnGUI()
        {
            object uawstate;
            AnimationClip clip;
            bool isReady = _IsAnimationWindowOpenAndWithClip(out uawstate, out clip);

            if( !isReady )
            {
                EditorGUILayout.LabelField("Need to open AnimationWindow with a clip first ... ");
            }
            else
            {
                EditorGUILayout.LabelField("Set the Roots:");
                for (int i = 0; i < m_Roots.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginChangeCheck();
                        var root = (Transform)EditorGUILayout.ObjectField(m_Roots[i], typeof(Transform), true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_Roots[i] = root;

                        }
                        if (EUtil.Button("-", "delete entry", Color.red, GUILayout.Width(20f)))
                        {
                            m_Roots.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(40f);
                    if (GUILayout.Button("Add Root Entry"))
                    {
                        m_Roots.Add(null);
                    }
                    GUILayout.Space(40f);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5f);

                if (GUILayout.Button(m_baking ? "Baking" : "Not Baking", EditorStyles.toolbarButton))
                {
                    m_baking = !m_baking;

                    if (m_baking)
                    {
                        m_Trs.Clear();
                        foreach (var oneRoot in m_Roots)
                        {
                            for (var ie = oneRoot.GetRecurEnumerator(); ie.MoveNext();)
                            {
                                m_Trs.Add(ie.Current);
                            }
                        }

                        m_TrsArr = m_Trs.ToArray();
                        m_uaw = EUtil.GetUnityAnimationWindow();
                        if (null == m_uaw)
                        {
                            m_baking = false;
                        }

                    }
                }

                if (!m_baking)
                {
                    GUILayout.Space(5f);

                    if (GUILayout.Button(new GUIContent("Compress Angles", "Only use this method on a clips full of keyframes"), EditorStyles.toolbarButton))
                    {
                        _NormalizeRotationsInClip(uawstate, clip);
                    }
                }
            }           

        }

        private static bool _IsAnimationWindowOpenAndWithClip(out object uawstate, out AnimationClip clip)
        {
            uawstate = null;
            clip = null;

            EditorWindow uaw = (EditorWindow)EUtil.GetUnityAnimationWindow();
            if (uaw == null)
                return false;
            uawstate = EUtil.GetUnityAnimationWindowState(uaw);
            if (uawstate == null)
                return false;
            clip = RCall.GetField("UnityEditorInternal.AnimationWindowState",
                    "m_ActiveAnimationClip", uawstate) as AnimationClip;
            if (clip == null)
                return false;

            return true;
        }

        void Update()
        {
            if (!m_baking || m_TrsArr == null)
                return;

            Undo.RecordObjects(m_TrsArr, "baking");
        }

        private void _NormalizeRotationsInClip(object uawstate, AnimationClip clip)
        {
            //float interval = 1 / clip.frameRate;
            IList allCurves = (IList)RCall.GetProp("UnityEditorInternal.AnimationWindowState", "allCurves", uawstate);
            foreach (var oneUAWCurve in allCurves)
            {
                EditorCurveBinding oneBinding = (EditorCurveBinding)RCall.GetProp("UnityEditorInternal.AnimationWindowCurve", "binding", oneUAWCurve);
                string prop = oneBinding.propertyName;
                string[] parts = prop.Split('.');
                if (parts.Length != 2)
                    continue;

                string main = parts[0];
                //string acc = parts[1];
                if (main == "localEulerAnglesRaw" || main == "localEulerAnglesBaked")
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, oneBinding);
                    var keys = curve.keys;

                    ////////////////////
                    // 1. normalize the first key to [-PI, PI], get the delta, and apply delta on all other keys in this curve
                    // 2. ensure the abs distance between each keyframe is less than 360 deg
                    ////////////////////

                    // 1
                    if (keys.Length == 0)
                        continue;

                    {
                        float oldV = keys[0].value;
                        float newV = Misc.NormalizeAnglePI(oldV);
                        float delta = newV - oldV;

                        // apply delta to all keyframes
                        for (int i = 0; i < keys.Length; ++i)
                        {
                            var k = keys[i];
                            k.value += delta;
                            k.inTangent = k.outTangent = 0;
                            keys[i] = k;
                        }
                    }

                    // 2
                    for (int i=0; i<keys.Length-1; ++i)
                    {
                        var k0 = keys[i];
                        var k1 = keys[i + 1];

                        float delta = (k1.value - k0.value) % 360f;
                        k1.value = k0.value + delta;

                        keys[i+1] = k1;
                    }

                    curve.keys = keys;
                    AnimationUtility.SetEditorCurve(clip, oneBinding, curve);
                }
            }
        }
    }
}
