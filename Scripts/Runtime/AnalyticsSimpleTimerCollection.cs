using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        /// <summary>
        /// This is an analytics class for tracking a collection of simple (non-splittable) timers
        /// </summary>
        public class AnalyticsSimpleTimerCollection : AnalyticsDataComponent
        {

            #region PROPERTIES

            /// <summary>
            /// The current timer of focus
            /// </summary>
            public string targetTimer = "";

            /// <summary>
            /// Should the current timer be stopped when exported
            /// </summary>
            public bool stopOnExport = true;

            /// <summary>
            /// Should the current timer be stopped when the component is disabled
            /// </summary>
            public bool stopOnDisable = true;

            /// <summary>
            /// Should all timers be reset when exported
            /// </summary>
            public bool eraseAllTimesAfterExport = true;

            /// <summary>
            /// Should a total for all timers be generated on export
            /// </summary>
            public bool showTotalTime = true;

            /// <summary>
            /// The start time for the current timer
            /// </summary>
            private float _startTime = 0f;

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Converts the integer time (in seconds) to a human-readable string ("h hrs m mins s secs").
            /// </summary>
            public string ConvertIntToStringTime(int _pTime) => $"{_pTime / 3600} hrs {(_pTime % 3600) / 60} mins {_pTime % 60} secs";

            /// <summary>
            /// Export the timer data
            /// </summary>
            /// <returns></returns>
            public override DataTable ExportData()
            {
                //  Stop the current timer if desired
                if (stopOnExport) StopTimer();

                //  Create the export table for the component
                DataTable table = _CreateExportTable("Timer", "Time");

                //  Adds another data entry if the total time should be displayed for the timer collection
                if (showTotalTime)
                {
                    float _totalTime = 0f;
                    table.data.ForEach(t => _totalTime += t.floatValue);
                    string _totalTimeStr = "~ " + ConvertIntToStringTime(Mathf.RoundToInt(_totalTime));
                    table.data.Add(new DataEntry() { label = "Approximate Total Time", valueType = EntryValues.String, stringValue = _totalTimeStr });
                }

                //  Add the internal data to the exported table
                table.data.ForEach(e => table.data.Add(e));
                return table;
            }

            /// <summary>
            /// Starts the target timer
            /// </summary>
            public void StartTimer(string _pLabel)
            {
                //  Stop the current target timer
                StopTimer();

                //  Change target timers
                targetTimer = _pLabel;

                //  Set the new start time
                _startTime = Time.time;
            }

            /// <summary>
            /// Stops the target timer
            /// </summary>
            public void StopTimer()
            {
                //  Breakout if there is no target timer
                if (targetTimer == "") return;

                //  Breakout if the target timer cannot be found
                int _index = table.data.FindIndex(d => d.label == targetTimer);
                if (_index == -1) return;

                //  Update the last data entry for the target timer
                DataEntry _d = table.data[_index];
                _d.valueType = EntryValues.String;
                _d.floatValue += Time.time - _startTime;
                _d.stringValue = ConvertIntToStringTime(Mathf.RoundToInt(_d.floatValue));
                table.data[_index] = _d;

                //  Clears the start time for the target timer
                _startTime = -1;
            }

            #endregion

            #region PROTECTED METHODS

            /// <summary>
            /// Called after data is exported.
            /// </summary>
            protected override void OnExportComplete()
            {
                if (eraseAllTimesAfterExport)
                {
                    for (int i = 0; i < table.data.Count; i ++)
                    {
                        table.data[i] = new DataEntry() { label = table.data[i].label, valueType = table.data[i].valueType };
                    }
                }
            }

            #endregion

            #region PRIVATE METHODS

            /// <summary>
            /// Disables the current target timer if desired
            /// </summary>
            private void OnDisable()
            {
                if (stopOnDisable) StopTimer();
            }

            #endregion

        }

    }
}