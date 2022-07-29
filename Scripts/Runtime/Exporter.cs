using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        public class Exporter
        {
            #region PUBLIC METHODS

            /// <summary>
            /// Perform export operations
            /// </summary>
            public static void Export(string pJSON)
            {
                Debug.Log(pJSON);
            }

            #endregion
        }
    }
}