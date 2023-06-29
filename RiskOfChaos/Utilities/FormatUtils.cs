using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class FormatUtils
    {
        public static string GetBestBodyName(CharacterBody body)
        {
            if (!body)
                return "null";

            string bodyName = Util.GetBestBodyName(body.gameObject);
            if (!string.IsNullOrWhiteSpace(bodyName))
            {
                return bodyName;
            }
            else
            {
                return body.ToString();
            }
        }
    }
}
