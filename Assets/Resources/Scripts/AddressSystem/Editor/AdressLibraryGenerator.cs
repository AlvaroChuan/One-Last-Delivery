using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
        LocalAddressComponent[] validAddresses = Object.FindObjectsByType<LocalAddressComponent>(FindObjectsSortMode.None);

        foreach (var addressComponent in validAddresses)
        {
            addressLibrary.AddAddress(addressComponent.Address, addressComponent.gameObject);
        }

        if (!EditorApplication.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(addressLibrary.gameObject.scene);
        }
    }
}