namespace RiskOfChaos.Utilities.ParsedValueHolders
{
    public sealed record class ParseFailReason(string ParseInput, ParseException ParseException);
}
