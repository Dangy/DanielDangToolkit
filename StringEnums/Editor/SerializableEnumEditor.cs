using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(AnalyticsKeysClass))]
public class SerializableEnumEditor : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        SerializedProperty enumProperty = property.FindPropertyRelative("m_EnumValue");
        SerializedProperty enumStringProperty = property.FindPropertyRelative("m_EnumValueAsString");

        for(int nameIndex = 0; nameIndex < enumProperty.enumNames.Length; nameIndex++)
        {
            if (enumProperty.enumNames[nameIndex] == enumStringProperty.stringValue)
            {
                enumProperty.enumValueIndex = nameIndex;
                break;
            }
        }

        // Enum
        enumProperty.enumValueIndex = EditorGUI.Popup(position, enumProperty.enumValueIndex, enumProperty.enumNames);
        enumStringProperty.stringValue = enumProperty.enumNames[enumProperty.enumValueIndex];

        EditorGUI.EndProperty();
    }
}
