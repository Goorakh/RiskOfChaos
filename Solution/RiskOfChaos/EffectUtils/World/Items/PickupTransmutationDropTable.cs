using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectUtils.World.Items
{
    public class PickupTransmutationDropTable : PickupDropTable
    {
        public PickupIndex SourcePickup = PickupIndex.none;

        PickupIndex[] _transmutationGroup = [];

        bool _transmutationGroupDirty;

        public override void OnEnable()
        {
            base.OnEnable();

            Run.onRunStartGlobal += onRunStartGlobal;
            Run.onAvailablePickupsModified += onAvailablePickupsModified;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            Run.onRunStartGlobal -= onRunStartGlobal;
            Run.onAvailablePickupsModified -= onAvailablePickupsModified;
        }

        void onRunStartGlobal(Run run)
        {
            _transmutationGroupDirty = true;
        }

        void onAvailablePickupsModified(Run run)
        {
            _transmutationGroupDirty = true;
        }

        void regenerateIfNeeded()
        {
            if (_transmutationGroupDirty)
            {
                Regenerate(Run.instance);
            }
        }

        public override void Regenerate(Run run)
        {
            base.Regenerate(run);

            Log.Debug($"Regenerating transmutation drop table: {name}");

            PickupIndex[] pickupGroup = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(SourcePickup) ?? [];
            if (pickupGroup.Length > 0)
            {
                List<PickupIndex> availablePickups = new List<PickupIndex>(pickupGroup.Length);

                foreach (PickupIndex pickupIndex in pickupGroup)
                {
                    if (pickupIndex != SourcePickup && run.IsPickupEnabled(pickupIndex))
                    {
                        availablePickups.Add(pickupIndex);
                    }
                }

                pickupGroup = [.. availablePickups];
            }

            _transmutationGroup = pickupGroup;
            _transmutationGroupDirty = false;
        }

        public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
        {
            regenerateIfNeeded();

            if (_transmutationGroup.Length == 0)
                return PickupIndex.none;

            return rng.NextElementUniform(_transmutationGroup);
        }

        public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
        {
            regenerateIfNeeded();

            if (_transmutationGroup.Length == 0)
                return [];

            PickupIndex[] modifiableTransmutationGroup = ArrayUtils.Clone(_transmutationGroup);
            Util.ShuffleArray(modifiableTransmutationGroup, rng);

            return modifiableTransmutationGroup[0..Mathf.Min(modifiableTransmutationGroup.Length, maxDrops)];
        }

        public override int GetPickupCount()
        {
            regenerateIfNeeded();

            return _transmutationGroup.Length;
        }
    }
}
