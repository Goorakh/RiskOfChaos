using RiskOfChaos.Content.AssetCollections;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Content
{
    static class Unlockables
    {
        [ContentInitializer]
        static void LoadContent(UnlockableDefAssetCollection unlockableDefs)
        {
            {
                UnlockableDef invincibleLemurianLogbook = ScriptableObject.CreateInstance<UnlockableDef>();
                invincibleLemurianLogbook.cachedName = "Logs.InvincibleLemurian";
                invincibleLemurianLogbook.nameToken = "UNLOCKABLE_LOG_INVINCIBLE_LEMURIAN";

                unlockableDefs.Add(invincibleLemurianLogbook);
            }

            {
                UnlockableDef invincibleLemurianElderLogbook = ScriptableObject.CreateInstance<UnlockableDef>();
                invincibleLemurianElderLogbook.cachedName = "Logs.InvincibleLemurianElder";
                invincibleLemurianElderLogbook.nameToken = "UNLOCKABLE_LOG_INVINCIBLE_LEMURIAN_ELDER";

                unlockableDefs.Add(invincibleLemurianElderLogbook);
            }
        }
    }
}
