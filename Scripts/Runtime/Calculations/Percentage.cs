using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace Calculations
        {
            public class Percentage : _Calculation
            {
                public override RangeInt RequiredDataPoints() => new RangeInt(2, 0);
                public override string CalculationDescription() => "[0] / [1]";
                public override float CalculateNumber(ref List<DataPoint> pProfile, params int[] pSources)
                {
                    if (pSources.Length < 2 || pProfile[pSources[1]].Number(ref pProfile) == 0) return 0f;
                    return (pProfile[pSources[0]].Number(ref pProfile) * 1f) / (pProfile[pSources[1]].Number(ref pProfile) * 1f);
                }
                public override bool CalculateFlag(ref List<DataPoint> pProfile, params int[] pSources) => false;
                public override string CalculateText(ref List<DataPoint> pProfile, params int[] pSources) =>
                    $"{Mathf.Floor(CalculateNumber(ref pProfile, pSources) * 10000f)/100f}%";
            }
        }
    }
}