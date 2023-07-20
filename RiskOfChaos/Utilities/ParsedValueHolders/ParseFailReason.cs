using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Utilities.ParsedValueHolders
{
    public record class ParseFailReason(string ParseInput, Exception ParseException);
}
