using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace DataTypes
        {
            public class Flag : _DataType
            {
                public override string GetTypeString() => "bool";
                public override string SetTypeString() => "bool";
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
                        name = "Toggle",
                        parameterTypes = new string[] { },
                    },
                };
                public void Toggle(DataPoint pPoint) => pPoint.flag = !pPoint.flag;
            }
        }
    }
}