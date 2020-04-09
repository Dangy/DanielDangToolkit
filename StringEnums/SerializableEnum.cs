using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class SerializableEnum<T> where T : struct, IConvertible
{
    public T Value
    {
        get { return m_EnumValue; }
        set { m_EnumValue = value; }
    }

    public string ValueAsString
    {
        get { return m_EnumValueAsString; }
    }

    [SerializeField]
    private string m_EnumValueAsString;
    [SerializeField]
    private T m_EnumValue;
}

//  Example Use:

//[Serializable]
//public class AnalyticsKeysClass : SerializableEnum<AnalyticsKeys> { }

//[Serializable]
//public class AnalyticsKeysClassAsMask : SerializableEnum<AnalyticsKeys> { }

