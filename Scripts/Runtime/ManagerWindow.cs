using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.IMGUI.Controls;
#endif

namespace CodySource
{
    namespace CustomAnalytics
    {
#if UNITY_EDITOR
        public class ManagerWindow : EditorWindow
        {
#region PROPERTIES

            public static string version = "2.0.0";
            public static EditorWindow win = null;

            /// <summary>
            /// The cached list of available datatypes
            /// </summary>
            public static List<DataTypes._DataType> dataTypes = new List<DataTypes._DataType>();
            public static List<string> dataTypeNames = new List<string>();

            /// <summary>
            /// The cached list of available calculations
            /// </summary>
            public static List<Calculations._Calculation> calculations = new List<Calculations._Calculation>();
            public static List<string> calculationNames = new List<string>();

            /// <summary>
            /// The current window scroll position
            /// </summary>
            public Vector2 winScrollPos = Vector2.zero;
            public int cachedDataPointCount = 0;

            /// <summary>
            /// The headers for the data point table
            /// </summary>
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
                    minWidth = 100f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("Export"),
                    maxWidth = 60f,
                    minWidth = 60f,
                    width = 60f,
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
                    minWidth = 100f,
                    canSort = false
                },
                new MultiColumnHeaderState.Column(){
                    headerContent = new GUIContent("Calculation"),
                    minWidth = 100f,
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

            /// <summary>
            /// Has a datapoint's id been changed
            /// </summary>
            public bool isIDChanged = false;

            /// <summary>
            /// The profile attempting to be loaded
            /// </summary>
            private AnalyticsProfile _load = null;

            /// <summary>
            /// The current profile being displayed
            /// </summary>
            public ProfileDisplay profile = null;

            /// <summary>
            /// The current profile's name
            /// </summary>
            public string profileName = "MyProfile";
            private string _profileNameCache = "";

            /// <summary>
            /// Is the profile being closed
            /// </summary>
            private bool isClosingProfile = false;

            /// <summary>
            /// The cached autosave value for the loaded profile
            /// </summary>
            private string autoSave = "";

            /// <summary>
            /// Used to check if there have been any changes since the last export
            /// </summary>
            private string exportSave = "";

#endregion

#region PUBLIC METHODS

            /// <summary>
            /// Load the provided profile
            /// </summary>
            public void LoadProfile(AnalyticsProfile pProfile)
            {
                try
                {
                    string _loadedString = pProfile.serializedProfileDisplay.Replace("\\\"", "\"");
                    profile = JsonUtility.FromJson<ProfileDisplay>(_loadedString);
                    profile.Deserialize();
                    profile.state = ProfileDisplay.PROFILE_STATE.IDLE;
                    autoSave = profile.Serialize();
                    exportSave = autoSave;
                }
                catch
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

            /// <summary>
            /// Try to delete the auto save
            /// </summary>
            public void OnDestroy()
            {
                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
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
                         switch (profile.state)
                        {
                            case ProfileDisplay.PROFILE_STATE.LOADING:
                                //  TODO:   CREATE DRAW LOADING TO RUN UNDERNEATH EACH STATE
                                break;
                            case ProfileDisplay.PROFILE_STATE.ADDING:
                                if (Event.current.type != EventType.Repaint) break;
                                profile.AddDataPoint(dataTypeNames[0]);
                                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                                break;
                            case ProfileDisplay.PROFILE_STATE.DELETING:
                                if (Event.current.type != EventType.Repaint) break;
                                profile.DeleteDataPoint();
                                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                                break;
                            case ProfileDisplay.PROFILE_STATE.SWAPING:
                                if (Event.current.type != EventType.Repaint) break;
                                profile.SwapDataPoints();
                                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                                break;
                            case ProfileDisplay.PROFILE_STATE.EXPORTING:
                                if (Event.current.type != EventType.Repaint) break;
                                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                                profile.GenerateCS();
                                break;
                            case ProfileDisplay.PROFILE_STATE.COMPILING:
                                if (Event.current.type != EventType.Repaint) break;
                                if (!EditorApplication.isCompiling) profile.GenerateObject();
                                exportSave = profile.Serialize();
                                autoSave = exportSave;
                                break;
                        }
                        _ShowProfileMenu();
                        EditorGUILayout.LabelField(version, EditorStyles.miniLabel);
                    }
                    else _CloseProfile();
                }
            }

            public void OnFocus()
            {
                //  Updates the list of available datatypes & calculations
                _UpdateDataTypeOptions();
                _UpdateCalculationOptions();
                tableHeaderState = new MultiColumnHeaderState(tableHeaders.ToArray());
                table = new MultiColumnHeader(tableHeaderState);
            }

#endregion

#region PRIVATE METHODS

            /// <summary>
            /// Updates the list of available datatype options
            /// </summary>
            private void _UpdateDataTypeOptions()
            {
                dataTypes.Clear();
                dataTypeNames.Clear();
                System.Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (System.Type type in types)
                {
                    if (type.IsSubclassOf(typeof(DataTypes._DataType)))
                    {
                        dataTypes.Add((DataTypes._DataType)System.Activator.CreateInstance(type));
                        dataTypeNames.Add(type.Name);
                    }
                }
                dataTypeNames.Sort();
            }

            /// <summary>
            /// Updates the list of available calculation options
            /// </summary>
            private void _UpdateCalculationOptions()
            {
                calculations.Clear();
                calculationNames.Clear();
                System.Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (System.Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Calculations._Calculation)))
                    {
                        calculations.Add((Calculations._Calculation)System.Activator.CreateInstance(type));
                        calculationNames.Add(type.Name);
                    }
                }
                calculationNames.Add("<None>");
                calculationNames.Sort();
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
                    _UpdateDataTypeOptions();
                    _UpdateCalculationOptions();
                    tableHeaderState = new MultiColumnHeaderState(tableHeaders.ToArray());
                    table = new MultiColumnHeader(tableHeaderState);
                    profile.state = ProfileDisplay.PROFILE_STATE.IDLE;
                }
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10f);
                GUILayout.Label("OR", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10f);

                //  Load Area
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                //  Load Field
                EditorGUILayout.LabelField("Load Profile");
                _load = (AnalyticsProfile)EditorGUILayout.ObjectField(_load, typeof(AnalyticsProfile), false);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                //  Load Button
                if (GUILayout.Button($"Load {((_load != null)? _load.name : "")}", GUILayout.Width(position.width * 0.5f), GUILayout.Height(35f)) &&
                    _load != null)
                {
                    profileName = "";
                    //  Try to load asset
                    LoadProfile(_load);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            /// <summary>
            /// Draw a text field for the profile name
            /// </summary>
            private void _DrawProfileName(string pText)
            {
                _profileNameCache = _profileNameCache == "" ? "" : pText;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Profile Name:");
                string _temp = GUILayout.TextField(pText);
                EditorGUILayout.EndHorizontal();
                //  There are changes in the id
                if (_temp != _profileNameCache)
                {
                    //  Screen for unwanted characters
                    _temp = (_temp != _profileNameCache) ? Regex.Replace(_temp, @"[^a-zA-Z0-9_]", "") : _temp;
                    //  Prevent zero length
                    _temp = (_temp == "") ? "_" : _temp;
                    //  Screen for starting with numbers
                    _temp = (_temp != "" && Regex.Replace(_temp.Substring(0, 1), @"[0-9]", "") == "") ? $"_{_temp}" : _temp;
                    //  Ensure profile name isn't already in use
                    System.Type[] types = Assembly.Load(new AssemblyName("Assembly-CSharp")).GetTypes();
                    foreach (System.Type type in types)
                    {
                        string _cleanName = type.Name.Substring(type.Name.LastIndexOf('.') + 1);
                        if (type == typeof(AnalyticsProfile) || type.IsSubclassOf(typeof(AnalyticsProfile)))
                        {
                            _temp = (_cleanName == _temp) ? $"{_temp}_" : _temp;
                        }
                    }
                }
                profileName = _temp;
                _profileNameCache = _temp;
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

                //  Attempt autosave
                if (Event.current.type == EventType.Repaint)
                {
                    if (EditorPrefs.HasKey("CustomAnalyticsAutoSave"))
                    {
                        if (autoSave == "")
                        {
                            string _loaded = EditorPrefs.GetString("CustomAnalyticsAutoSave").Replace("\\\"", "\"");
                            profile = JsonUtility.FromJson<ProfileDisplay>(_loaded);
                            EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        }
                    }
                    else
                    {
                        autoSave = profile.Serialize();
                        //  Save the profile temporarily for quick load after re-compile
                        EditorPrefs.SetString("CustomAnalyticsAutoSave", autoSave);
                    }
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                winScrollPos = EditorGUILayout.BeginScrollView(winScrollPos, GUILayout.Height(position.height - 30f));

                GUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.BeginVertical();

                Exporter.StorageType _cachedType = profile.exportProfile.storageType;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Export Type:", GUILayout.MaxWidth(100f));
                profile.exportProfile.storageType = (Exporter.StorageType)EditorGUILayout.Popup((int)_cachedType,
                    System.Enum.GetNames(typeof(Exporter.StorageType)));
                if (profile.exportProfile.storageType != _cachedType) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                EditorGUILayout.EndHorizontal();
                switch (_cachedType)
                {
                    case Exporter.StorageType.PHP_SQL:
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Export URL:", GUILayout.MaxWidth(100f));
                        EditorGUILayout.LabelField("https://", GUILayout.MaxWidth(50f));
                        string _cachedURL = profile.exportProfile.sql_url ?? "";
                        profile.exportProfile.sql_url = $"https://{EditorGUILayout.TextField(_cachedURL.Replace($"/{profile.name}.php", "").Replace("https://",""))}/{profile.name}.php";
                        if (profile.exportProfile.sql_url != _cachedURL) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        EditorGUILayout.LabelField($"/{profile.name}.php", GUILayout.MaxWidth(100f));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("SQL Key:", GUILayout.MaxWidth(100f));
                        string _cachedKey = profile.exportProfile.sql_key ?? "";
                        profile.exportProfile.sql_key = EditorGUILayout.TextField(_cachedKey);
                        if (profile.exportProfile.sql_key != _cachedKey) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("SQL Host:", GUILayout.MaxWidth(100f));
                        string _cachedHost = profile.exportProfile.sql_host ?? "";
                        profile.exportProfile.sql_host = EditorGUILayout.TextField(_cachedHost);
                        if (profile.exportProfile.sql_host != _cachedHost) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("SQL DB:", GUILayout.MaxWidth(100f));
                        string _cachedDB = profile.exportProfile.sql_db ?? "";
                        profile.exportProfile.sql_db = EditorGUILayout.TextField(_cachedDB);
                        if (profile.exportProfile.sql_db != _cachedDB) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("SQL User:", GUILayout.MaxWidth(100f));
                        string _cachedUser = profile.exportProfile.sql_user ?? "";
                        profile.exportProfile.sql_user = EditorGUILayout.TextField(_cachedUser);
                        if (profile.exportProfile.sql_user != _cachedUser) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("SQL Pass:", GUILayout.MaxWidth(100f));
                        string _cachedPass = profile.exportProfile.sql_pass ?? "";
                        profile.exportProfile.sql_pass = EditorGUILayout.PasswordField(_cachedPass);
                        if (profile.exportProfile.sql_pass != _cachedPass) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        break;
                    case Exporter.StorageType.XAPI:
                        GUI.backgroundColor = Color.red;
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("This export type is not yet supported.");
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = Color.white;
                        break;
                }

                string _cachedDescription = profile.description;
                EditorGUILayout.LabelField("Profile Description:");
                profile.description = EditorGUILayout.TextArea(profile.description, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f));
                if (profile.description != _cachedDescription) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button($"Close Profile", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f)))
                {
                    isClosingProfile = true;
                }
                GUI.backgroundColor = (exportSave == autoSave) ? Color.grey : Color.green;
                if (GUILayout.Button($"Export Profile", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f)) && (exportSave != autoSave))
                {
                    profile.state = ProfileDisplay.PROFILE_STATE.EXPORTING;
                    autoSave = "";
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10f);

                //  The table & table controls
                EditorGUILayout.BeginVertical();

                //  Data Point Table
                Rect _table = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _table.height = EditorGUIUtility.singleLineHeight;
                table.ResizeToFit();
                table.OnGUI(_table, 0f);
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                //  Data Rows
                if (cachedDataPointCount != profile.dataPoints.Count)
                { 
                    if (Event.current.type == EventType.Repaint) cachedDataPointCount = profile.dataPoints.Count;
                }
                else
                {
                    for (int i = 0; i < profile.dataPoints.Count; i++)
                    {
                        GUI.backgroundColor = (i % 2 == 0) ? Color.clear : new Color(0.25f, 0.25f, 0.25f);
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        DataPoint _target = profile.dataPoints[i];
                        GUI.backgroundColor = Color.white;
                        if (profile.state != ProfileDisplay.PROFILE_STATE.IDLE || _target.type == null)
                        {
                            EditorGUILayout.LabelField($"Loading...");
                            profile.Deserialize();
                        }
                        else
                        {
                            _DrawDelete(i);
                            _DrawId(_target);
                            _DrawExported(_target);
                            _DrawType(_target);
                            _DrawValue(_target, i);
                            _DrawCalculationOptions(_target, i);
                            _DrawSources(_target, i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
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
                    EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
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
                    List<DataPoint> _dependencies = new List<DataPoint>(profile.dataPoints.FindAll(d => d.sources.Contains(pIndex)));
                    if (_dependencies.Count > 0)
                    {
                        string _message =
                            $"Unable to delete {profile.dataPoints[pIndex].id}.\n" +
                            $"Please remove source references from:\n";
                        _dependencies.ForEach(d => _message +=
                            $"\n\t{d.id}");
                        EditorUtility.DisplayDialog("Calculation Dependencies", _message, "Ok");
                    }
                    else
                    {
                        profile.stateInformation[0] = pIndex;
                        profile.state = ProfileDisplay.PROFILE_STATE.DELETING;
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginVertical(GUILayout.Width((table.GetColumnRect(0).width / 2f) - 10f));
                if (pIndex == 0) GUILayout.FlexibleSpace();
                else if (GUILayout.Button("↑"))
                {
                    profile.stateInformation[0] = pIndex;
                    profile.stateInformation[1] = pIndex - 1;
                    profile.state = ProfileDisplay.PROFILE_STATE.SWAPING;
                }
                if (pIndex == profile.dataPoints.Count - 1) GUILayout.FlexibleSpace();
                else if (GUILayout.Button("↓"))
                {
                    profile.stateInformation[0] = pIndex;
                    profile.stateInformation[1] = pIndex + 1;
                    profile.state = ProfileDisplay.PROFILE_STATE.SWAPING;
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
                    EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
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
                bool _cache = pTarget.export;
                pTarget.export = EditorGUILayout.Toggle(pTarget.export);
                if (_cache != pTarget.export) EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            /// <summary>
            /// Draw the data type for the datapoint
            /// </summary>
            private void _DrawType(DataPoint pTarget)
            {
                //  Get the current information & cache the old info
                string _cache = pTarget.typeString;
                int _cacheIndex = dataTypeNames.IndexOf(pTarget.typeString);
                int _typeIndex = Mathf.Max(EditorGUILayout.Popup(_cacheIndex, dataTypeNames.ToArray(), GUILayout.Width(table.GetColumnRect(3).width - 3f)), 0);
                string _typeName = dataTypeNames[_typeIndex];
                //  Update the info if it's different than the cache
                if (_cacheIndex != _typeIndex)
                {
                    pTarget.typeString = _typeName;
                    pTarget.type = (DataTypes._DataType)System.Activator.CreateInstance(
                        System.Type.GetType($"CodySource.CustomAnalytics.DataTypes.{_typeName}"));
                    profile.UpdateAvailableSources();
                    EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                }
            }

            /// <summary>
            /// Draw the value for the datapoint
            /// </summary>
            private void _DrawValue(DataPoint pTarget, int pIndex)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Width(table.GetColumnRect(4).width - 3f));

                //  Since these values are not calculations, we don't need to pass the source parameters
                float _cacheFloat = pTarget.Number(ref profile.dataPoints);
                bool _cacheBool = pTarget.Flag(ref profile.dataPoints);
                string _cacheString = pTarget.Text(ref profile.dataPoints);

                //  Draw the input label to adjust the default value for the data point
                if (pTarget.calculation == null)
                {
                    //  Since these values are not calculations, we don't need to pass the source parameters
                    switch (pTarget.type.SetTypeString())
                    {
                        case "float":
                            float _floatVal = EditorGUILayout.FloatField(pTarget.Number(ref profile.dataPoints));
                            if (_floatVal != _cacheFloat)
                            {
                                pTarget.Set(_floatVal);
                                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                            }
                            break;
                        case "bool":
                            bool _boolVal = EditorGUILayout.Toggle(pTarget.Flag(ref profile.dataPoints));
                            if (_boolVal != _cacheBool)
                            {
                                pTarget.Set(_boolVal);
                                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                            }
                            break;
                        case "string":
                            string _stringVal = EditorGUILayout.TextField(pTarget.Text(ref profile.dataPoints));
                            if (_stringVal != _cacheString)
                            {
                                pTarget.Set(_stringVal);
                                EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                            }
                            break;
                    }
                }

                //  Add a custom label if the output type is different than the input type
                if (pTarget.calculation != null || pTarget.type.SetTypeString() != pTarget.type.GetTypeString())
                {
                    switch (pTarget.type.GetTypeString())
                    {
                        case "float":
                            GUILayout.Label($"{pTarget.Number(ref profile.dataPoints)}");
                            break;
                        case "bool":
                            GUILayout.Label($"{pTarget.Flag(ref profile.dataPoints)}");
                            break;
                        case "string":
                            GUILayout.Label($"{pTarget.Text(ref profile.dataPoints)}");
                            break;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            /// <summary>
            /// Draw a column value (string-array-based) for the data point table
            /// </summary>
            private void _DrawCalculationOptions(DataPoint pTarget, int pIndex)
            {
                if (pTarget.calculation != null || profile.dataPoints.FindAll(d => d.calculation == null).Count > 1)
                {
                    //  Get current information
                    string _cache = pTarget.calculationString;
                    int _cacheIndex = calculationNames.IndexOf(pTarget.calculationString);
                    EditorGUILayout.BeginVertical(GUILayout.Width(table.GetColumnRect(5).width - 3f));
                    int _calculationIndex = EditorGUILayout.Popup(_cacheIndex, calculationNames.ToArray(), GUILayout.Width(table.GetColumnRect(5).width - 4f));
                    string _calculationName = calculationNames[_calculationIndex];
                    //  If there was a change
                    if (_cacheIndex != _calculationIndex)
                    {
                        //  If the calculation was reset
                        if (_calculationName == "<None>")
                        {
                            pTarget.calculationString = "<None>";
                            pTarget.calculation = null;
                            pTarget.sources.Clear();
                        }
                        else
                        {
                            //  Update the calculation information
                            pTarget.calculationString = _calculationName;
                            pTarget.calculation = (Calculations._Calculation)System.Activator.CreateInstance(
                                System.Type.GetType($"CodySource.CustomAnalytics.Calculations.{_calculationName}"));
                            //  Add sources until within range
                            while (pTarget.sources.Count < pTarget.calculation.RequiredDataPoints().start)
                            {
                                //  Find the first data point that is not the current data point and does not use the current data point as a source
                                int def = profile.dataPoints.FindIndex(d =>
                                {
                                    int _index = profile.dataPoints.IndexOf(d);
                                    return (_index != pIndex && !profile.DoesSourceLoopExist(pIndex, _index));
                                });
                                pTarget.sources.Add(def);
                            }
                            //  Remove sources until within range
                            while (pTarget.sources.Count > pTarget.calculation.RequiredDataPoints().end)
                            {
                                pTarget.sources.RemoveAt(pTarget.sources.Count - 1);
                            }
                        }
                        profile.UpdateAvailableSources();
                        EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                    }
                    if (pTarget.calculation != null)
                    {
                        //  Draw the calculation description
                        GUILayout.Label(pTarget.calculation.CalculationDescription(), EditorStyles.largeLabel, GUILayout.Width(table.GetColumnRect(5).width - 4f));
                    }
                    EditorGUILayout.EndVertical();
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

                if (pTarget.calculationString != "<None>")
                {
                    int count = pTarget.sources.Count;
                    int _remove = -1;
                    for (int i = 0; i < count; i++)
                    {
                        int _sourceCacheIndex = pTarget.sources[i];
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(table.GetColumnRect(6).width - 3f));

                        //  Filter the available datapoint sources to prevent selection of invalid datapoints
                        List<string> sourceOptions = new List<string>(profile.dataPointNames.ToArray());
                        for (int s = 0; s < sourceOptions.Count; s++)
                        {
                            sourceOptions[s] = (s == pIndex) ? "" : sourceOptions[s];
                            sourceOptions[s] = profile.DoesSourceLoopExist(pIndex, s) ? "" : sourceOptions[s];
                        }

                        EditorGUILayout.LabelField($" [{i}]", GUILayout.Width(count <= 10 ? 20f : ((count <= 100) ? 27f : 35f)));

                        //  Display source pop-up
                        int sourceIndex = EditorGUILayout.Popup(pTarget.sources[i], sourceOptions.ToArray(), GUILayout.ExpandWidth(true));

                        //  If a change was made
                        if (sourceIndex != _sourceCacheIndex)
                        {
                            //  Reset the index if the same datapoint was selected
                            if (sourceIndex == pIndex)
                            {
                                Debug.LogWarning("A datapoint cannot list itself as a source.");
                                sourceIndex = _sourceCacheIndex;
                            }
                            //  Reset if the dataPoint has this datapoint as a source
                            if (pTarget.sources.Contains(pIndex))
                            {
                                Debug.LogWarning($"\"{profile.dataPoints[sourceIndex].id}\" is already using \"{pTarget.id}\" as a source.");
                                sourceIndex = _sourceCacheIndex;
                            }
                            //  Set the value
                            pTarget.sources[i] = sourceIndex;
                            EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                        }

                        //  Draw the remove button for the source
                        GUI.backgroundColor = Color.red;
                        if (pTarget.sources.Count > pTarget.calculation.RequiredDataPoints().start && GUILayout.Button("X"))
                        {
                            //  Remove source
                            _remove = i;
                        }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                    if (_remove != -1)
                    {
                        //  Remove source
                        pTarget.sources.RemoveAt(_remove);
                        EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
                    }
                    //  Draw Add Source button
                    GUI.backgroundColor = Color.green;
                    if (pTarget.sources.Count < pTarget.calculation.RequiredDataPoints().end && GUILayout.Button("+"))
                    {
                        //  Find the first data point that is not the current data point and does not use the current data point as a source
                        int def = profile.dataPoints.FindIndex(d =>
                        {
                            int _index = profile.dataPoints.IndexOf(d);
                            return (_index != pIndex && !profile.DoesSourceLoopExist(pIndex, _index));
                        });
                        pTarget.sources.Add(def);
                        EditorPrefs.DeleteKey("CustomAnalyticsAutoSave");
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
                    profile.state = ProfileDisplay.PROFILE_STATE.ADDING;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

#endregion
        }
#else
        public class ManagerWindow { }
#endif
    }
}