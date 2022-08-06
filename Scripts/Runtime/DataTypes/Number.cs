using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace DataTypes
        {
            public class Number : _DataType
            {
                public override string GetTypeString() => "float";
                public override string SetTypeString() => "float";
                public override float GetNumber(DataPoint pPoint, ref List<DataPoint> pProfile) =>
                    pPoint.calculation == null ? pPoint.number : pPoint.calculation.CalculateNumber(ref pProfile, pPoint.sources.ToArray());
                public override bool GetFlag(DataPoint pPoint, ref List<DataPoint> pProfile) =>
                    pPoint.calculation == null ? pPoint.flag : pPoint.calculation.CalculateFlag(ref pProfile, pPoint.sources.ToArray());
                public override string GetText(DataPoint pPoint, ref List<DataPoint> pProfile) =>
                    pPoint.calculation == null ? pPoint.text : pPoint.calculation.CalculateText(ref pProfile, pPoint.sources.ToArray());
                public override CustomMethodDefinition[] CustomMethodNames() => new CustomMethodDefinition[]
                {
                    new CustomMethodDefinition() {
                        scope = "public",
                        type = "void",
                        name = "Add",
                        parameterTypes = new string[] { "float" },
                    },
                };
                public void Add(DataPoint pPoint, float pVal) => pPoint.number += pVal;
            }
        }
    }
}