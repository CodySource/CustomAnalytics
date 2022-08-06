using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        namespace DataTypes
        {
            public abstract class _DataType
            {
                public string CleanName() => GetType().ToString().Replace("CodySource.CustomAnalytics.DataTypes.","");
                public abstract CustomMethodDefinition[] CustomMethodNames();
                public abstract string GetTypeString();
                public abstract string SetTypeString();
                public abstract float GetNumber(DataPoint pPoint, ref List<DataPoint> pProfile);
                public abstract bool GetFlag(DataPoint pPoint, ref List<DataPoint> pProfile);
                public abstract string GetText(DataPoint pPoint, ref List<DataPoint> pProfile);
                public struct CustomMethodDefinition
                {
                    public string scope, type, name;
                    //  When defining parameter types, ignore the DataPoint which is passed in as the first parameter
                    public string[] parameterTypes;
                }
            }
        }
    }
}