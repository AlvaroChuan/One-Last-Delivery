using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AddressLibrary", menuName = "ScriptableObjects/AddressLibrary", order = 1)]
public class AddressLibrary : ScriptableObject
{
    public static string GetPath()
    {
        return "Assets/Resources/ScriptableObjects/AddressLibrary" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ".asset";
    }
    public List<AddressInfo> validAddresses = new List<AddressInfo>();
}