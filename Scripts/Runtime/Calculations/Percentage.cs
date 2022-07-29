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
                public override int CalculateInt(params DataPoint[] pDataPoints) => 0;
                public override float CalculateFloat(params DataPoint[] pDataPoints)
                {
                    if (pDataPoints.Length < 2 || pDataPoints[1].GetIntValue() == 0) return 0f;
                    return (pDataPoints[0].GetIntValue() * 1f) / (pDataPoints[1].GetIntValue() * 1f);
                }
                public override bool CalculateBool(params DataPoint[] pDataPoints) => false;
                public override string CalculateString(params DataPoint[] pDataPoints) => $"{Mathf.Floor(CalculateFloat(pDataPoints) * 10000f)/100f}%";
            }
        }
    }
}