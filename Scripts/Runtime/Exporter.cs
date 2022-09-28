using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace CodySource
{
    namespace CustomAnalytics
    {
        public class Exporter : MonoBehaviour
        {

            #region ENUMERATIONS

            public enum StorageType { PHP_SQL, XAPI };

            #endregion

            #region PROPERTIES

            /// <summary>
            /// Singleton
            /// </summary>
            private static Exporter _instance = null;

            /// <summary>
            /// Auto-instantiation
            /// </summary>
            public static Exporter instance
            {
                get
                {
                    _instance = _instance ?? new GameObject().AddComponent<Exporter>();
                    return _instance;
                }
            }

            /// <summary>
            /// Triggers when the export fails
            /// </summary>
            public static UnityEvent<EXPORT_STATUS> onExportFailed = new UnityEvent<EXPORT_STATUS>();

            /// <summary>
            /// Triggers when the export succeeds
            /// </summary>
            public static UnityEvent<EXPORT_STATUS> onExportComplete = new UnityEvent<EXPORT_STATUS>();

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Perform export operations
            /// </summary>
            public void Export(ExportProfile pProfile, object pObject)
            {
                switch (pProfile.storageType)
                {
                    case StorageType.PHP_SQL:
                        StartCoroutine(_SQL_Export(pProfile, pObject));
                        break;
                    case StorageType.XAPI:
                        break;
                }
            }

            #endregion

            #region PRIVATE METHODS

            /// <summary>
            /// Singleton
            /// </summary>
            private void Awake()
            {
                _instance = _instance ?? this;
                if (_instance == this) DontDestroyOnLoad(this);
                else Destroy(this);
            }

            //  ------------------------------
            //  PHP SQL
            //  ------------------------------

            /// <summary>
            /// Performs the actual object eqport
            /// </summary>
            internal IEnumerator _SQL_Export(ExportProfile pProfile, object pObject)
            {
                WWWForm form = new WWWForm();
                form.AddField("key", $"{pProfile.sql_key}");
                form.AddField("payload", JsonConvert.SerializeObject(pObject));
                using (UnityWebRequest www = UnityWebRequest.Post($"{pProfile.sql_url}", form))
                {
                    yield return www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(www.error);
                        onExportFailed?.Invoke(EXPORT_STATUS.SQL_Error(www.error));
                    }
                    else
                    {
                        try
                        {
                            SQL_Repsponse response = JsonUtility.FromJson<SQL_Repsponse>(www.downloadHandler.text);
                            if (response.success)
                            {
                                Debug.Log($"Success => {response.success}\t\tTimestamp => {response.submission_success}");
                                onExportComplete?.Invoke(EXPORT_STATUS.SQL_Succss(response.submission_success));
                            }
                            else
                            {
                                Debug.Log($"Success => {response.success}\t\tError => {response.error}");
                                onExportFailed?.Invoke(EXPORT_STATUS.SQL_Error(response.error));
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log(e.Message + "\n\n" + www.downloadHandler.text);
                        }
                    }
                }
            }

            #endregion

            #region STRUCTS

            [System.Serializable]
            public struct EXPORT_STATUS
            {
                public bool success;
                public StorageType storageType;
                public string message;
                public static EXPORT_STATUS SQL_Error(string pMessage) => 
                    new EXPORT_STATUS() { success = false, storageType = StorageType.PHP_SQL, message = pMessage };
                public static EXPORT_STATUS SQL_Succss(string pMessage) => 
                    new EXPORT_STATUS() { success = true, storageType = StorageType.PHP_SQL, message = pMessage };
            }

            [System.Serializable]
            public struct SQL_Repsponse
            {
                public bool success;
                public string error;
                public string session_start;
                public string submission_success;
            }

            [System.Serializable]
            public struct ExportProfile
            {
                public StorageType storageType;
                public string sql_url;
                public string sql_key;
                public string sql_host;
                public string sql_db;
                public string sql_user;
                public string sql_pass;
            }

            #endregion
        }
    }
}