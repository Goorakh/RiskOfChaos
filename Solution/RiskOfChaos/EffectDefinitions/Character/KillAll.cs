using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("kill_all", DefaultSelectionWeight = 0.7f)]
    public sealed class KillAll : BaseEffect
    {
        public override void OnStart()
        {
            bool sentInvincibleLemurianMessage = false;

            for (int i = CharacterMaster.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                if (!master || master.isBoss || master.playerCharacterMasterController)
                    continue;

                if (master.inventory && master.inventory.GetItemCount(RoCContent.Items.InvincibleLemurianMarker) > 0)
                {
                    if (!sentInvincibleLemurianMessage)
                    {
                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                        {
                            baseToken = "INVINCIBLE_LEMURIAN_KILL_FAIL_MESSAGE"
                        });

                        sentInvincibleLemurianMessage = true;
                    }

                    continue;
                }

                CharacterBody body = master.GetBody();
                if (!body)
                    continue;

                try
                {
                    switch (body.teamComponent.teamIndex)
                    {
                        case TeamIndex.Neutral:
                        case TeamIndex.Monster:
                        case TeamIndex.Lunar:
                        case TeamIndex.Void:
                            HealthComponent healthComponent = body.healthComponent;
                            if (healthComponent)
                            {
                                healthComponent.Suicide();
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error_NoCallerPrefix($"Failed to kill {Util.GetBestMasterName(master)}: {ex}");
                }
            }
        }
    }
}
