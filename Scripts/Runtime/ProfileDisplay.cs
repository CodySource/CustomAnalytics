using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Threading;
#endif

namespace CodySource
{
    namespace CustomAnalytics
    {
#if UNITY_EDITOR
        [System.Serializable]
        public class ProfileDisplay
        {
            #region ENUMERATIONS

            public enum PROFILE_STATE { LOADING, IDLE, EXPORTING, ADDING, DELETING, SWAPING, COMPILING };

            #endregion

            #region PROPERTIES

            /// <summary>
            /// The name of the profile
            /// </summary>
            public string name => _name;
            [SerializeField] private string _name = "";

            /// <summary>
            /// A description for the profile
            /// </summary>
            public string description = "";

            /// <summary>
            /// The profile's contained datapoints
            /// </summary>
            public List<DataPoint> dataPoints = new List<DataPoint>();

            /// <summary>
            /// The cached list of available datapoint sources when selecting a source (so a new list doesn't have to be calculated every frame)
            /// </summary>
            public List<string> dataPointNames = new List<string>();

            /// <summary>
            /// The state of the profile
            /// </summary>
            public PROFILE_STATE state = PROFILE_STATE.LOADING;

            /// <summary>
            /// Any information that needs to be passed along when the state of the profile is different
            /// - DELETING: index 0
            /// - SWAPING: index 0, index 1
            /// </summary>
            public int[] stateInformation = new int[2] { -1, -1 };

            /// <summary>
            /// When calculating previews, this tracks which previews have already been calcuated
            /// </summary>
            public HashSet<int> recalculatedPreviews = new HashSet<int>();

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Constructor
            /// </summary>
            public ProfileDisplay(string pName) => _name = pName;

            /// <summary>
            /// Repopulate the available sources
            /// </summary>
            public void UpdateAvailableSources()
            {
                dataPointNames.Clear();
                dataPoints.ForEach(d => dataPointNames.Add(d.id));
            }

            /// <summary>
            /// Adds a new data point with the provided type
            /// </summary>
            public void AddDataPoint(string _pType)
            {
                state = PROFILE_STATE.IDLE;
                string newID = $"DataPoint{dataPoints.Count}";
                int _tries = 1;
                while (dataPoints.Exists(d => d.id == newID))
                {
                    newID = $"DataPoint{dataPoints.Count + _tries}";
                    _tries++;
                }
                string _typeString = _pType;
                DataTypes._DataType _type = (DataTypes._DataType)System.Activator.CreateInstance(
                    System.Type.GetType($"CodySource.CustomAnalytics.DataTypes.{_typeString}"));
                dataPoints.Add(new DataPoint()
                {
                    id = newID,
                    typeString = _pType,
                    type = _type
                });
                UpdateAvailableSources();
            }

            /// <summary>
            /// Deletes a datapoint from the profile
            /// </summary>
            public void DeleteDataPoint()
            {
                //  Reset state
                state = PROFILE_STATE.IDLE;

                //  Remove source references to the data point
                for (int d = 0; d < dataPoints.Count; d++)
                {
                    List<int> _removeAt = new List<int>();
                    for (int s = 0; s < dataPoints[d].sources.Count; s ++)
                    {
                        if (dataPoints[d].sources[s] == stateInformation[0]) _removeAt.Add(s);
                        else if (dataPoints[d].sources[s] > stateInformation[0]) dataPoints[d].sources[s] -= 1;
                    }
                    if (_removeAt.Count > 0) _removeAt.ForEach(r => dataPoints[d].sources.RemoveAt(r));
                }

                //  Remove the datapoint
                dataPoints.RemoveAt(stateInformation[0]);

                //  Refresh the sources list
                UpdateAvailableSources();
            }

            /// <summary>
            /// Swaps two datapoints by index
            /// </summary>
            public void SwapDataPoints()
            {
                //  Quick reference
                int p1 = stateInformation[0];
                int p2 = stateInformation[1];

                //  Perform Swap
                int _replace = (p1 < p2) ? p1 : p2;
                int _remove = (p1 > p2) ? p1 : p2;
                DataPoint _clone = new DataPoint(dataPoints[_remove]);
                dataPoints.Insert(_replace, _clone);
                dataPoints.RemoveAt(_remove + 1);

                //  Update references for all datapoints
                for (int i = 0; i < dataPoints.Count; i ++)
                {
                    for (int s = 0; s < dataPoints[i].sources.Count; s ++)
                    {
                        dataPoints[i].sources[s] = (dataPoints[i].sources[s] == p1) ? p2 :
                            (dataPoints[i].sources[s] == p2) ? p1 : dataPoints[i].sources[s];
                    }
                }

                //  Refresh the sources list & recalculate the calculation previews
                UpdateAvailableSources();

                //  Reset profile state
                state = PROFILE_STATE.IDLE;
                stateInformation[0] = -1;
                stateInformation[1] = -1;
            }

            /// <summary>
            /// Deserialize the display
            /// </summary>
            public void Deserialize()
            {
                dataPoints.ForEach(d => {
                    d.calculation = (d.calculationString != "<None>") ? (Calculations._Calculation)System.Activator.CreateInstance(
                        System.Type.GetType($"CodySource.CustomAnalytics.Calculations.{d.calculationString}")) : null;
                    d.type = (DataTypes._DataType)System.Activator.CreateInstance(
                        System.Type.GetType($"CodySource.CustomAnalytics.DataTypes.{d.typeString}"));
                });
            }

            /// <summary>
            /// Serialize the display
            /// </summary>
            public string Serialize()
            {
                return JsonUtility.ToJson(this);
            }

            /// <summary>
            /// Verifies if a potential source dependency loop exists
            /// </summary>
            public bool DoesSourceLoopExist(int pStart, int pTarget)
            {
                bool r = false;
                if (dataPoints[pTarget].sources.Count == 0) return false;
                else
                {
                    for (int i = 0; i < dataPoints[pTarget].sources.Count; i++)
                    {
                        if (dataPoints[pTarget].sources[i] == pStart) return true;
                        else
                        {
                            r = DoesSourceLoopExist(pStart, dataPoints[pTarget].sources[i]);
                            if (r) return true;
                        }
                    }
                }
                return r;
            }

            /// <summary>
            /// Export the profile
            /// </summary>
            public void GenerateCS()
            {
                //  Set the state
                state = PROFILE_STATE.COMPILING;
                _WriteProfileScript();
            }

            /// <summary>
            /// Creates the scriptable object
            /// </summary>
            public void GenerateObject()
            {
                //  Either create or update the profile asset
                List<string> _assets = new List<string> (UnityEditor.AssetDatabase.FindAssets($"t:{name}"));
                if (_assets.Count == 0)
                {
                    Texture2D texture = (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath(
                        "Packages/com.codysource.customanalytics/Editor Resources/Gizmos/AnalyticsIcon.png", typeof(Texture2D));
                    ScriptableObject _o = ScriptableObject.CreateInstance(name);
#if UNITY_2021_OR_NEWER
                    UnityEditor.EditorGUIUtility.SetIconForObject(_o, texture);
#endif
                    UnityEditor.AssetDatabase.CreateAsset(_o, $"Assets/{name}.asset");
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    UnityEditor.EditorGUIUtility.PingObject(_o);
                }
                else
                {
                    Texture2D texture = (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath(
                        "Packages/com.codysource.customanalytics/Editor Resources/Gizmos/AnalyticsIcon.png", typeof(Texture2D));
                    AnalyticsProfile _prof = (AnalyticsProfile)UnityEditor.AssetDatabase
                        .LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(_assets[0]), typeof(AnalyticsProfile));
#if UNITY_2021_OR_NEWER
                    UnityEditor.EditorGUIUtility.SetIconForObject(_prof, texture);
#endif
                    UnityEditor.EditorUtility.SetDirty(_prof);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    UnityEditor.EditorGUIUtility.PingObject(_prof);
                }

                //  Update the state
                state = PROFILE_STATE.IDLE;
            }

#endregion

            #region PRIVATE METHODS

            /// <summary>
            /// Writes the profile script
            /// </summary>
            private void _WriteProfileScript()
            {
                string _template =
                    "using System.Collections;\n" +
                    "using System.Collections.Generic;\n" +
                    "using UnityEngine;\n" +
                    "\n" +
                    "namespace CodySource\n" +
                    "{\n" +
                    "\tnamespace CustomAnalytics\n" +
                    "\t{\n" +
                    $"\t\tpublic class {name} : AnalyticsProfile\n" +
                    "\t\t{\n" +
                    "\t\t\t#region BOILERPLATE\n" +
                    "\n" +
                    "\t\t\t/// The serialized profile\n" +
                    $"\t\t\tpublic override string serializedProfileDisplay {{ get; }} = \"{Serialize().Replace("\"", "\\\"")}\";\n" +
                    "\n" +
                    "\t\t\t/// The runtime instance\n" +
                    "\t\t\tpublic static AnalyticsRuntime runtime { get { _runtime = _runtime ?? new AnalyticsRuntime(); return _runtime; } }\n" +
                    "\t\t\tprivate static AnalyticsRuntime _runtime;\n" +
                    "\n" +
                    "\t\t\t#endregion\n" +
                    "\n" +
                    "\t\t\t#region GENERATED\n" +
                    "\n" +
                    "\t\t\t/// Export runtime data\n" +
                    "\t\t\tpublic void Export()\n" +
                    "\t\t\t{\n" +
                    "\t\t\t\tAnalyticsRuntime.Export _export = new AnalyticsRuntime.Export()\n" +
                    "\t\t\t\t{\n" +
                    "{RuntimeExports}" +
                    "\t\t\t\t};\n" +
                    "\t\t\t\tExporter.Export(JsonUtility.ToJson(_export));\n" +
                    "\t\t\t}\n" +
                    "\n" +
                    "\t\t\t/// Runtime Accessors / Mutators\n" +
                    "\n" +
                    "{RuntimeAccessorsMutators}" +
                    "\t\t\t/// Runtime Definition\n" +
                    "\t\t\tpublic class AnalyticsRuntime\n" +
                    "\t\t\t{\n" +
                    "\t\t\t\tpublic List<DataPoint> dataPoints = new List<DataPoint>() {\n" +
                    "{RuntimeDefinition}" +
                    "\t\t\t\t};\n" +
                    "\n" +
                    "{RuntimeDataPointRefs}" +
                    "\n" +
                    "\t\t\t\t[System.Serializable]\n" +
                    "\t\t\t\tpublic struct Export\n" +
                    "\t\t\t\t{\n" +
                    "{RuntimeExportDefinition}" +
                    "\t\t\t\t}\n" +
                    "\t\t\t}\n" +
                    "\n" +
                    "\t\t\t#endregion\n" +
                    "\t\t}\n" +
                    "\t}\n" +
                    "}";

                //  Build output
                string _output = _template
                    .Replace("{RuntimeExports}", _GenerateRuntimeExports())
                    .Replace("{RuntimeAccessorsMutators}", _GenerateRuntimeAccessorsMutators())
                    .Replace("{RuntimeDataPointRefs}", _GenerateRuntimeDataPointRefs())
                    .Replace("{RuntimeDefinition}", _GenerateRuntimeDefinition())
                    .Replace("{RuntimeExportDefinition}", _GenerateRuntimeExportDefinition());

                //  Write file
                File.WriteAllText($"./Assets/{name}.cs", _output);

                //  Refresh the asset database
                UnityEditor.AssetDatabase.ImportAsset($"Assets/{name}.cs");
                UnityEditor.AssetDatabase.Refresh();

                //  Generate the exported datapoint callbacks
                string _GenerateRuntimeExports()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        if (dataPoints[i].export)
                        {
                            _out += $"\t\t\t\t\t{dataPoints[i].id} = {dataPoints[i].id}_Get(),\n";
                        }
                    }
                    return _out;
                }

                //  Generate the accessors and mutators for the datapoints
                string _GenerateRuntimeAccessorsMutators()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i++) _out += dataPoints[i].GetProfileGenerationString();
                    return _out;
                }

                //  Generate the definition for each datapoint
                string _GenerateRuntimeDefinition()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        string _sources = "";
                        dataPoints[i].sources.ForEach(s => _sources += $"{s}, ");
                        _out +=
                        $"\t\t\t\t\tnew DataPoint()\n" +
                        "\t\t\t\t\t{\n" +
                        $"\t\t\t\t\t\tid = \"{dataPoints[i].id}\",\n" +
                        $"\t\t\t\t\t\texport = {dataPoints[i].export.ToString().ToLower()},\n" +
                        $"\t\t\t\t\t\tnumber = {dataPoints[i].Number(ref dataPoints)}f,\n" +
                        $"\t\t\t\t\t\tflag = {dataPoints[i].Flag(ref dataPoints).ToString().ToLower()},\n" +
                        $"\t\t\t\t\t\ttext = \"{dataPoints[i].Text(ref dataPoints).Replace("\"","\\\"")}\",\n" +
#if UNITY_2021_OR_NEWER
                        $"\t\t\t\t\t\tsources = new List<int>(new int[] {{{((_sources == "")? ", " : _sources)[..^2]}}}),\n" +
#else
                        $"\t\t\t\t\t\tsources = new List<int>(new int[] {{{((_sources == "") ? "" : _sources.Substring(0, (_sources.Length - 2)))}}}),\n" +
#endif
                        $"\t\t\t\t\t\ttypeString = \"{dataPoints[i].typeString}\",\n" +
                        $"\t\t\t\t\t\ttype = new DataTypes.{dataPoints[i].typeString}(),\n" +
                        $"\t\t\t\t\t\tcalculationString = \"{dataPoints[i].calculationString}\",\n" +
                        ((dataPoints[i].calculationString != "<None>") ? $"\t\t\t\t\t\tcalculation = new Calculations.{dataPoints[i].calculationString}(),\n" : "") +
                        "\t\t\t\t\t},\n";
                    }
                    return _out;
                }

                //  Generates the datapoint reference properties
                string _GenerateRuntimeDataPointRefs()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        _out += $"\t\t\t\tpublic DataPoint {dataPoints[i].id} => dataPoints[{i}];\n";
                    }
                    return _out;
                }

                //  Generate the export definition for the runtime class
                string _GenerateRuntimeExportDefinition()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i ++)
                    {
                        if (dataPoints[i].export)
                        {
                            _out += $"\t\t\t\t\tpublic {dataPoints[i].type.GetTypeString()} {dataPoints[i].id};\n";
                        }
                    }
                    return _out;
                }
            }

            #endregion
        }
#else
        public class ProfileDisplay {}        
#endif
    }
}