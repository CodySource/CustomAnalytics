using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CodySource
{
    namespace CustomAnalytics
    {
#if UNITY_EDITOR
        [CustomEditor(typeof(DataEntry))]
        public class DataEntryEditor : Editor
        {

            #region PROPERTES

            SerializedProperty valueType;
            SerializedProperty label;
            SerializedProperty floatValue;
            SerializedProperty intValue;
            SerializedProperty boolValue;
            SerializedProperty stringValue;

            #endregion

            #region PUBLIC METHODS

            /// <summary>
            /// Overides the inspector GUI
            /// </summary>
            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.9f));

                string _labelVal = "";
                _labelVal = EditorGUILayout.TextField("Entry Label", _labelVal);

                //  Allow changing of data entry type
                EditorGUILayout.PropertyField(valueType, new GUIContent("Value Type"), true);

                //  Display float entry if float type is chosen
                if (((EntryValues)valueType.enumValueFlag).HasFlag(EntryValues.Float))
                {
                    //  Allow changing of data entry type
                    EditorGUILayout.PropertyField(floatValue, new GUIContent("Float Value"), true);
                }

                //  Display int entry if int type is chosen
                if (((EntryValues)valueType.enumValueFlag).HasFlag(EntryValues.Int))
                {
                    //  Allow changing of data entry type
                    EditorGUILayout.PropertyField(intValue, new GUIContent("Int Value"), true);
                }

                //  Display bool entry if bool type is chosen
                if (((EntryValues)valueType.enumValueFlag).HasFlag(EntryValues.Bool))
                {
                    //  Allow changing of data entry type
                    EditorGUILayout.PropertyField(boolValue, new GUIContent("Bool Value"), true);
                }

                //  Display string entry if string type is chosen
                if (((EntryValues)valueType.enumValueFlag).HasFlag(EntryValues.String))
                {
                    //  Allow changing of data entry type
                    EditorGUILayout.PropertyField(stringValue, new GUIContent("String Value"), true);
                }

                EditorGUILayout.EndVertical();

                serializedObject.ApplyModifiedProperties();
            }

            #endregion

            #region PRIVATE METHODS

            private void OnEnable()
            {
                valueType = serializedObject.FindProperty("valueType");
                label = serializedObject.FindProperty("label");
                floatValue = serializedObject.FindProperty("floatValue");
                intValue = serializedObject.FindProperty("intValue");
                boolValue = serializedObject.FindProperty("boolValue");
                stringValue = serializedObject.FindProperty("stringValue");
            }

            #endregion

        }
#else
        public class DataEntryEditor {}
#endif
    }
}