using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace Calculations
        {
            public abstract class _Calculation
            {
                /// <summary>
                /// The acceptable required data points to perform the calculation
                /// </summary>
                public abstract RangeInt RequiredDataPoints();
                /// <summary>
                /// A short description of what the calulation output will look like
                /// </summary>
                public abstract string CalculationDescription();
                public abstract float CalculateNumber(ref List<DataPoint> pProfile, params int[] pSources);
                public abstract bool CalculateFlag(ref List<DataPoint> pProfile, params int[] pSources);
                public abstract string CalculateText(ref List<DataPoint> pProfile, params int[] pSources);
            }                
        }
    }
}