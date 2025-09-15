using RoR2;
using UnityEngine;

namespace RiskOfChaos.Content
{
    partial class RoCContent
    {
        partial class Unlockables
        {
            [ContentInitializer]
            static void LoadContent(ContentIntializerArgs args)
            {
                UnlockableDef invincibleLemurianLogbook;
                {
                    invincibleLemurianLogbook = ScriptableObject.CreateInstance<UnlockableDef>();
                    invincibleLemurianLogbook.cachedName = "Logs.InvincibleLemurian";
                    invincibleLemurianLogbook.nameToken = "UNLOCKABLE_LOG_INVINCIBLE_LEMURIAN";
                }

                UnlockableDef invincibleLemurianElderLogbook;
                {
                    invincibleLemurianElderLogbook = ScriptableObject.CreateInstance<UnlockableDef>();
                    invincibleLemurianElderLogbook.cachedName = "Logs.InvincibleLemurianElder";
                    invincibleLemurianElderLogbook.nameToken = "UNLOCKABLE_LOG_INVINCIBLE_LEMURIAN_ELDER";
                }

                args.ContentPack.unlockableDefs.Add([
                    invincibleLemurianLogbook,
                    invincibleLemurianElderLogbook,
                ]);
            }
        }
    }
}
