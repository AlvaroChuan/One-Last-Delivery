[System.Serializable]
public struct AddressInfo
{
    [DropdownString("StreetNames")] public string streetName;
    public int number;

    public static bool operator ==(AddressInfo a, AddressInfo b)
    {
        return a.streetName == b.streetName && a.number == b.number;
    }

    public static bool operator !=(AddressInfo a, AddressInfo b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj is AddressInfo other)
        {
            return this == other;
        }
        return false;
    }
}