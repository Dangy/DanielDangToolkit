using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(AnalyticsKeysClassAsMask))]
public class SerializableEnumMaskEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        SerializedProperty enumProperty = property.FindPropertyRelative("m_EnumValue");
        SerializedProperty enumStringListProperty = property.FindPropertyRelative("m_EnumMaskValuesAsStrings");

        int maskValue = 0;
        for (int arrayIndex = 0; arrayIndex < enumStringListProperty.arraySize; arrayIndex++)
        {
            for (int nameIndex = 0; nameIndex < enumProperty.enumNames.Length; nameIndex++)
            {
                if(enumProperty.enumNames[nameIndex] == enumStringListProperty.GetArrayElementAtIndex(arrayIndex).stringValue)
                {
                    maskValue |= 1 << (nameIndex);
                }
            }
        }
        maskValue = EditorGUI.MaskField(position, maskValue, enumProperty.enumNames);

        enumStringListProperty.ClearArray();
        // Enum
        enumProperty.intValue = maskValue;
        int currentIndex = 0;
        for (int nameIndex = 0; nameIndex < enumProperty.enumNames.Length; nameIndex++)
        {
            if((maskValue & 1 << (nameIndex)) != 0)
            {
                enumStringListProperty.InsertArrayElementAtIndex(currentIndex);
                enumStringListProperty.GetArrayElementAtIndex(currentIndex).stringValue = enumProperty.enumNames[nameIndex];
                currentIndex++;
            }
        }

        EditorGUI.EndProperty();
    }
}
