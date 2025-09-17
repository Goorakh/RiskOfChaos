namespace RiskOfChaos.PatcherInterop
{
    internal static class InteropUtils
    {
        public static float? DecodePackedOverrideValue(float overridePlusOne)
        {
            float overrideValue = overridePlusOne - 1f;
            if (overrideValue < 0f)
                return null;

            return overrideValue;
        }

        public static float EncodePackedOverrideValue(float? overrideValue)
        {
            if (!overrideValue.HasValue)
                return 0f;

            return overrideValue.Value + 1f;
        }
    }
}
