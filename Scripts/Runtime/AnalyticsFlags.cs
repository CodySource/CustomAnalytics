using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        /// <summary>
        /// This is an analytics class for tracking simple boolean values
        /// </summary>
        public class AnalyticsFlags : AnalyticsDataComponent
        {

            #region PROPERTIES

            /// <summary>
            /// Are the flags reset after export
            /// </summary>
            public bool resetAfterExport = true;

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Export the flag data
            /// </summary>
            public override DataTable ExportData()
            {
                //  Create the export table for the component
                DataTable table = _CreateExportTable("Flag", "Complete");

                //  Add the internal data to the exported table
                data.ForEach(f => table.data.Add(f));
                return table;
            }

            /// <summary>
            /// Sets the respective flag to true
            /// </summary>
            public void SetFlag(string _pLabel) => data[data.FindIndex(f => f.label == _pLabel)] = new DataEntry() { label = _pLabel, valueType = EntryValues.Bool, boolValue = true };

            /// <summary>
            /// Sets the respective flag to false
            /// </summary>
            public void ResetFlag(string _pLabel) => data[data.FindIndex(f => f.label == _pLabel)] = new DataEntry() { label = _pLabel, valueType = EntryValues.Bool, boolValue = false };

            #endregion

        }
    }   
}