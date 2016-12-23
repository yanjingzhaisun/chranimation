using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    public abstract class EditorCommon : Editor
    {
        protected static void SaveInsideAssetsFolderDisplayDialog()
        {
            EditorUtility.DisplayDialog("Need to save in the Assets folder", "You need to save the file inside of the project's assets floder", "ok");
        }

        protected string GetPrefabHelpStrings(List<string> helpList)
        {
            return GetHelpStrings("Prefab", helpList);
        }
        protected string GetHelpStrings(string title, List<string> helpList)
        {
            if (helpList.Count > 0)
            {
                string text = "";
                if (helpList.Count >= 3)
                {
                    int i = 0;
                    var enu = helpList.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        if (i == helpList.Count - 1)
                            text += ", and ";
                        else if (i != 0)
                            text += ", ";
                        text += enu.Current;
                        i++;
                    }
                }
                else if (helpList.Count == 2)
                {
                    var enu = helpList.GetEnumerator();
                    enu.MoveNext();
                    text += enu.Current;
                    text += " and ";
                    enu.MoveNext();
                    text += enu.Current;
                }
                else if (helpList.Count == 1)
                {
                    var enu = helpList.GetEnumerator();
                    enu.MoveNext();
                    text += enu.Current;
                }
                return string.Format("{0} is need save file.\nPlease save {1}.", title, text);
            }
            return null;
        }
    }
}
