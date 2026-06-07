using UnityEngine;

[System.Serializable]
public struct Address
{

    [DropdownString("StreetNames")] public string streetName;
    public int number;
}