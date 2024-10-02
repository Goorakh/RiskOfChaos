using RiskOfChaos.EffectHandling;
using RoR2;
using System;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class RunExtensions
    {
        public static float GetRunTime(this Run run, RunTimerType timerType)
        {
            return timerType switch
            {
                RunTimerType.Stopwatch => run.GetRunStopwatch(),
                RunTimerType.Realtime => run.fixedTime,
                _ => throw new NotImplementedException($"Timer type {timerType} is not implemented"),
            };
        }

        public static bool IsPickupEnabled(this Run run, PickupIndex pickupIndex)
        {
            if (!pickupIndex.isValid)
                return false;

            if (!run.IsPickupAvailable(pickupIndex))
                return false;

            return run.availableTier1DropList.Contains(pickupIndex) ||
                   run.availableTier2DropList.Contains(pickupIndex) ||
                   run.availableTier3DropList.Contains(pickupIndex) ||
                   run.availableEquipmentDropList.Contains(pickupIndex) ||
                   run.availableLunarCombinedDropList.Contains(pickupIndex) ||
                   run.availableBossDropList.Contains(pickupIndex) ||
                   run.availableVoidTier1DropList.Contains(pickupIndex) ||
                   run.availableVoidTier2DropList.Contains(pickupIndex) ||
                   run.availableVoidTier3DropList.Contains(pickupIndex) ||
                   run.availableVoidBossDropList.Contains(pickupIndex);
        }

        public static bool IsItemEnabled(this Run run, ItemIndex itemIndex)
        {
            return itemIndex != ItemIndex.None && run.IsPickupEnabled(PickupCatalog.FindPickupIndex(itemIndex));
        }

        public static bool IsEquipmentEnabled(this Run run, EquipmentIndex equipmentIndex)
        {
            return equipmentIndex != EquipmentIndex.None && run.IsPickupEnabled(PickupCatalog.FindPickupIndex(equipmentIndex));
        }
    }
}
