using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("kill_all", DefaultSelectionWeight = 0.7f)]
    public sealed class KillAll : MonoBehaviour
    {
        void Start()
        {
            if (!NetworkServer.active)
                return;
            
            bool sendInvincibleLemurianMessage = false;

            for (int i = CharacterBody.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterBody body = CharacterBody.readOnlyInstancesList[i];
                HealthComponent healthComponent = body.healthComponent;
                CharacterMaster master = body.master;
                if (!healthComponent ||
                    body.isPlayerControlled ||
                    body.teamComponent.teamIndex == TeamIndex.Player ||
                    body.teamComponent.teamIndex == TeamIndex.None ||
                    (master && master.isBoss))
                {
                    continue;
                }

                Inventory inventory = body.inventory;
                if (inventory)
                {
                    if (inventory.GetItemCount(RoCContent.Items.InvincibleLemurianMarker) > 0)
                    {
                        sendInvincibleLemurianMessage = true;
                        continue;
                    }
                }

                if (master)
                {
                    MinionOwnership minionOwnership = master.minionOwnership;
                    if (minionOwnership)
                    {
                        CharacterMaster ownerMaster = minionOwnership.ownerMaster;
                        if (ownerMaster && ownerMaster.playerCharacterMasterController)
                            continue;
                    }
                }

                healthComponent.Suicide();
            }

            if (sendInvincibleLemurianMessage)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = "INVINCIBLE_LEMURIAN_KILL_FAIL_MESSAGE"
                });
            }
        }
    }
}
