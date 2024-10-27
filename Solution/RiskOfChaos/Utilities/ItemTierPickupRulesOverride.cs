using HG;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities
{
    public class ItemTierPickupRulesOverride : IDisposable
    {
        static ItemTierDef.PickupRules[] _originalPickupRules = [];

        [SystemInitializer(typeof(ItemTierCatalog))]
        static void Init()
        {
            ReadOnlyArray<ItemTierDef> itemTierDefs = ItemTierCatalog.allItemTierDefs;
            _originalPickupRules = new ItemTierDef.PickupRules[itemTierDefs.Length];

            for (int i = 0; i < itemTierDefs.Length; i++)
            {
                _originalPickupRules[i] = itemTierDefs[i].pickupRules;
            }
        }

        static readonly List<ItemTierPickupRulesOverride> _activeRuleOverrides = [];

        static void refreshPickupRules()
        {
            if (_activeRuleOverrides.Count > 0)
            {
                setOverridePickupRules(_activeRuleOverrides[^1].OverrideRules);
            }
            else
            {
                restorePickupRules();
            }
        }

        static void setOverridePickupRules(ItemTierDef.PickupRules overridePickupRules)
        {
            for (int i = 0; i < ItemTierCatalog.allItemTierDefs.Length; i++)
            {
                ItemTierCatalog.allItemTierDefs[i].pickupRules = overridePickupRules;
            }

#if DEBUG
            Log.Debug($"Set item pickup rules override: {overridePickupRules}");
#endif
        }

        static void restorePickupRules()
        {
            if (_originalPickupRules.Length == 0)
            {
                Log.Error("Original rules not initialized, cannot restore pickup rules");
                return;
            }

            for (int i = 0; i < ItemTierCatalog.allItemTierDefs.Length; i++)
            {
                if (i >= _originalPickupRules.Length)
                {
                    Log.Error($"Missing original pickup rules for {ItemTierCatalog.GetItemTierDef((ItemTier)i).name}");
                    continue;
                }

                ItemTierCatalog.allItemTierDefs[i].pickupRules = _originalPickupRules[i];
            }

#if DEBUG
            Log.Debug("Restored item pickup rules");
#endif
        }

        ItemTierDef.PickupRules _overrideRules;

        public ItemTierDef.PickupRules OverrideRules
        {
            get
            {
                return _overrideRules;
            }
            set
            {
                if (_overrideRules == value)
                    return;

                _overrideRules = value;
                refreshPickupRules();
            }
        }

        public ItemTierPickupRulesOverride(ItemTierDef.PickupRules pickupRulesOverride)
        {
            _overrideRules = pickupRulesOverride;
            _activeRuleOverrides.Add(this);
            refreshPickupRules();
        }

        public void Dispose()
        {
            if (_activeRuleOverrides.Remove(this))
            {
                refreshPickupRules();
            }
        }
    }
}
