using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodySource
{
    namespace CustomAnalytics
    {
        [System.Serializable]
        public class DataPoint
        {
            #region ENUMERATIONS

            public enum DataType { INT, FLOAT, BOOL, STRING };

            #endregion

            #region PROPERTIES

            public string id = "";
            public bool isExported = false;
            public DataType dataType = DataType.INT;
            public int intVal = 0;
            public float floatVal = 0f;
            public bool boolVal = false;
            public string stringVal = "";

            #endregion

            #region PUBLIC METHODS

            public DataPoint() { }
            public DataPoint(DataPoint pPoint)
            {
                id = pPoint.id;
                isExported = pPoint.isExported;
                dataType = pPoint.dataType;
                intVal = pPoint.intVal;
                floatVal = pPoint.floatVal;
                boolVal = pPoint.boolVal;
                stringVal = pPoint.stringVal;
            }

            public int GetIntValue() => intVal;
            public float GetFloatValue() => floatVal;
            public bool GetBoolValue() => boolVal;
            public string GetStringValue() => stringVal;

            public void SetValue(int pInt) => intVal = pInt;
            public void SetValue(float pFloat) => floatVal = pFloat;
            public void SetValue(bool pBool) => boolVal = pBool;
            public void SetValue(string pString) => stringVal = pString;

            /*
             *  INT OPERATIONS
             */
            public void AddIntValue(int pVal = 1) => intVal += pVal;

            /*
             *  FLOAT OPERATIONS
             */
            public void AddFloatValue(float pVal = 1f) => floatVal += pVal;

            /*
             *  BOOL OPERATIONS
             */
            public void ToggleBoolValue() => boolVal = !boolVal;

            /*
             *  STRING OPERATIONS
             */


            #endregion
        }
    }
}