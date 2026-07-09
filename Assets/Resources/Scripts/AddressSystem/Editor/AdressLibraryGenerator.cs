using UnityEngine;
using UnityEditor;

public class AddressLibraryGenerator
{
    [MenuItem("Tools/Generate Address Library")]
    public static void GenerateAddressLibrary()
    {
        AddressLibrary addressLibrary = Object.FindAnyObjectByType<AddressLibrary>();
        if (addressLibrary == null)
        {
            addressLibrary = new GameObject("AddressLibrary").AddComponent<AddressLibrary>();
        }
        else
        {
            addressLibrary.addressMap.Clear();
        }
        LocalAddressComponent[] validAddresses = Object.FindObjectsByType<LocalAddressComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var addressComponent in validAddresses)
        {
            addressLibrary.AddAddress(addressComponent.Address, addressComponent.gameObject);
        }

        EditorUtility.SetDirty(addressLibrary);
        AssetDatabase.SaveAssets();
        DevLogger.Log($"Address library generated with {addressLibrary.AddressCount} addresses.");
    }
}