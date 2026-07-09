using System;
using System.Collections.Generic;
using UnityEngine;

public class AddressLibrary : MonoBehaviour
{
    [Serializable]
    public struct AddressDoorPair
    {
        public AddressInfo address;
        public GameObject door;
        public AddressDoorPair(AddressInfo address, GameObject door)
        {
            this.address = address;
            this.door = door;
        }
    }
    public int AddressCount => addressMap.Count;
    public List<AddressDoorPair> addressMap = new List<AddressDoorPair>();

    [ContextMenu("print doors")]
    private void PrintDoors()
    {
        DevLogger.Log($"Printing registered doors: {addressMap.Count}");
        foreach (var pair in addressMap)
        {
            DevLogger.Log($"Address: {pair.address}, Door: {pair.door}");
        }
    }

    public AddressInfo GetRandomAddress()
    {
        if (addressMap.Count == 0)
        {
            DevLogger.LogError("No addresses available in the library. Please generate addresses before attempting to retrieve one.");
            return new AddressInfo(); // Return an empty address info to avoid null reference exceptions
        }
        return addressMap[UnityEngine.Random.Range(0, addressMap.Count)].address;
    }

    public void AddAddress(AddressInfo newAddress, GameObject door)
    {
        if (!addressMap.Exists(pair => pair.address == newAddress))
        {
            addressMap.Add(new AddressDoorPair(newAddress, door));
        }
        else
        {
            DevLogger.LogWarning($"Address {newAddress} already exists in the library. Skipping addition.");
        }
    }

    public GameObject GetDoorForAddress(AddressInfo address)
    {
        var pair = addressMap.Find(p => p.address == address);
        if (pair.door != null)
        {
            return pair.door;
        }
        return null; // Return null if no door is associated with the address
    }

    public void ClearRegistry()
    {
        addressMap.Clear();
    }
}