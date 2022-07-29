using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Networking;

namespace CodySource
{
    namespace CustomAnalytics
    {
        public class AnalyticsDataController : MonoBehaviour
        {

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
            /// The authentication endpoint
            /// </summary>
            public string authLocation = "";

            /// <summary>
            /// The title of the export
            /// </summary>
            public string exportTitle = "";

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
            /// Performs an export of all registered components
            /// </summary>
            public void ExportData()
            {
                //  Create the export string from the compiled data
                string _outString = _ExportString(_CompileData());

                //  Export the serialzied string
                StartCoroutine(_UploadJSON(_outString, r => Debug.Log($"Analytics Uploaded -> {r}")));

                // Compiles the data from all of the components
                DataExport _CompileData()
                {
                    //  Create a new export
                    DataExport _export = new DataExport() { title = exportTitle, tables = new List<DataTable>() };
                    allDataComponents.Sort();
                    foreach (AnalyticsDataComponent _entry in allDataComponents)
                    {
                        //  Create the table for the component but breakout if the table has no label
                        DataTable table = _entry.ExportData();
                        if (_entry.table.label == "") continue;

                        //  Add the tables and perform any post-export operations
                        _export.tables.Add(table);
                        _entry.CompleteExport();
                    }
                    return _export;
                }

                // Serializes compiled data into an export string (CSV or JSON)
                string _ExportString(DataExport _pExport) => JsonUtility.ToJson(_pExport);
               
                //  Upload the data to the endpoint
                IEnumerator _UploadJSON(string pOut, Action<bool> pOnComplete)
                {
                    bool isDone = false;
                    //  Start the auth process
                    yield return StartCoroutine(_SendRequest(authLocation, pOut, auth => {
                        //  If authenticated successfully, send export json
                        if (auth.result == UnityWebRequest.Result.Success)
                            StartCoroutine(_SendRequest(exportLocation, pOut, post => isDone = (post.result == UnityWebRequest.Result.Success)));
                    }));
                    pOnComplete?.Invoke(isDone);
                }
            }

            /// <summary>
            /// Sends a web request
            /// </summary>
            IEnumerator _SendRequest(string pEndpoint, string pContent, Action<UnityWebRequest> pOnComplete)
            {
                using (UnityWebRequest www = UnityWebRequest.Post(pEndpoint, pContent))
                {
                    yield return www.SendWebRequest();
                    pOnComplete.Invoke(www);
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
                }
            }

            #endregion

        }
    }
}