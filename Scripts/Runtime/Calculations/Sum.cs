using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace Calculations
        {
            public class Sum : _Calculation
            {
                public override RangeInt RequiredDataPoints() => new RangeInt(2, 100);
                public override string CalculationDescription() => "[0] + [1] + ...";
                public override int CalculateInt(params DataPoint[] pDataPoints)
                {
                    int r = 0;
                    for (int i = 0; i < pDataPoints.Length; i++) r += pDataPoints[i].GetIntValue();
                    return r;
                }
                public override float CalculateFloat(params DataPoint[] pDataPoints)
                {
                    float r = 0;
                    for (int i = 0; i < pDataPoints.Length; i++) r += pDataPoints[i].GetFloatValue();
                    return r;
                }
                public override bool CalculateBool(params DataPoint[] pDataPoints) => false;
                public override string CalculateString(params DataPoint[] pDataPoints) => "";
            }
        }
    }
}