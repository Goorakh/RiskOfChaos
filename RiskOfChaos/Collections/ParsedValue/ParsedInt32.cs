﻿using System.Globalization;
using RiskOfChaos.Utilities.ParsedValueHolders;

namespace RiskOfChaos.Collections.ParsedValue
{
    public class ParsedInt32 : GenericParsedValue<int>
    {
        protected override int parseInput(string input)
        {
            if (int.TryParse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }

            throw new ParseException($"\"{input}\" is not a valid integer");
        }
    }
}