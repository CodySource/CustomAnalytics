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
        public abstract class AnalyticsDataComponent : MonoBehaviour, System.IComparable<AnalyticsDataComponent>
        {

            #region PROPERTIES

            /// <summary>
            /// The label for the component (Required in order for the component's data to be exported)
            /// </summary>
            public string label = "";

            /// <summary>
            /// If flagged, will export the component's data while the component is disabled
            /// </summary>
            public bool exportWhileDisabled = false;

            /// <summary>
            /// If flagged, will disable the component after export
            /// </summary>
            public bool disableAfterExport = false;

            /// <summary>
            /// The applicable export group for the component
            /// </summary>
            public AnalyticsDataController.ExportGroup exportGroup = (AnalyticsDataController.ExportGroup)~0;

            /// <summary>
            /// Used to override the export order for greater organizational control in the data export
            /// </summary>
            public int exportOrder = -1;

            /// <summary>
            /// The data stored within the component
            /// </summary>
            public List<DataEntry> data = new List<DataEntry>();

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
                if (disableAfterExport) enabled = false;
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
            public void SetLabel(string _pString) => label = _pString;

            /// <summary>
            /// Adds an annotiation to the data of the component
            /// </summary>
            public void AddAnnotation(string _pString) => data.Add(new DataEntry() { label = _pString, valueType = 0 });

            /// <summary>
            /// Used for sorting components by export order
            /// </summary>
            public int CompareTo(AnalyticsDataComponent other)
            {
                if (other == null) return 1;
                if (exportOrder == -1) exportOrder = transform.GetSiblingIndex();
                return exportOrder.CompareTo(other.exportOrder);
            }
            #endregion

            #region PROTECTED METHODS

            /// <summary>
            /// Creates and returns a new epxort table
            /// </summary>
            protected DataTable _CreateExportTable(params string[] pColumns) => new DataTable()
                {
                    label = label,
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
                if (AnalyticsDataController.allDataComponents.Exists(c => c.label == label))
                {
                    Debug.LogWarning($"Another analytics component with the same label ({label}) was already found.\n" +
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