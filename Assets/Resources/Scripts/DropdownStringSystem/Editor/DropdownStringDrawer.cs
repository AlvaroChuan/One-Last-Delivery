using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;

[CustomPropertyDrawer(typeof(DropdownStringAttribute))]
public class DropdownStringDrawer : PropertyDrawer
{
    DropDownStringDatabase _database;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Safety check: Ensure this attribute is only placed on string types
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [DropdownString] on string fields only!");
            return;
        }

        // Get the key provided inside the attribute parameter tag e.g. [DropdownString("KeyName")]
        DropdownStringAttribute dropdownAttribute = (DropdownStringAttribute)attribute;
        string currentKey = dropdownAttribute.Key;

        EditorGUI.BeginProperty(position, label, property);

        float buttonWidth = 18f;
        Rect fieldRect = new Rect(position.x, position.y, position.width - buttonWidth * 2, position.height);
        Rect dropdownButtonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);
        Rect saveButtonRect = new Rect(position.x + position.width - buttonWidth * 2, position.y, buttonWidth, position.height);

        string textFieldControlName = $"DropdownStringField_{property.propertyPath}";
        GUI.SetNextControlName(textFieldControlName);

        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
        {
            if (GUI.GetNameOfFocusedControl() == textFieldControlName)
            {
                if (!string.IsNullOrWhiteSpace(property.stringValue))
                {
                    AddOptionToDatabase(currentKey, property.stringValue);

                    GUI.FocusControl(null);
                    currentEvent.Use();
                }
            }
        }

        // Draw the standard Text Field (this automatically saves changes cleanly!)
        EditorGUI.BeginChangeCheck();
        string newValue = EditorGUI.TextField(fieldRect, label, property.stringValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = newValue;
            property.serializedObject.ApplyModifiedProperties();
        }

        // Draw the Dropdown Button
        if (GUI.Button(dropdownButtonRect, "▼", EditorStyles.miniButtonRight))
        {
            GenericMenu valueMenu = new GenericMenu();

            if (_database == null)
            {
                _database = Resources.Load<DropDownStringDatabase>("ScriptableObjects/DropDownStringDatabase");
                if (_database == null)
                {
                    // Create a new database if it doesn't exist
                    _database = ScriptableObject.CreateInstance<DropDownStringDatabase>();
                    AssetDatabase.CreateAsset(_database, "Assets/Resources/ScriptableObjects/DropDownStringDatabase.asset");
                    AssetDatabase.SaveAssets();
                }
            }

            List<string> options = _database.GetOptions(currentKey);

            if (options.Count == 0)
            {
                valueMenu.AddDisabledItem(new GUIContent($"(No choices for '{currentKey}')"));
            }
            else
            {
                foreach (string option in options)
                {
                    string localOption = option;
                    valueMenu.AddItem(new GUIContent(localOption), property.stringValue == localOption, () =>
                    {
                        property.serializedObject.Update();
                        property.stringValue = localOption;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
            }
            valueMenu.DropDown(dropdownButtonRect);
        }

        //Draw the save inputted value button
        if (GUI.Button(saveButtonRect, "S", EditorStyles.miniButtonLeft))
        {
            AddOptionToDatabase(currentKey, property.stringValue);
        }

        EditorGUI.EndProperty();
    }

    void AddOptionToDatabase(string key, string option)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(option))
        {
            Debug.LogWarning("Key and option cannot be empty.");
            return;
        }

        if (_database == null)
        {
            _database = Resources.Load<DropDownStringDatabase>("ScriptableObjects/DropDownStringDatabase");
            if (_database == null)
            {
                // Create a new database if it doesn't exist
                _database = ScriptableObject.CreateInstance<DropDownStringDatabase>();
                AssetDatabase.CreateAsset(_database, "Assets/Resources/ScriptableObjects/DropDownStringDatabase.asset");
                AssetDatabase.SaveAssets();
            }
        }

        _database.AddOption(key, option);
        EditorUtility.SetDirty(_database);
        AssetDatabase.SaveAssets();
    }
}