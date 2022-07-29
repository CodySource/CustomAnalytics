using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.IMGUI.Controls;

namespace CodySource
{
    namespace CustomAnalytics
    {
        public class ManagerWindow : EditorWindow
        {
            #region PROPERTIES

            /// <summary>
            /// The available calculations & their ranges
            /// </summary>
            public static List<string> calulationOptions = new List<string>();
            public static Dictionary<string, RangeInt> calculationRanges = new Dictionary<string, RangeInt>();
            public static Dictionary<string, string> calculationDescriptions = new Dictionary<string, string>();
            public static EditorWindow win = null;

            public Vector2 winScrollPos = Vector2.zero;

            public List<MultiColumnHeaderState.Column> tableHeaders = new List<MultiColumnHeaderState.Column>()
            {
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent(""),
                    maxWidth = 60f,
                    minWidth = 60f,
                    width = 60f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("ID"),
                    minWidth = 150f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("Export"),
                    maxWidth = 80f,
                    minWidth = 80f,
                    width = 80f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("Format"),
                    maxWidth = 80f,
                    minWidth = 80f,
                    width = 80f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("Value"),
                    minWidth = 150f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("Calculation"),
                    minWidth = 150f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("Sources"),
                    minWidth = 150f,
                    canSort = false
                }
            };
            public MultiColumnHeaderState tableHeaderState;
            public MultiColumnHeader table;

            public bool isIDChanged = false;

            public ProfileDisplay profile = null;
            public string profileName = "MyProfile";

            private bool isClosingProfile = false;

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Load the provided profile
            /// </summary>
            public void LoadProfile(AnalyticsProfile pProfile)
            {
                try
                {
                    profile = JsonUtility.FromJson<ProfileDisplay>(pProfile.serializedProfileDisplay.Replace("\\\"","\""));
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Unable to load profile.");
                }
            }

            /// <summary>
            /// Write / overwrite the profile script & asset
            /// </summary>
            public void UpdateProfile()
            {
                profile.Serialize();
            }

            [MenuItem("Custom Analytics/Manager")]
            public static void ShowWindow()
            {
                //Show existing window instance. If one doesn't exist, make one.
                win = GetWindow(typeof(ManagerWindow));
                win.titleContent = new GUIContent("Analytics Manager");
            }

            public void OnGUI()
            {
                if (profile == null)
                {
                    _ShowStartMenu();
                }
                else
                {
                    if (!isClosingProfile)
                    {
                        if (profile.name == "")
                        {
                            _ShowStartMenu();
                            return;
                        }
                        if (profile.deletedIndex != -1 && Event.current.type == EventType.Repaint) profile.DeleteDataPoint();
                        else if (profile.swapIndex[0] != -1 && Event.current.type == EventType.Repaint) profile.SwapDataPoints();
                        else if (profile.isExporting && Event.current.type == EventType.Repaint) profile.GenerateCS();
                        else if (EditorPrefs.HasKey("NewAnalyticsProfile") &&
                            !EditorApplication.isCompiling &&
                            Event.current.type == EventType.Repaint) profile.GenerateObject();
                        else _ShowProfileMenu();
                    }
                    else _CloseProfile();
                }
            }

            public void OnFocus()
            {
                //  Updates the list of available calculations
                _UpdateCalculationOptions();
                tableHeaderState = new MultiColumnHeaderState(tableHeaders.ToArray());
                table = new MultiColumnHeader(tableHeaderState);
            }

            #endregion

            #region PRIVATE METHODS

            /// <summary>
            /// Updates the list of available calculation options
            /// </summary>
            private void _UpdateCalculationOptions()
            {
                calulationOptions.Clear();
                calculationRanges.Clear();
                calculationDescriptions.Clear();

                System.Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (System.Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Calculations._Calculation)))
                    {
                        Calculations._Calculation _cal = (Calculations._Calculation)System.Activator.CreateInstance(type);
                        calulationOptions.Add($"{type.Name}");
                        calculationRanges.Add(type.Name, _cal.RequiredDataPoints());
                        calculationDescriptions.Add(type.Name, $"output: {_cal.CalculationDescription()}");
                    }
                }

                calulationOptions.Add("<None>");
                calulationOptions.Sort();
            }

            /*
             * ----------------------------------------------------------------------
             *  START MENU
             * ----------------------------------------------------------------------
             */

            /// <summary>
            /// Give the option to create a new profile or load an existing one
            /// </summary>
            private void _ShowStartMenu()
            {
                EditorGUILayout.BeginVertical();

                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginHorizontal();
                //  Create Button
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.5f));
                _DrawProfileName(profileName);
                if (profileName != "" && GUILayout.Button($"Create Profile [ {profileName} ]", GUILayout.Height(35f)))
                {
                    profile = new ProfileDisplay(profileName);
                    profileName = "";
                    _UpdateCalculationOptions();
                    tableHeaderState = new MultiColumnHeaderState(tableHeaders.ToArray());
                    table = new MultiColumnHeader(tableHeaderState);
                }
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10f);
                GUILayout.Label("OR", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                //  Load Button
                if (GUILayout.Button("Load Profile", GUILayout.Width(position.width * 0.5f), GUILayout.Height(35f)))
                {
                    string _file = EditorUtility.OpenFilePanel("Open Analytics Profile", "Assets/", "asset");
                    if (_file != "")
                    {
                        _file = _file.Replace(Application.dataPath, "Assets");
                        //  Try to load asset
                        LoadProfile((AnalyticsProfile)AssetDatabase.LoadAssetAtPath(_file, typeof(AnalyticsProfile)));
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            /// <summary>
            /// Draw a text field for the profile name
            /// </summary>
            private void _DrawProfileName(string pText)
            {
                string _cache = pText;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Profile Name:");
                string _temp = GUILayout.TextField(pText);
                EditorGUILayout.EndHorizontal();
                //  There are changes in the id
                if (_temp != _cache)
                {
                    //  Screen for unwanted characters
                    _temp = (_temp != _cache) ? Regex.Replace(_temp, @"[^a-zA-Z0-9_]", "") : _temp;
                    //  Prevent zero length
                    _temp = (_temp == "") ? "_" : _temp;
                    //  Screen for starting with numbers
                    _temp = (_temp != "" && Regex.Replace(_temp.Substring(0, 1), @"[0-9]", "") == "") ? $"_{_temp}" : _temp;
                    //  Ensure profile name isn't already in use
                    System.Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                    foreach (System.Type type in types)
                    {
                        _temp = (type == typeof(AnalyticsProfile) && type.Name == _temp) ? $"{_temp}_" : _temp;
                        _temp = (type.IsSubclassOf(typeof(AnalyticsProfile)) && type.Name == _temp) ? $"{_temp}_" : _temp;
                    }
                }
                profileName = _temp;
            }

            /*
             * ----------------------------------------------------------------------
             *  PROFILE MENU
             * ----------------------------------------------------------------------
             */

            /// <summary>
            /// Show the menu for the active profile
            /// </summary>
            private void _ShowProfileMenu()
            {
                //  Break out if the profile is null
                if (profile == null) return;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                winScrollPos = EditorGUILayout.BeginScrollView(winScrollPos, GUILayout.Height(position.height - 30f));

                GUILayout.Space(10f);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (GUILayout.Button($"Close [ {profile.name} ]", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f)))
                {
                    isClosingProfile = true;
                }
                if (GUILayout.Button($"Export [ {profile.name} ]", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f)))
                {
                    profile.isExporting = true;
                }

                EditorGUILayout.EndVertical();

                GUILayout.Space(10f);

                //  The table & table controls
                EditorGUILayout.BeginVertical();

                //  Data Point Table
                Rect _table = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _table.height = EditorGUIUtility.singleLineHeight;
                table.ResizeToFit();
                table.OnGUI(_table, 0f);
                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                //  Data Rows
                for (int i = 0; i < profile.dataPoints.Count; i++)
                {
                    GUI.backgroundColor = (i % 2 == 0) ? Color.clear : new Color(0.25f, 0.25f, 0.25f);
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    DataPoint _target = profile.dataPoints[i];
                    GUI.backgroundColor = Color.white;
                    _DrawDelete(i);
                    _DrawId(_target);
                    _DrawExported(_target);
                    _DrawType(_target);
                    _DrawValue(_target, i);
                    _DrawCalculationOptions(_target, i);
                    _DrawSources(_target, i);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                //  The table controls
                _DrawAddRow();

                GUI.backgroundColor = Color.clear;
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndHorizontal();
            }

            /// <summary>
            /// Closes the profile
            /// </summary>
            private void _CloseProfile()
            {
                if (Event.current.type == EventType.Repaint)
                {
                    profile = null;
                    isClosingProfile = false;
                }
            }

            /// <summary>
            /// Draw a button to delete the data point
            /// </summary>
            private void _DrawDelete(int pIndex)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Width(table.GetColumnRect(0).width - 10f), GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight * 2f));

                GUI.backgroundColor = Color.red;
                EditorGUILayout.BeginVertical(GUILayout.Width((table.GetColumnRect(0).width / 2f) - 10f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X"))
                {
                    List<ProfileDisplay.SourceSelections> _dependencies = new List<ProfileDisplay.SourceSelections>
                        (profile.datapointSourceSelections.FindAll(s => s.sources.Contains(pIndex)));
                    if (_dependencies.Count > 0)
                    {
                        string _message = 
                            $"Unable to delete {profile.dataPoints[pIndex].id}.\n" +
                            $"Please remove source references from:\n";
                        _dependencies.ForEach(d => _message += 
                            $"\n\t{profile.dataPoints[profile.datapointSourceSelections.IndexOf(d)].id}");
                        EditorUtility.DisplayDialog("Calculation Dependencies", _message, "Ok");
                    }
                    else
                    {
                        profile.deletedIndex = pIndex;
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginVertical(GUILayout.Width((table.GetColumnRect(0).width / 2f) - 10f));
                if (pIndex == 0) GUILayout.FlexibleSpace();
                else if (GUILayout.Button("↑"))
                {
                    profile.swapIndex[0] = pIndex;
                    profile.swapIndex[1] = pIndex - 1;
                }
                if (pIndex == profile.dataPoints.Count - 1) GUILayout.FlexibleSpace();
                else if (GUILayout.Button("↓"))
                {
                    profile.swapIndex[0] = pIndex;
                    profile.swapIndex[1] = pIndex + 1;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

            /// <summary>
            /// Draw a column value (text-based) for the data point table
            /// </summary>
            private void _DrawId(DataPoint pTarget)
            {
                string _cache = pTarget.id;
                string _temp = GUILayout.TextField(_cache, GUILayout.Width(table.GetColumnRect(1).width - 5f));
                //  There are changes in the id
                if (_temp != _cache)
                {
                    isIDChanged = true;
                    //  Screen for unwanted characters
                    _temp = (_temp != _cache) ? Regex.Replace(_temp, @"[^a-zA-Z0-9_]", "") : _temp;
                    //  Prevent zero length
                    _temp = (_temp == "") ? "_" : _temp;
                    //  Screen for starting with numbers
                    _temp = (_temp != "" && Regex.Replace(_temp.Substring(0, 1), @"[0-9]", "") == "") ? $"_{_temp}" : _temp;
                    //  Ensure id is unique
                    for (int i = 0; i < profile.dataPoints.Count; i++)
                    {
                        _temp = (_temp == profile.dataPoints[i].id && profile.dataPoints[i] != pTarget) ? $"{_temp}_" : _temp;
                    }
                }
                pTarget.id = _temp;
            }

            /// <summary>
            /// Draw a column value (bool-based) for the data point table
            /// </summary>
            private void _DrawExported(DataPoint pTarget)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Width(table.GetColumnRect(2).width - 5f));
                GUILayout.FlexibleSpace();
                pTarget.isExported = EditorGUILayout.Toggle(pTarget.isExported);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            /// <summary>
            /// Draw the data type for the datapoint
            /// </summary>
            private void _DrawType(DataPoint pTarget)
            {
                DataPoint.DataType _cache = pTarget.dataType;
                pTarget.dataType = (DataPoint.DataType)EditorGUILayout.Popup((int)pTarget.dataType,
                    System.Enum.GetNames(typeof(DataPoint.DataType)), GUILayout.Width(table.GetColumnRect(3).width - 3f));
                if (pTarget.dataType != _cache)
                {
                    pTarget.SetValue(0);
                    pTarget.SetValue(0f);
                    pTarget.SetValue(false);
                    pTarget.SetValue(""); 
                    profile.UpdateCalculationPreviews();
                }
            }

            /// <summary>
            /// Draw the value for the datapoint
            /// </summary>
            private void _DrawValue(DataPoint pTarget, int pIndex)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Width(table.GetColumnRect(4).width - 3f));

                int _cacheInt = pTarget.GetIntValue();
                float _cacheFloat = pTarget.GetFloatValue();
                bool _cacheBool = pTarget.GetBoolValue();
                string _cacheString = pTarget.GetStringValue();

                switch (pTarget.dataType)
                {
                    case DataPoint.DataType.INT:
                        if (profile.calculationNames[pIndex] == "<None>")
                        {
                            pTarget.SetValue(EditorGUILayout.IntField(pTarget.GetIntValue()));
                            if (pTarget.GetIntValue() != _cacheInt) profile.UpdateCalculationPreviews();
                        }
                        else EditorGUILayout.LabelField($"{pTarget.GetIntValue()}");
                        break;
                    case DataPoint.DataType.FLOAT:
                        if (profile.calculationNames[pIndex] == "<None>")
                        {
                            pTarget.SetValue(EditorGUILayout.FloatField(pTarget.GetFloatValue()));
                            if (pTarget.GetFloatValue() != _cacheFloat) profile.UpdateCalculationPreviews();
                        }
                        else EditorGUILayout.LabelField($"{pTarget.GetFloatValue()}");
                        break;
                    case DataPoint.DataType.BOOL:
                        if (profile.calculationNames[pIndex] == "<None>")
                        {
                            pTarget.SetValue(EditorGUILayout.Toggle(pTarget.GetBoolValue()));
                            if (pTarget.GetBoolValue() != _cacheBool) profile.UpdateCalculationPreviews();
                        }
                        else EditorGUILayout.LabelField($"{pTarget.GetBoolValue()}");
                        break;
                    case DataPoint.DataType.STRING:
                        if (profile.calculationNames[pIndex] == "<None>")
                        {
                            pTarget.SetValue(EditorGUILayout.TextField(pTarget.GetStringValue()));
                            if (pTarget.GetStringValue() != _cacheString) profile.UpdateCalculationPreviews();
                        }
                        else EditorGUILayout.LabelField($"{pTarget.GetStringValue()}");
                        break;
                }

                EditorGUILayout.EndHorizontal();
            }

            /// <summary>
            /// Draw a column value (string-array-based) for the data point table
            /// </summary>
            private void _DrawCalculationOptions(DataPoint pTarget, int pIndex)
            {
                if (profile.calculationNames[pIndex] != "<None>" || profile.calculationNames.FindAll(c => c == "<None>").Count > 1)
                {
                    //  Get current information
                    string _cal = profile.calculationNames[pIndex];
                    int _index = Mathf.Max(calulationOptions.IndexOf(_cal), 0);
                    int _cache = _index;

                    //  Perform pop-up & update information
                    EditorGUILayout.BeginVertical(GUILayout.Width(table.GetColumnRect(5).width - 3f));
                    _index = EditorGUILayout.Popup(_index, calulationOptions.ToArray(), GUILayout.Width(table.GetColumnRect(5).width - 3f));
                    profile.calculationNames[pIndex] = calulationOptions[_index];
                    _cal = profile.calculationNames[pIndex];

                    //  Clear source selections if no calculation is selected
                    if (_cal == "<None>")
                    {
                        profile.datapointSourceSelections[pIndex].sources.Clear();
                    }
                    else
                    {
                        //  Add sources until within range
                        while (profile.datapointSourceSelections[pIndex].sources.Count < calculationRanges[_cal].start)
                        {
                            //  Find the first data point that is not the current data point and does not use the current data point as a source
                            int def = profile.datapointSourceSelections.FindIndex(s =>
                            {
                                int _index = profile.datapointSourceSelections.IndexOf(s);
                                return (_index != pIndex && !profile.DoesSourceLoopExist(pIndex, _index));
                            });
                            profile.datapointSourceSelections[pIndex].sources.Add(def);
                            profile.UpdateCalculationPreviews();
                        }
                        //  Remove sources until within range
                        while (profile.datapointSourceSelections[pIndex].sources.Count > calculationRanges[_cal].end)
                        {
                            profile.datapointSourceSelections[pIndex].sources.RemoveAt(profile.datapointSourceSelections[pIndex].sources.Count - 1);
                            profile.UpdateCalculationPreviews();
                        }
                        EditorGUILayout.LabelField(calculationDescriptions[_cal], EditorStyles.largeLabel, GUILayout.Width(table.GetColumnRect(5).width - 1f));
                    }
                    EditorGUILayout.EndVertical();
                    //  If there was a change in selection
                    if (_cache != _index)
                    {
                        profile.datapointSourceSelections[pIndex].sources.Clear();
                        profile.UpdateAvailableSources();
                        profile.UpdateCalculationPreviews();
                    }
                }
            }

            /// <summary>
            /// Draw a column value (string-array-based) for the data point table
            /// </summary>
            private void _DrawSources(DataPoint pTarget, int pIndex)
            {
                //  Update the avilable source names to match current ids
                if (isIDChanged)
                {
                    profile.UpdateAvailableSources();
                    isIDChanged = false;
                }

                GUILayout.BeginVertical();

                string _cal = profile.calculationNames[pIndex];
                if (_cal != "<None>")
                {
                    int count = profile.datapointSourceSelections[pIndex].sources.Count;
                    for (int i = 0; i < count; i ++)
                    {
                        int cache = profile.datapointSourceSelections[pIndex].sources[i];
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(table.GetColumnRect(6).width));
                        List<string> sources = new List<string>(profile.availableSources.ToArray());
                        for (int s = 0; s < sources.Count; s++)
                        {
                            sources[s] = (s == pIndex) ? "" : sources[s];
                            sources[s] = profile.DoesSourceLoopExist(pIndex, s) ? "" : sources[s];
                        }
                        EditorGUILayout.LabelField($" [{i}]", GUILayout.Width(count <= 10? 20f : ((count <= 100)? 27f : 35f)));
                        int index = EditorGUILayout.Popup(profile.datapointSourceSelections[pIndex].sources[i], sources.ToArray(),
                            GUILayout.ExpandWidth(true));
                        //  If a new selection was made
                        if (index != cache)
                        {
                            //  Reset the index if the same datapoint was selected
                            if (index == pIndex)
                            {
                                Debug.LogWarning("A datapoint cannot list itself as a source.");
                                index = cache;
                            }
                            //  Reset if the dataPoint has this datapoint as a source
                            if (profile.datapointSourceSelections[index].sources.Contains(pIndex))
                            {
                                Debug.LogWarning($"\"{profile.dataPoints[index].id}\" is already using \"{pTarget.id}\" as a source.");
                                index = cache;
                            }
                        }
                        profile.datapointSourceSelections[pIndex].sources[i] = index;

                        //  Update calculated previews with new, filtered sources
                        if (index != cache) profile.UpdateCalculationPreviews();

                        GUI.backgroundColor = Color.red;
                        if (profile.datapointSourceSelections[pIndex].sources.Count > calculationRanges[_cal].start && GUILayout.Button("X"))
                        {
                            //  Remove source
                            profile.datapointSourceSelections[pIndex].sources.RemoveAt(i);
                            profile.UpdateCalculationPreviews();
                        }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                    GUI.backgroundColor = Color.green;
                    if (profile.datapointSourceSelections[pIndex].sources.Count < calculationRanges[_cal].end && GUILayout.Button("+"))
                    {
                        //  Find the first data point that is not the current data point and does not use the current data point as a source
                        int def = profile.datapointSourceSelections.FindIndex(s =>
                        {
                            int _index = profile.datapointSourceSelections.IndexOf(s);
                            return (_index != pIndex && !profile.DoesSourceLoopExist(pIndex, _index));
                        });
                        profile.datapointSourceSelections[pIndex].sources.Add(def);
                        profile.UpdateCalculationPreviews();
                    }
                    GUI.backgroundColor = Color.white;
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// Draws the add data source button
            /// </summary>
            private void _DrawAddRow()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Add Data Point", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f), GUILayout.Width(100f)))
                {
                    //  Get next dataPoint id
                    string newID = $"DataPoint{profile.dataPoints.Count}";
                    int _tries = 1;
                    while (profile.dataPoints.Exists(d => d.id == newID))
                    {
                        newID = $"DataPoint{profile.dataPoints.Count + _tries}";
                        _tries++;
                    }
                    profile.dataPoints.Add(new DataPoint() { id = newID });
                    profile.datapointSourceSelections.Add(new ProfileDisplay.SourceSelections() { sources = new List<int>() });
                    profile.calculationNames.Add("<None>");
                    profile.UpdateAvailableSources();
                    profile.UpdateCalculationPreviews();
                }
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            #endregion
        }
    }
}