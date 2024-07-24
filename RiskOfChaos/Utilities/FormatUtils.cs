using RoR2;
using UnityEngine;

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

            if (!string.IsNullOrWhiteSpace(item.nameToken) && !Language.IsTokenInvalid(item.nameToken))
            {
                string displayName = Language.GetString(item.nameToken);
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }
            }

            return item.name;
        }

        public static string FormatTimeSeconds(float seconds)
        {
            if (seconds < (2f * 60f) + 0.5f)
            {
                return seconds.ToString(seconds >= 10f - 0.05f ? "F0" : "F1") + "s";
            }
            else
            {
                int minutesRemaining = Mathf.FloorToInt(seconds / 60f);
                int secondsRemaining = Mathf.RoundToInt(seconds % 60f);

                return $"{minutesRemaining}:{secondsRemaining:D2}";
            }
        }
    }
}
