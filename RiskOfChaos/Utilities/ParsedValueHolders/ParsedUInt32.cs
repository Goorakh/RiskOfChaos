using System.Globalization;

namespace RiskOfChaos.Utilities.ParsedValueHolders
{
    public class ParsedUInt32 : GenericParsedValue<int>
    {
        protected override int parseInput(string input)
        {
            return int.Parse(input, NumberStyles.Integer | NumberStyles.AllowThousands);
        }
    }
}
