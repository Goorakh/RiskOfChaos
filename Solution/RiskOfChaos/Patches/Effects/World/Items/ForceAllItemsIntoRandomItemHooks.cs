using HG;
using RiskOfChaos.EffectDefinitions.World.Items;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.Patches.Effects.World.Items
{
    static class ForceAllItemsIntoRandomItemHooks
    {
        static PickupIndex currentOverridePickup
        {
            get
            {
                PickupIndex pickupIndex = PickupIndex.none;

                if (ChaosEffectTracker.Instance)
                {
                    ChaosEffectComponent forceAllItemsEffectComponent = ChaosEffectTracker.Instance.GetFirstActiveTimedEffect(ForceAllItemsIntoRandomItem.EffectInfo);
                    if (forceAllItemsEffectComponent && forceAllItemsEffectComponent.TryGetComponent(out ForceAllItemsIntoRandomItem forceAllItemsEffectController))
                    {
                        pickupIndex = forceAllItemsEffectController.OverridePickupIndex;
                    }
                }

                return pickupIndex;
            }
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupDropTable.GenerateDrop += PickupDropTable_GenerateDrop;
            On.RoR2.PickupDropTable.GenerateUniqueDrops += PickupDropTable_GenerateUniqueDrops;

            On.RoR2.ChestBehavior.PickFromList += ChestBehavior_PickFromList;

            On.RoR2.PickupPickerController.GetOptionsFromPickupIndex += PickupPickerController_GetOptionsFromPickupIndex;
        }

        static PickupIndex PickupDropTable_GenerateDrop(On.RoR2.PickupDropTable.orig_GenerateDrop orig, PickupDropTable self, Xoroshiro128Plus rng)
        {
            PickupIndex originalDrop = orig(self, rng);
            PickupIndex overridePickup = currentOverridePickup;
            return overridePickup.isValid ? overridePickup : originalDrop;
        }

        static PickupIndex[] PickupDropTable_GenerateUniqueDrops(On.RoR2.PickupDropTable.orig_GenerateUniqueDrops orig, PickupDropTable self, int maxDrops, Xoroshiro128Plus rng)
        {
            PickupIndex[] result = orig(self, maxDrops, rng);

            PickupIndex overridePickup = currentOverridePickup;
            if (overridePickup.isValid)
            {
                ArrayUtils.SetAll(result, overridePickup);
            }

            return result;
        }

        static void ChestBehavior_PickFromList(On.RoR2.ChestBehavior.orig_PickFromList orig, ChestBehavior self, List<PickupIndex> dropList)
        {
            PickupIndex overridePickup = currentOverridePickup;
            if (overridePickup.isValid)
            {
                dropList.Clear();
                dropList.Add(overridePickup);
            }

            orig(self, dropList);
        }

        static PickupPickerController.Option[] PickupPickerController_GetOptionsFromPickupIndex(On.RoR2.PickupPickerController.orig_GetOptionsFromPickupIndex orig, PickupIndex pickupIndex)
        {
            PickupPickerController.Option[] options = orig(pickupIndex);

            PickupIndex overridePickup = currentOverridePickup;
            if (overridePickup.isValid)
            {
                if (pickupIndex == overridePickup)
                {
                    ArrayUtils.SetAll(options, new PickupPickerController.Option
                    {
                        pickupIndex = overridePickup,
                        available = true
                    });
                }
            }

            return options;
        }
    }
}
