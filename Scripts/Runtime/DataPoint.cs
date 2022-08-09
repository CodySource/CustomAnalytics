using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodySource.CustomAnalytics.DataTypes;

namespace CodySource
{
    namespace CustomAnalytics
    {
        [System.Serializable]
        public class DataPoint
        {
            #region PROPERTIES

            public string id = "";
            public bool export = false;
            public float number = 0f;
            public bool flag = false;
            public string text = "";
            public List<int> sources = new List<int>();

            //  For serializing / deserializing
            public string typeString = "Number";
            public string calculationString = "<None>";

            //  For runtime
            public _DataType type = null;
            public Calculations._Calculation calculation = null;

            #endregion

            #region PUBLIC METHODS

            public DataPoint() { }
            public DataPoint(DataPoint pPoint)
            {
                id = pPoint.id;
                export = pPoint.export;
                number = pPoint.number;
                flag = pPoint.flag;
                text = pPoint.text;
                sources = new List<int>(pPoint.sources);
                typeString = pPoint.typeString;
                type = (_DataType)System.Activator.CreateInstance(
                    System.Type.GetType($"CodySource.CustomAnalytics.DataTypes.{typeString}"));
                calculationString = pPoint.calculationString;
                calculation = (calculationString == "<None>") ? null : 
                    (Calculations._Calculation)System.Activator.CreateInstance(
                        System.Type.GetType($"CodySource.CustomAnalytics.Calculations.{calculationString}"));
            }

            /// <summary>
            /// Returns the generated profile string for the datapoint
            /// </summary>
            public string GetProfileGenerationString()
            {
                //  Add the default Accessor and Mutator Methods
                string _defaultGetter = type.GetTypeString() switch
                {
                    "float" => "Number",
                    "bool" => "Flag",
                    "string" => "Text",
                    _ => ""
                };
                string r =
                    $"\t\t\t//  DataPoint {id}\n" +
                    ((calculation == null)? $"\t\t\tpublic void {id}_Set ({type.SetTypeString()} pVal) => runtime.{id}.Set(pVal);\n" : "")+
                    $"\t\t\tpublic {type.GetTypeString()} {id}_Get() => runtime.{id}.{_defaultGetter}(ref runtime.dataPoints);\n";
                //  For each custom method definition
                if (calculation == null)
                {
                    for (int i = 0; i < type.CustomMethodNames().Length; i ++)
                    {
                        //  Get the method definition
                        _DataType.CustomMethodDefinition _def = type.CustomMethodNames()[i];
                        string _pDefs = "";
                        string _pVals = "";
                        //  For each of the parameter types listed
                        for (int p = 0; p < _def.parameterTypes.Length; p++)
                        {
                            _pDefs += $"{_def.parameterTypes[p]} p{p}, ";
                            _pVals += $", p{p}";
                        }
                        //  Add the newly generated custom method call
                        r +=
#if UNITY_2021_OR_NEWER
                        $"\t\t\t{_def.scope} {_def.type} {id}_{_def.name}({((_pDefs == "") ? ", " : _pDefs)[..^2]}) => " +
#else
                        $"\t\t\t{_def.scope} {_def.type} {id}_{_def.name}({((_pDefs == "") ? ", " : _pDefs).Substring(0, _pDefs.Length - 2)}) => " +
#endif
                            $"((DataTypes.{type.CleanName()})runtime.{id}.type).{_def.name}(runtime.{id}{((_pVals == "")? "" : _pVals)});\n";
                    }
                }
                return $"{r}\n";
            }

            public float Number(ref List<DataPoint> pProfile) => type.GetNumber(this, ref pProfile);
            public bool Flag(ref List<DataPoint> pProfile) => type.GetFlag(this, ref pProfile);
            public string Text(ref List<DataPoint> pProfile) => type.GetText(this, ref pProfile);

            public void Set(float pFloat) => number = pFloat;
            public void Set(bool pBool) => flag = pBool;
            public void Set(string pString) => text = pString;

#endregion
        }
    }
}