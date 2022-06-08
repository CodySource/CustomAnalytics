using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        /// <summary>
        /// This is an analytics class for tracking simple integer values.
        /// This component may be used to track multiple integer values such as Score, Round, Lives, Interactions, etc...
        /// </summary>
        public class AnalyticsCounter : AnalyticsDataComponent
        {

            #region PROPERTIES

            public bool resetAfterExport = true;
            public bool showCounterTotals = true;

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Export the counter data
            /// </summary>
            public override DataTable ExportData()
            {
                //  Create the export table for the component
                DataTable table = _CreateExportTable("Counter", "Value");

                //  Adds another data entry if the total count should be displayed for the counters
                if (showCounterTotals)
                {
                    int _totalCount = 0;
                    data.ForEach(c => _totalCount += c.intValue);
                    table.data.Add(new DataEntry() { label = "Total Count", valueType = EntryValues.Int, intValue = _totalCount });
                }

                //  Add the internal data to the exported table
                data.ForEach(f => table.data.Add(f));
                return table;
            }

            /// <summary>
            /// Increments the provided counter by the provided amount
            /// </summary>
            public void IncrementCounter(string pLabel, int pAmount = 1)
            {
                int _index = data.FindIndex(f => f.label == pLabel);
                int _count = data[_index].intValue + pAmount;
                data[_index] = new DataEntry()
                {
                    label = pLabel,
                    valueType = EntryValues.Int,
                    intValue = _count
                };
            }

            /// <summary>
            /// Decrements the provided counter by the provided amount
            /// </summary>
            public void DecrementCounter(string pLabel, int pAmount = 1)
            {
                int _index = data.FindIndex(f => f.label == pLabel);
                int _count = data[_index].intValue - pAmount;
                data[_index] = new DataEntry()
                {
                    label = pLabel,
                    valueType = EntryValues.Int,
                    intValue = _count
                };
            }

            #endregion

        }
    }
}