using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        /// <summary>
        /// This is an analytics class for tracking a splittable timer
        /// </summary>
        public class AnalyticsSplitTimer : AnalyticsDataComponent
        {

            #region PROPERTIES

            /// <summary>
            /// The current lap time of the timer
            /// </summary>
            public float startTime = 0f;

            /// <summary>
            /// Does the timer split on export
            /// </summary>
            public bool splitOnExport = true;

            /// <summary>
            /// Does the timer split on disable
            /// </summary>
            public bool splitOnDisable = true;

            /// <summary>
            /// Are the split times erased after export
            /// </summary>
            public bool eraseSplitTimesAfterExport = true;

            /// <summary>
            /// Are the split times totaled on export
            /// </summary>
            public bool showTotalTime = true;

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Converts the integer time (in seconds) to a human-readable string ("h hrs m mins s secs").
            /// </summary>
            public string ConvertIntToStringTime(int _pTime) => $"{_pTime / 3600} hrs {(_pTime % 3600) / 60} mins {_pTime % 60} secs";

            /// <summary>
            /// Export the timer data
            /// </summary>
            public override DataTable ExportData()
            {
                //  Split if desired
                if (splitOnExport) SplitTimer("On Export");

                //  Create the export table for the component
                DataTable table = _CreateExportTable("Label", "Time");

                //  Adds another data entry if the total time should be displayed for the timer
                if (showTotalTime)
                {
                    float _totalTime = 0f;
                    table.data.ForEach(t => _totalTime += t.floatValue);
                    string _totalTimeStr = "~ " +ConvertIntToStringTime(Mathf.RoundToInt(_totalTime));
                    table.data.Add(new DataEntry() { label = "Approximate Total Time", valueType = EntryValues.String, stringValue = _totalTimeStr });
                }

                //  Add the internal data to the exported table
                table.data.ForEach(e => table.data.Add(e));
                return table;
            }

            /// <summary>
            /// Starts the timer
            /// </summary>
            public void StartTimer() => enabled = true;

            /// <summary>
            /// Stops the timer
            /// </summary>
            public void StopTimer() => enabled = false;

            /// <summary>
            /// Splits the timer and adds the split to the split times list
            /// </summary>
            public void SplitTimer(string pLabel = "")
            {
                //  Creates the readable split time entry
                float _time = Time.time - startTime;
                string _convertedTime = "~ " + ConvertIntToStringTime(Mathf.RoundToInt(_time));

                //  Adds the data entry with the split info
                table.data.Add(new DataEntry() {
                    label = (pLabel != "") ? pLabel : table.label,
                    valueType = EntryValues.String,
                    floatValue = (_time),
                    stringValue = _convertedTime });

                //  Reset the start time (lap time)
                startTime = Time.time;
            }

            #endregion

            #region PROTECTED METHODS

            /// <summary>
            /// Called after data is exported.
            /// </summary>
            protected override void OnExportComplete()
            {
                if (eraseSplitTimesAfterExport) table.data.Clear();
            }

            #endregion

            #region PRIVATE METHODS

            /// <summary>
            /// Timers are started when they are enabled
            /// </summary>
            private void OnEnable() => startTime = Time.time;

            /// <summary>
            /// Timers are split when they are disabled
            /// </summary>
            private void OnDisable()
            {
                //  Split if desired
                if (splitOnDisable) SplitTimer("Timer Stopped");
            }

            #endregion

        }
    }
}