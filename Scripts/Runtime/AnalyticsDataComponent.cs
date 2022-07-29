using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        /// <summary>
        /// This abstract class is used for the foundation of each new analytics component in the Custom Analytics suite.
        /// Inherit from this class in order to ensure your custom analytics data will be exported by the ATT Analytics Data Controller
        /// </summary>
        public abstract class AnalyticsDataComponent : MonoBehaviour
        {

            #region PROPERTIES

            /// <summary>
            /// The table for the data contained within
            /// </summary>
            public DataTable table = new DataTable();

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Export the data from the component
            /// </summary>
            public abstract DataTable ExportData();

            /// <summary>
            /// Invoked whenever the export is completed
            /// </summary>
            public void CompleteExport()
            {
                OnExportComplete();
            }

            /// <summary>
            /// Enables the component
            /// </summary>
            public void EnableComponent() => enabled = true;

            /// <summary>
            /// Disables the component
            /// </summary>
            public void DisableComponent() => enabled = false;

            /// <summary>
            /// Sets the label for the component
            /// </summary>
            public void SetLabel(string _pString) => table.label = _pString;

            /// <summary>
            /// Adds an annotiation to the data of the component
            /// </summary>
            public void AddAnnotation(string _pString) => table.data.Add(new DataEntry() { label = _pString, valueType = 0 });

            #endregion

            #region PROTECTED METHODS

            /// <summary>
            /// Creates and returns a new epxort table
            /// </summary>
            protected DataTable _CreateExportTable(params string[] pColumns) => new DataTable()
                {
                    label = table.label,
                    columns = new List<string>(pColumns),
                    data = new List<DataEntry>()
                };

            /// <summary>
            /// Virtual methods that will be invoked on Awake & Export Complete respectively
            /// </summary>
            protected virtual void OnAwake() { }
            protected virtual void OnExportComplete() { }

            #endregion

            #region PRIVATE METHODS

            /// <summary>
            /// Triggered on awake of the analytics component
            /// </summary>
            private void Awake()
            {
                //  There can only be component which matches the label
                if (AnalyticsDataController.allDataComponents.Exists(c => c.table.label == table.label))
                {
                    Debug.LogWarning($"Another analytics component with the same label ({table.label}) was already found.\n" +
                        "Please make sure that all analytics components have unique labels.");
                    Destroy(gameObject);
                    return;
                }

                //  Set the component up as a persistent game object
                transform.parent = null;
                DontDestroyOnLoad(gameObject);

                //  Register the component
                AnalyticsDataController.allDataComponents.Add(this); 

                //  Invoke inheritted awake operations
                OnAwake();
            }

            #endregion

        }
    }
}