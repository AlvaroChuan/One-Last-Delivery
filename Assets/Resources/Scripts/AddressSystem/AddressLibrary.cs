using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AddressLibrary", menuName = "ScriptableObjects/AddressLibrary", order = 1)]
public class AddressLibrary : ScriptableObject
{
    public int AddressCount => _addresses.Count;
    [SerializeField] private List<AddressInfo> _addresses = new List<AddressInfo>();

    public static string GetPath()
    {
        return "Assets/Resources/ScriptableObjects/AddressLibrary" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ".asset";
    }
    public static string GetResourcePath()
    {
        return "ScriptableObjects/AddressLibrary" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    public AddressInfo GetRandomAddress()
    {
        if (_addresses.Count == 0)
        {
            DevLogger.LogError("No addresses available in the library. Please generate addresses before attempting to retrieve one.");
            return new AddressInfo(); // Return an empty address info to avoid null reference exceptions
        }
        return _addresses[Random.Range(0, _addresses.Count)];
    }
    public void AddAddress(AddressInfo newAddress)
    {
        if (!_addresses.Contains(newAddress))
        {
            _addresses.Add(newAddress);
        }
    }
}