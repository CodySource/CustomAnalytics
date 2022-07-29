using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Threading;

namespace CodySource
{
    namespace CustomAnalytics
    {
        [System.Serializable]
        public class ProfileDisplay
        {
            #region PROPERTIES

            public string name => _name;
            [SerializeField] private string _name = "";
            public List<DataPoint> dataPoints = new List<DataPoint>();

            //  Window Information
            public List<string> availableSources = new List<string>();
            public List<string> calculationNames = new List<string>();
            public List<SourceSelections> datapointSourceSelections = new List<SourceSelections>();
            public bool isExporting = false;
            public int deletedIndex = -1;
            public int[] swapIndex = new int[2] { -1, -1 };

            /// <summary>
            /// The datapoints that have already been recalculated
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
                availableSources.Clear();
                dataPoints.ForEach(d => availableSources.Add(d.id));
            }

            /// <summary>
            /// Recalculates all the data point value previews
            /// </summary>
            public void UpdateCalculationPreviews()
            {
                if (recalculatedPreviews == null) recalculatedPreviews = new HashSet<int>();
                recalculatedPreviews.Clear();
                for (int i = 0; i < dataPoints.Count; i++) RecaculatePreviewValues(i);

                void RecaculatePreviewValues(int pTarget)
                {
                    if (recalculatedPreviews.Contains(pTarget)) return;
                    recalculatedPreviews.Add(pTarget);
                    if (calculationNames[pTarget] == "<None>") return;
                    else
                    {
                        for (int i = 0; i < datapointSourceSelections[pTarget].sources.Count; i++)
                        {
                            RecaculatePreviewValues(datapointSourceSelections[pTarget].sources[i]);
                        }
                        //  Perform Calculation
                        System.Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                        foreach (System.Type type in types)
                        {
                            if (type.IsSubclassOf(typeof(Calculations._Calculation)) && type.Name == calculationNames[pTarget])
                            {
                                Calculations._Calculation _cal = (Calculations._Calculation)System.Activator.CreateInstance(type);
                                DataPoint[] _params = new DataPoint[datapointSourceSelections[pTarget].sources.Count];
                                for (int i = 0; i < _params.Length; i++)
                                {
                                    _params[i] = dataPoints[datapointSourceSelections[pTarget].sources[i]];
                                }
                                dataPoints[pTarget].SetValue(_cal.CalculateInt(_params));
                                dataPoints[pTarget].SetValue(_cal.CalculateFloat(_params));
                                dataPoints[pTarget].SetValue(_cal.CalculateBool(_params));
                                dataPoints[pTarget].SetValue(_cal.CalculateString(_params));
                                break;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Deletes a datapoint from the profile
            /// </summary>
            public void DeleteDataPoint()
            {
                //  Remove source references to the data point
                for (int d = 0; d < dataPoints.Count; d++)
                {
                    for (int s = 0; s < datapointSourceSelections[d].sources.Count; s++)
                    {
                        int v = datapointSourceSelections[d].sources[s];
                        datapointSourceSelections[d].sources[s] = (v > deletedIndex) ? v - 1 : v;
                    }
                }
                dataPoints.RemoveAt(deletedIndex);
                datapointSourceSelections.RemoveAt(deletedIndex);
                calculationNames.RemoveAt(deletedIndex);
                UpdateAvailableSources();
                UpdateCalculationPreviews();
                deletedIndex = -1;
            }

            /// <summary>
            /// Swaps two datapoints by index
            /// </summary>
            public void SwapDataPoints()
            {
                //  Swap the items
                int p1 = swapIndex[0];
                int p2 = swapIndex[1];
                //  Update all source references
                for (int d = 0; d < dataPoints.Count; d++)
                {
                    for (int s = 0; s < datapointSourceSelections[d].sources.Count; s++)
                    {
                        int v = datapointSourceSelections[d].sources[s];
                        datapointSourceSelections[d].sources[s] = (v == p1) ? -1 : ((v == p2) ? p1 : v);
                        v = datapointSourceSelections[d].sources[s];
                        datapointSourceSelections[d].sources[s] = (v == -1) ? p2 : v;
                    }
                }
                //  Perform Swap
                int _replace = (p1 < p2) ? p1 : p2;
                int _remove = (p1 > p2) ? p1 : p2;
                dataPoints.Insert(_replace, new DataPoint(dataPoints[_remove]));
                datapointSourceSelections.Insert(_replace, new SourceSelections() { sources = new List<int>(datapointSourceSelections[_remove].sources) });
                calculationNames.Insert(_replace, calculationNames[_remove]);
                dataPoints.RemoveAt(_remove + 1);
                datapointSourceSelections.RemoveAt(_remove + 1);
                calculationNames.RemoveAt(_remove + 1);
                swapIndex = new int[] { -1, -1};
                UpdateAvailableSources();
                UpdateCalculationPreviews();
            }

            /// <summary>
            /// Serialize the display
            /// </summary>
            public string Serialize()
            {
                //serializedDataPoints = JsonUtility.ToJson(dataPoints);
                return JsonUtility.ToJson(this);
            }

            /// <summary>
            /// Verifies if a potential source dependency loop exists
            /// </summary>
            public bool DoesSourceLoopExist(int pStartIndex, int pTarget)
            {
                bool r = false;
                if (calculationNames[pTarget] == "<None>") return false;
                else
                {
                    for (int i = 0; i < datapointSourceSelections[pTarget].sources.Count; i ++)
                    {
                        int _source = datapointSourceSelections[pTarget].sources[i];
                        if (_source == pStartIndex) return true;
                        else
                        {
                            r = DoesSourceLoopExist(pStartIndex, _source);
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
                isExporting = false;
                _WriteProfileScript();
            }

            /// <summary>
            /// Creates the scriptable object
            /// </summary>
            public void GenerateObject()
            {
                UnityEditor.EditorPrefs.DeleteKey("NewAnalyticsProfile");
                List<string> _assets = new List<string> (UnityEditor.AssetDatabase.FindAssets($"t:{name}"));
                if (_assets.Count == 0)
                {
                    ScriptableObject _o = ScriptableObject.CreateInstance(name);
                    UnityEditor.AssetDatabase.CreateAsset(_o, $"Assets/{name}.asset");
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    UnityEditor.EditorGUIUtility.PingObject(_o);
                }
                else
                {
                    AnalyticsProfile _prof = (AnalyticsProfile)UnityEditor.AssetDatabase
                        .LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(_assets[0]), typeof(AnalyticsProfile));
                    UnityEditor.EditorUtility.SetDirty(_prof);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    UnityEditor.EditorGUIUtility.PingObject(_prof);
                }
            }

            #endregion

            #region SUB-CLASSES

            [System.Serializable]
            public struct SourceSelections
            {
                public List<int> sources;
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
                    "{RuntimeDefinition}" +
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
                    .Replace("{RuntimeDefinition}", _GenerateRuntimeDefinition())
                    .Replace("{RuntimeExportDefinition}", _GenerateRuntimeExportDefinition());

                //  Write file
                File.WriteAllText($"./Assets/{name}.cs", _output);

                //  Refresh the asset database
                UnityEditor.EditorPrefs.SetBool("NewAnalyticsProfile", true);
                UnityEditor.AssetDatabase.ImportAsset($"Assets/{name}.cs");

                string _GenerateRuntimeExports()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        if (dataPoints[i].isExported)
                        {
                            _out += $"\t\t\t\t\t{dataPoints[i].id} = {dataPoints[i].id}_Get(),\n";
                        }
                    }
                    return _out;
                }

                string _GenerateRuntimeDefinition()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        _out +=
                        $"\t\t\t\tpublic DataPoint {dataPoints[i].id} = new DataPoint()\n" +
                        "\t\t\t\t{\n" +
                        $"\t\t\t\t\tid = \"{dataPoints[i].id}\",\n" +
                        $"\t\t\t\t\tisExported = {dataPoints[i].isExported.ToString().ToLower()},\n" +
                        $"\t\t\t\t\tdataType = DataPoint.DataType.{dataPoints[i].dataType},\n" +
                        $"\t\t\t\t\tintVal = {dataPoints[i].GetIntValue()},\n" +
                        $"\t\t\t\t\tfloatVal = {dataPoints[i].GetFloatValue()}f,\n" +
                        $"\t\t\t\t\tboolVal = {dataPoints[i].GetBoolValue().ToString().ToLower()},\n" +
                        $"\t\t\t\t\tstringVal = \"{dataPoints[i].GetStringValue().Replace("\"","\\\"")}\",\n" +
                        "\t\t\t\t};\n" +
                        "\n";
                    }
                    return _out.Substring(0, _out.Length - 1);
                }

                string _GenerateRuntimeAccessorsMutators()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        string _id = dataPoints[i].id;
                        string _param = dataPoints[i].dataType switch
                        {
                            DataPoint.DataType.INT => "Int",
                            DataPoint.DataType.FLOAT => "Float",
                            DataPoint.DataType.BOOL => "Bool",
                            DataPoint.DataType.STRING => "String",
                            _ => "",
                        };
                        //  A standard datapoint
                        if (datapointSourceSelections[i].sources.Count == 0)
                        {
                            _out +=
                                $"\t\t\t//  DataPoint {_id}\n" +
                                $"\t\t\tpublic void {_id}_Set ({_param.ToLower()} pVal) => runtime.{_id}.SetValue(pVal);\n" +
                                $"\t\t\tpublic {_param.ToLower()} {_id}_Get() => runtime.{_id}.Get{_param}Value();\n";
                            string _conditionalMethods = dataPoints[i].dataType switch
                            {
                                DataPoint.DataType.INT => 
                                $"\t\t\tpublic void {_id}_Add(int pVal = 1) => runtime.{_id}.AddIntValue(pVal);\n",
                                DataPoint.DataType.FLOAT =>
                                $"\t\t\tpublic void {_id}_Add(float pVal = 1) => runtime.{_id}.AddFloatValue(pVal);\n",
                                DataPoint.DataType.BOOL =>
                                $"\t\t\tpublic void {_id}_Toggle() => runtime.{_id}.ToggleBoolValue();\n",
                                DataPoint.DataType.STRING => "",
                                _ => "",
                            };
                            _out += $"{_conditionalMethods}\n";
                        }
                        //  A calculated datapoint
                        else
                        {
                            string _sources = "";
                            for (int s = 0; s < datapointSourceSelections[i].sources.Count; s++) _sources += $"runtime.{dataPoints[datapointSourceSelections[i].sources[s]].id},";
                            _out +=
                                $"\t\t\t//  DataPoint {_id}\n" +
                                $"\t\t\tpublic {_param.ToLower()} {_id}_Get() => new Calculations.{calculationNames[i]}().Calculate{_param}({_sources.TrimEnd(',')});\n" +
                                "\n";
                        }
                    }
                    return _out;
                }

                string _GenerateRuntimeExportDefinition()
                {
                    string _out = "";
                    for (int i = 0; i < dataPoints.Count; i ++)
                    {
                        if (dataPoints[i].isExported)
                        {
                            _out += $"\t\t\t\t\tpublic {dataPoints[i].dataType.ToString().ToLower()} {dataPoints[i].id};\n";
                        }
                    }
                    return _out;
                }
            }

            #endregion
        }
    }
}