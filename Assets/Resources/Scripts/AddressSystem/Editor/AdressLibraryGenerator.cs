using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AddressLibraryGenerator
{
    [MenuItem("Tools/Generate Address Library")]
    public static void GenerateAddressLibrary()
    {
        string assetPath = AddressLibrary.GetPath();
        AddressLibrary addressLibrary = ScriptableObject.CreateInstance<AddressLibrary>();
        AddressComponent[] validAddresses = Object.FindObjectsByType<AddressComponent>(FindObjectsSortMode.None);
        foreach (var addressComponent in validAddresses)
        {
            if (addressComponent.IsValidAddress)
            {
                addressLibrary.validAddresses.Add(addressComponent.Address);
            }
        }
        AssetDatabase.CreateAsset(addressLibrary, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Address library generated with {addressLibrary.validAddresses.Count} addresses at {assetPath}");
    }
}