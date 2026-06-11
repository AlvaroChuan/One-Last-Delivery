public interface IAddress
{
    AddressInfo Address { get; }
    bool MatchesAddress(AddressInfo address);
}