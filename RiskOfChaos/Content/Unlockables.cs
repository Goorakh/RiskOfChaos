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

                Texture2D iconTexture = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Lemurian/LemurianBody.png").WaitForCompletion();
                Sprite iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);

                InvincibleLemurianLogbook.achievementIcon = iconSprite;
            }

            {
                InvincibleLemurianElderLogbook = ScriptableObject.CreateInstance<UnlockableDef>();
                InvincibleLemurianElderLogbook.cachedName = "Logs.InvincibleLemurianElder";
                InvincibleLemurianElderLogbook.nameToken = "UNLOCKABLE_LOG_INVINCIBLE_LEMURIAN_ELDER";

                Texture2D iconTexture = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/LemurianBruiser/LemurianBruiserBody.png").WaitForCompletion();
                Sprite iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);

                InvincibleLemurianElderLogbook.achievementIcon = iconSprite;
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
