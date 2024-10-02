using System;

namespace RiskOfChaos.Utilities.ParsedValueHolders
{
    public sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {
        }
    }
}
