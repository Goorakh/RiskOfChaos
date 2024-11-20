using HG;
using RoR2;
using System;
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
                int sourcePickupIndex = Array.IndexOf(pickupGroup, SourcePickup);
                if (sourcePickupIndex != -1)
                {
                    pickupGroup = ArrayUtils.Clone(pickupGroup);
                    ArrayUtils.ArrayRemoveAtAndResize(ref pickupGroup, sourcePickupIndex);
                }
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
