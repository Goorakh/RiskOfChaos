using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("order_inventory", DefaultSelectionWeight = 0.2f, EffectWeightReductionPercentagePerActivation = 60f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class OrderInventory : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(m => ItemTierCatalog.allItemTierDefs.Any(itd => itd.canRestack && hasAtLeastXUniqueItemsInTier(m.inventory, itd.tier, 2)));
        }

        static bool hasAtLeastXUniqueItemsInTier(Inventory inventory, ItemTier itemTier, int count)
        {
            if (!inventory)
                return false;

            int totalCount = 0;
            foreach (ItemIndex item in inventory.itemAcquisitionOrder)
            {
                if (ItemCatalog.GetItemDef(item).tier == itemTier)
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
            PlayerUtils.GetAllPlayerBodies(false).TryDo(playerBody =>
            {
                CharacterMaster master = playerBody.master;
                if (!master)
                    return;

                Inventory inventory = master.inventory;
                if (!inventory)
                    return;

                inventory.ShrineRestackInventory(RNG);
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = playerBody,
                    baseToken = "SHRINE_RESTACK_USE_MESSAGE"
                });

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                {
                    origin = playerBody.footPosition,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = new Color(1f, 0.23f, 0.6337214f)
                }, true);
            }, FormatUtils.GetBestBodyName);
        }
    }
}
