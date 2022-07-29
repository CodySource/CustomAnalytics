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
        [CustomEditor(typeof(DataTable))]
        public class DataTableEditor : Editor
        {
            SerializedProperty label = null;
            SerializedProperty columns = null;
            SerializedProperty data = null;

            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(label, new GUIContent("Table Label"), true);
                EditorGUILayout.PropertyField(columns, new GUIContent("Columns"), true);
                EditorGUILayout.PropertyField(data, new GUIContent("Data"), true);
                serializedObject.ApplyModifiedProperties();
            }

            private void OnEnable()
            {
                label = serializedObject.FindProperty("label");
                columns = serializedObject.FindProperty("columns");
                data = serializedObject.FindProperty("data");
            }
        }
#else
        public class DataTableEditor {}
#endif
    }
}