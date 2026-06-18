using System.Collections.Generic;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DropdownStringAttribute : PropertyAttribute
{
    string _key;
    public string Key => _key;
    public DropdownStringAttribute(string key) { _key = key; }
}