using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        [System.Serializable]
        public abstract class AnalyticsProfile : ScriptableObject
        {
            public abstract string serializedProfileDisplay { get; }
        }
    }
}