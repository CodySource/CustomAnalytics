using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Text.RegularExpressions;
#endif

namespace CodySource
{
    namespace CustomAnalytics
    {
        [System.Serializable]
        public abstract class AnalyticsProfile : ScriptableObject
#if UNITY_EDITOR
            , IPreprocessBuildWithReport
#endif
        {
            public abstract string serializedProfileDisplay { get; }

#if UNITY_EDITOR
            public int callbackOrder => 1;

            /// <summary>
            /// Write a new export file on build
            /// </summary>
            public void OnPreprocessBuild(BuildReport report)
            {
                string name = GetType().ToString().Split('.')[2];
                if (!File.Exists($"./Assets/CustomAnalytics/{name}.php")) return;

                string _output = File.ReadAllText($"./Assets/CustomAnalytics/{name}.php");
                _output = Regex.Replace(_output, "const tableName = '.+';", $"const tableName = '{Application.productName.Replace(" ", "_")}_{Application.version.Replace(".", "_").Replace("[", "").Replace("]", "").Split('-')[0]}_Analytics';");

                //  Write file
                Directory.CreateDirectory("./Assets/CustomAnalytics/");
                File.WriteAllText($"./Assets/CustomAnalytics/{name}.php", _output);
            }

#endif
        }
    }
}