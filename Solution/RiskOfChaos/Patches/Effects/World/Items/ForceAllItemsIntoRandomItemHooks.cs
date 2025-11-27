using RiskOfChaos.EffectDefinitions.World.Items;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Patches.Effects.World.Items
{
    static class ForceAllItemsIntoRandomItemHooks
    {
        static UniquePickup currentOverridePickup
        {
            get
            {
                UniquePickup pickupIndex = UniquePickup.none;

                if (ChaosEffectTracker.Instance)
                {
                    ChaosEffectComponent forceAllItemsEffectComponent = ChaosEffectTracker.Instance.GetFirstActiveTimedEffect(ForceAllItemsIntoRandomItem.EffectInfo);
                    if (forceAllItemsEffectComponent && forceAllItemsEffectComponent.TryGetComponent(out ForceAllItemsIntoRandomItem forceAllItemsEffectController))
                    {
                        pickupIndex = forceAllItemsEffectController.OverridePickup;
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

            On.RoR2.PickupDropTable.GeneratePickup += PickupDropTable_GeneratePickup;
            On.RoR2.PickupDropTable.GenerateDistinctPickups += PickupDropTable_GenerateDistinctPickups;

            On.RoR2.ChestBehavior.PickFromList += ChestBehavior_PickFromList;

            On.RoR2.PickupPickerController.GetOptionsFromPickupState += PickupPickerController_GetOptionsFromPickupState;
        }

        static UniquePickup PickupDropTable_GeneratePickup(On.RoR2.PickupDropTable.orig_GeneratePickup orig, PickupDropTable self, Xoroshiro128Plus rng)
        {
            UniquePickup originalPickup = orig(self, rng);
            UniquePickup overridePickup = currentOverridePickup;
            return overridePickup.isValid ? overridePickup : originalPickup;
        }

        static void PickupDropTable_GenerateDistinctPickups(On.RoR2.PickupDropTable.orig_GenerateDistinctPickups orig, PickupDropTable self, List<UniquePickup> dest, int desiredCount, Xoroshiro128Plus rng, bool allowLoop)
        {
            orig(self, dest, desiredCount, rng, allowLoop);

            UniquePickup overridePickup = currentOverridePickup;
            if (overridePickup.isValid)
            {
                for (int i = 0; i < dest.Count; i++)
                {
                    dest[i] = overridePickup;
                }
            }
        }

        static PickupIndex PickupDropTable_GenerateDrop(On.RoR2.PickupDropTable.orig_GenerateDrop orig, PickupDropTable self, Xoroshiro128Plus rng)
        {
            PickupIndex originalDrop = orig(self, rng);
            PickupIndex overridePickup = currentOverridePickup.pickupIndex;
            return overridePickup.isValid ? overridePickup : originalDrop;
        }

        static PickupIndex[] PickupDropTable_GenerateUniqueDrops(On.RoR2.PickupDropTable.orig_GenerateUniqueDrops orig, PickupDropTable self, int maxDrops, Xoroshiro128Plus rng)
        {
            PickupIndex[] result = orig(self, maxDrops, rng);

            PickupIndex overridePickup = currentOverridePickup.pickupIndex;
            if (overridePickup.isValid)
            {
                Array.Fill(result, overridePickup);
            }

            return result;
        }

        static void ChestBehavior_PickFromList(On.RoR2.ChestBehavior.orig_PickFromList orig, ChestBehavior self, List<PickupIndex> dropList)
        {
            PickupIndex overridePickup = currentOverridePickup.pickupIndex;
            if (overridePickup.isValid)
            {
                dropList.Clear();
                dropList.Add(overridePickup);
            }

            orig(self, dropList);
        }

        static PickupPickerController.Option[] PickupPickerController_GetOptionsFromPickupState(On.RoR2.PickupPickerController.orig_GetOptionsFromPickupState orig, UniquePickup pickup)
        {
            PickupPickerController.Option[] options = orig(pickup);

            UniquePickup overridePickup = currentOverridePickup;
            if (overridePickup.isValid)
            {
                if (pickup == overridePickup)
                {
                    Array.Fill(options, new PickupPickerController.Option
                    {
                        pickup = overridePickup,
                        available = true
                    });
                }
            }

            return options;
        }
    }
}
