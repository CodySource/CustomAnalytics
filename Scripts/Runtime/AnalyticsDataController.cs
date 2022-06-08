using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace CodySource
{
    namespace CustomAnalytics
    {
        public class AnalyticsDataController : MonoBehaviour
        {

            #region ENUMERATIONS

            public enum ExportType { CSV, JSON };
            [System.Flags] public enum ExportGroup { Group1 = 1 << 0, Group2 = 1 << 1, Group3 = 1 << 2, Group4 = 1 << 3, Group5 = 1 << 4, Group6 = 1 << 5 };

            #endregion

            #region PROPERTIES

            /// <summary>
            /// The registry of all analytics components
            /// </summary>
            public static List<AnalyticsDataComponent> allDataComponents = new List<AnalyticsDataComponent>();

            /// TODO:   SUPPORT URL EXPORT LOCATION FOR API CALLS

            /// <summary>
            /// The export location
            /// </summary>
            public string exportLocation = "%default%";

            /// <summary>
            /// The title of the export
            /// </summary>
            public string exportTitle = "";

            /// <summary>
            /// The current export group
            /// </summary>
            public ExportGroup exportGroup = (ExportGroup)~0;

            /// <summary>
            /// The current export type
            /// </summary>
            public ExportType exportType = ExportType.CSV;

            /// <summary>
            /// The analytics session id
            /// </summary>
            public static string sessionID = "";

            /// <summary>
            /// The number for the session
            /// </summary>
            public static int sessionNumber = 0;

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Set the title for the export
            /// </summary>
            public void SetTitle(string _pTitle) => exportTitle = _pTitle;

            /// <summary>
            /// Set the export group
            /// </summary>
            public void SetExportGroup(int _pGroup) => exportGroup = (ExportGroup)_pGroup;

            /// <summary>
            /// Enables a particular export group
            /// </summary>
            public void TurnOnExportGroup(int _pGroup) => exportGroup |= (ExportGroup)((int)exportGroup << _pGroup);

            /// <summary>
            /// Disables a particular export group
            /// </summary>
            public void TurnOffExportGroup(int _pGroup) => exportGroup &= ~(ExportGroup)((int)exportGroup << _pGroup);

            /// <summary>
            /// Performs an export of all registered components
            /// </summary>
            public void ExportData()
            {
                //  Breakout if the export group is 0
                if (exportGroup == 0) return;

                //  Create the export string from the compiled data
                string _outString = _ExportString(_CompileData());

                //  Export the serialzied string
                _WriteFile(_outString);

                // Compiles the data from all of the components
                DataExport _CompileData()
                {
                    //  Create a new export
                    DataExport _export = new DataExport() { title = exportTitle, tables = new List<DataTable>() };
                    allDataComponents.Sort();
                    foreach (AnalyticsDataComponent _entry in allDataComponents)
                    {
                        //  Breakout if disabled & not flagged for export while disabled
                        if (!_entry.enabled && !_entry.exportWhileDisabled) continue;

                        //  Breakout if not in the current export group
                        if (!_entry.exportGroup.HasFlag(exportGroup)) continue;

                        //  Create the table for the component but breakout if the table has no label
                        DataTable table = _entry.ExportData();
                        if (_entry.label == "") continue;

                        //  Add the tables and perform any post-export operations
                        _export.tables.Add(table);
                        _entry.CompleteExport();
                    }
                    return _export;
                }

                // Serializes compiled data into an export string (CSV or JSON)
                string _ExportString(DataExport _pExport)
                {
                    //  For JSON, use built-in json utility
                    if (exportType == ExportType.JSON) return JsonUtility.ToJson(_pExport);

                    //  For CSV, create CSV export
                    string _r = "";
                    foreach (DataTable table in _pExport.tables)
                    {
                        _r += table.label + "`";
                        foreach (string col in table.columns) _r += $"{col},";
                        _r += "`";
                        //  Export the data from each entry based off of whether or not the entry has the flag for the data type
                        foreach (DataEntry entry in table.data)
                        {
                            string _value = "";
                            _value += (entry.valueType.HasFlag(EntryValues.Bool)) ? entry.boolValue.ToString() + "," : "";
                            _value += (entry.valueType.HasFlag(EntryValues.Float)) ? entry.floatValue.ToString() + "," : "";
                            _value += (entry.valueType.HasFlag(EntryValues.Int)) ? entry.intValue.ToString() + "," : "";
                            _value += (entry.valueType.HasFlag(EntryValues.String)) ? entry.stringValue + "," : "";
                            _r += $"{entry.label}, {_value}`";
                        }
                        _r += " ,` ,`";
                    }
                    return _r;
                }

                // Write the string to the file
                void _WriteFile(string _pOut)
                {
                    //  Establish variables
                    string path = "";
                    string title = "";
                    string fName = "";
                    string full = "";
                    int _version = 0;

    #if UNITY_STANDALONE_WIN
                    path = $"{((exportLocation == "%default%") ? Application.dataPath + "/Analytics Exports/" : exportLocation)}/{sessionID}  -- {sessionNumber}/";
    #endif
    #if UNITY_ANDROID && !UNITY_EDITOR
                    path = $"{((exportLocation == "%default%") ? Application.persistentDataPath + "/Analytics Exports/" : exportLocation)}/{sessionID}  -- {sessionNumber}/";
    #endif

                    title = $"{exportTitle}";
                    fName = $"{title}.{((exportType == ExportType.JSON)? "json" : "csv")}";
                    full = path + fName;
                    _version = 0;

                    //  Create the directory if it does not exist
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    //  Update the file name with the file version
                    while (File.Exists(full))
                    {
                        _version++;
                        fName = $"{title} ({_version}).{((exportType == ExportType.JSON) ? "json" : "csv")}";
                        full = path + fName;
                    }

                    //  Perform write operation
                    switch (exportType)
                    {
                        case ExportType.CSV:
                            _WriteCSV(_pOut, '`');
                            break;
                        case ExportType.JSON:
                            _WriteJSON(_pOut);
                            break;
                    }

                    // Writes a csv file
                    void _WriteCSV(string _pOut, char _pSepChar)
                    {
                        StreamWriter _writer = File.CreateText(full);
                        List<string> text = new List<string>(_pOut.Split(_pSepChar));
                        text.ForEach(l => _writer.WriteLine(l));
                        _writer.Close();
                    }

                    // Writes a json file
                    void _WriteJSON(string _pOut)
                    {
                        StreamWriter _writer = File.CreateText(full);
                        _writer.WriteLine(_pOut);
                        _writer.Close();
                    }
                }

            }

            #endregion

            #region PRIVATE METHODS

            /// <summary>
            /// Triggered on awake
            /// </summary>
            private void Awake()
            {
                //  If there is no session id
                if (sessionID == "")
                {
                    //  Create a new session id
                    sessionID = $"Session ({DateTime.Today.ToShortDateString().Replace('/', '_')})";

                    //  Build export bath
                    string path = $"{((exportLocation == "%default%") ? Application.dataPath + "/Analytics Exports/" : exportLocation)}/{sessionID}  -- {sessionNumber}/";

                    //  If a session already exists for the day increment the session number
                    while (Directory.Exists(path))
                    {
                        sessionNumber++;
                        path = $"{((exportLocation == "%default%") ? Application.dataPath + "/Analytics Exports/" : exportLocation)}/{sessionID}  -- {sessionNumber}/";
                    }
                }
            }

            #endregion

        }
    }
}