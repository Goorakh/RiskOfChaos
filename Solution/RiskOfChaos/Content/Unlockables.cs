using RoR2;
using RoR2.ContentManagement;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Content
{
    public static class Unlockables
    {
        public static readonly UnlockableDef InvincibleLemurianLogbook;

        public static readonly UnlockableDef InvincibleLemurianElderLogbook;

        static Unlockables()
        {
            {
                InvincibleLemurianLogbook = ScriptableObject.CreateInstance<UnlockableDef>();
                InvincibleLemurianLogbook.cachedName = "Logs.InvincibleLemurian";
                InvincibleLemurianLogbook.nameToken = "UNLOCKABLE_LOG_INVINCIBLE_LEMURIAN";
            }

            {
                InvincibleLemurianElderLogbook = ScriptableObject.CreateInstance<UnlockableDef>();
                InvincibleLemurianElderLogbook.cachedName = "Logs.InvincibleLemurianElder";
                InvincibleLemurianElderLogbook.nameToken = "UNLOCKABLE_LOG_INVINCIBLE_LEMURIAN_ELDER";
            }
        }

        internal static void AddUnlockablesTo(NamedAssetCollection<UnlockableDef> unlockableDefs)
        {
            unlockableDefs.Add([
                InvincibleLemurianLogbook,
                InvincibleLemurianElderLogbook
            ]);
        }
    }
}
