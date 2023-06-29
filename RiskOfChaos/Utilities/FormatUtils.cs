using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class FormatUtils
    {
        public static string GetBestBodyName(CharacterBody body)
        {
            if (!body)
                return "null";

            return Util.GetBestBodyName(body.gameObject);
        }
    }
}
