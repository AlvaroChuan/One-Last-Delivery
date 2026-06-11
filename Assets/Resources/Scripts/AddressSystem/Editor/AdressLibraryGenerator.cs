using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor.VersionControl;

public class AddressLibraryGenerator
{
    [MenuItem("Tools/Generate Address Library")]
    public static void GenerateAddressLibrary()
    {
        string assetPath = AddressLibrary.GetPath();
        AddressLibrary existingLibrary = AssetDatabase.LoadAssetAtPath<AddressLibrary>(assetPath);
        if (existingLibrary != null)
        {
            if (EditorUtility.DisplayDialog("Address Library Exists", "An Address Library already exists at the path: " + assetPath + ". Do you want to overwrite it?", "Yes", "No"))
            {
                AssetDatabase.DeleteAsset(assetPath);
                DevLogger.Log("Existing Address Library deleted.");
            }
            else
            {
                DevLogger.Log("Address Library generation cancelled by user.");
                return;
            }
        }
        AddressLibrary addressLibrary = ScriptableObject.CreateInstance<AddressLibrary>();
        LocalAddressComponent[] validAddresses = Object.FindObjectsByType<LocalAddressComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var addressComponent in validAddresses)
        {
            addressLibrary.AddAddress(addressComponent.Address);
        }
        AssetDatabase.CreateAsset(addressLibrary, assetPath);
        EditorUtility.SetDirty(addressLibrary);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        DevLogger.Log($"Address library generated with {addressLibrary.AddressCount} addresses at {assetPath}");
    }
}