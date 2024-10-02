using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;

namespace RiskOfChaos.Utilities
{
    public static class ItemTierPickupRulesOverride
    {
        static ItemTierDef.PickupRules? _overrideRules;
        public static ItemTierDef.PickupRules? OverrideRules
        {
            get
            {
                return _overrideRules;
            }
            set
            {
                bool hadOverride = _overrideRules.HasValue;
                _overrideRules = value;

                if (_overrideRules.HasValue)
                {
                    applyOverrides();
                }
                else
                {
                    if (hadOverride)
                    {
                        restoreOverrides();
                    }
                }
            }
        }

        record class ItemTierPickupRulesOverrideInfo(ItemTierDef TierDef, ItemTierDef.PickupRules OriginalPickupRules)
        {
            public void RestorePickupRules()
            {
                TierDef.pickupRules = OriginalPickupRules;
            }

            public void SetOverrideRules(ItemTierDef.PickupRules overrideRules)
            {
                TierDef.pickupRules = overrideRules;
            }
        }
        static ItemTierPickupRulesOverrideInfo[] _activeOverrides = [];

        [SystemInitializer(typeof(ItemTierCatalog))]
        static void Init()
        {
            int itemTierCount = ItemTierCatalog.itemCount;
            _activeOverrides = new ItemTierPickupRulesOverrideInfo[itemTierCount];

            ReadOnlyArray<ItemTierDef> tierDefs = ItemTierCatalog.allItemTierDefs;
            for (int i = 0; i < itemTierCount; i++)
            {
                ItemTierDef tierDef = tierDefs[i];
                _activeOverrides[i] = new ItemTierPickupRulesOverrideInfo(tierDef, tierDef.pickupRules);
            }
        }

        static void applyOverrides()
        {
            if (!_overrideRules.HasValue)
            {
                Log.Error("Cannot apply overrides. No override rules specified");
                return;
            }

            for (int i = 0; i < _activeOverrides.Length; i++)
            {
                _activeOverrides[i].SetOverrideRules(_overrideRules.Value);
            }

#if DEBUG
            Log.Debug($"Set item pickup rule override: {_overrideRules.Value}");
#endif
        }

        static void restoreOverrides()
        {
            _activeOverrides.TryDo(overrideInfo => overrideInfo.RestorePickupRules());

#if DEBUG
            Log.Debug($"Restored item pickup rule override");
#endif
        }
    }
}
