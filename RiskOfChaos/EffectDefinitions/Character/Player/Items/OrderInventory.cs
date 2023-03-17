using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("OrderInventory", DefaultSelectionWeight = 0.2f, EffectWeightReductionPercentagePerActivation = 60f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public class OrderInventory : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return PlayerUtils.GetAllPlayerMasters(false).Any(m => ItemTierCatalog.allItemTierDefs.Any(itd => itd.canRestack && hasAtLeastXUniqueItemsInTier(m.inventory, itd.tier, 2)));
        }

        static bool hasAtLeastXUniqueItemsInTier(Inventory inventory, ItemTier itemTier, int count)
        {
            if (!inventory)
                return false;

            int totalCount = 0;
            foreach (ItemDef item in ItemCatalog.allItemDefs)
            {
                if (inventory.GetItemCount(item) > 0 && item.tier == itemTier)
                {
                    if (++totalCount >= count)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

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
