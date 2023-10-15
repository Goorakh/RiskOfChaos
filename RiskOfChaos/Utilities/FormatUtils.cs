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

        public static string GetBestItemDisplayName(ItemIndex item)
        {
            if (item == ItemIndex.None)
                return "None";

            return GetBestItemDisplayName(ItemCatalog.GetItemDef(item));
        }

        public static string GetBestItemDisplayName(ItemDef item)
        {
            if (!item)
                return "null";

            if (!string.IsNullOrWhiteSpace(item.nameToken))
            {
                string displayName = Language.GetString(item.nameToken);
                if (!string.IsNullOrWhiteSpace(displayName) && displayName != item.nameToken)
                {
                    return displayName;
                }
            }

            return item.name;
        }
    }
}
