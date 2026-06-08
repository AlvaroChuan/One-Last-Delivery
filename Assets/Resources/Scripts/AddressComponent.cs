using UnityEngine;

public class AddressComponent : MonoBehaviour
{
    [SerializeField, DropdownString("StreetNames")] string _streetName;
    [SerializeField] int _number;
    public string StreetName => _streetName;
    public int Number => _number;
    public bool MatchesAddress(string streetName, int number)
    {
        return _streetName == streetName && _number == number;
    }
}