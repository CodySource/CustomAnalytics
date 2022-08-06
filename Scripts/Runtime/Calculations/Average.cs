using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace Calculations
        {
            public class Average : _Calculation
            {
                public override RangeInt RequiredDataPoints() => new RangeInt(2, 100);
                public override string CalculationDescription() => "( [0] + [1] + ... ) / Count";
                public override float CalculateNumber(ref List<DataPoint> pProfile, params int[] pSources)
                {
                    float _sum = 0f;
                    for (int i = 0; i < pSources.Length; i++) _sum += pProfile[pSources[i]].Number(ref pProfile);
                    return _sum / Mathf.Max((pSources.Length * 1f), 1f);
                }
                public override bool CalculateFlag(ref List<DataPoint> pProfile, params int[] pSources) => false;
                public override string CalculateText(ref List<DataPoint> pProfile, params int[] pSources) => 
                    $"{Mathf.Floor(CalculateNumber(ref pProfile, pSources) * 1000f)/1000f}";
            }
        }
    }
}