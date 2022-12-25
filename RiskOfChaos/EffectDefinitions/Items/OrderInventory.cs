using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Items
{
    [ChaosEffect("OrderInventory", DefaultSelectionWeight = 0.2f, EffectRepetitionWeightExponent = 100f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public class OrderInventory : BaseEffect
    {
        public override void OnStart()
        {
            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(false))
            {
                CharacterMaster master = playerBody.master;
                if (master)
                {
                    Inventory inventory = master.inventory;
                    if (inventory)
                    {
                        inventory.ShrineRestackInventory(RNG);
                        Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                        {
                            subjectAsCharacterBody = playerBody,
                            baseToken = "SHRINE_RESTACK_USE_MESSAGE"
                        });
                    }
                }

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                {
                    origin = playerBody.footPosition,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = new Color(1f, 0.23f, 0.6337214f)
                }, true);
            }
        }
    }
}
