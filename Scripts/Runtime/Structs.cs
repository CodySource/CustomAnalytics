using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        /// <summary>
        /// A titled compilation of data tables
        /// </summary>
        [System.Serializable]
        public struct DataExport
        {
            public string title;
            public List<DataTable> tables;
        }

        /// <summary>
        /// A labeled table of data entries
        /// </summary>
        [System.Serializable]
        public struct DataTable
        {
            public string label;
            public List<string> columns;
            public List<DataEntry> data;
        }

        /// <summary>
        /// Used for denoting which entry values are relevant when retreving data from a component
        /// </summary>
        [System.Flags]
        public enum EntryValues { Float = (1 << 0), Int = (1 << 1), Bool = (1 << 2), String = (1 << 3) };

        /// <summary>
        /// A singular data entry from a component
        /// </summary>
        [System.Serializable]
        public struct DataEntry
        {
            public EntryValues valueType;
            public string label;
            public float floatValue;
            public int intValue;
            public bool boolValue;
            public string stringValue;
        }
    }
}