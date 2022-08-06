using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace Calculations
        {
            public class Difference : _Calculation
            {
                public override RangeInt RequiredDataPoints() => new RangeInt(2, 100);
                public override string CalculationDescription() => "[0] - [1] - ...";
                public override float CalculateNumber(ref List<DataPoint> pProfile, params int[] pSources)
                {
                    float r = pSources.Length > 0 ? pProfile[pSources[0]].Number(ref pProfile) : 0f;
                    for (int i = 1; i < pSources.Length; i++) r -= pProfile[pSources[i]].Number(ref pProfile);
                    return r;
                }
                public override bool CalculateFlag(ref List<DataPoint> pProfile, params int[] pSources) => false;
                public override string CalculateText(ref List<DataPoint> pProfile, params int[] pSources) => "";
            }
        }
    }
}