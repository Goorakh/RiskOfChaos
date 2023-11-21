using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2.UI;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.Patches
{
    static class OverrideEquipmentIconHook
    {
        public readonly struct IconOverrideInfo
        {
            public readonly RawImage IconImage;
            public readonly TextMeshProUGUI CooldownText;
            public readonly TextMeshProUGUI StockText;

            public IconOverrideInfo(EquipmentIcon icon)
            {
                IconImage = icon.iconImage;
                CooldownText = icon.cooldownText;
                StockText = icon.stockText;
            }
        }

        public delegate void OverrideEquipmentIconDelegate(in EquipmentIcon.DisplayData displayData, in IconOverrideInfo info);

        static event OverrideEquipmentIconDelegate _overrideEquipmentIcon;
        public static event OverrideEquipmentIconDelegate OverrideEquipmentIcon
        {
            add
            {
                _overrideEquipmentIcon += value;
                tryApplyPatches();
            }
            remove
            {
                _overrideEquipmentIcon -= value;
            }
        }

        static bool _hasAppliedPatches;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            MethodInfo EquipmentIcon_SetDisplayData_MI = SymbolExtensions.GetMethodInfo<EquipmentIcon>(_ => _.SetDisplayData(default));
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            if (EquipmentIcon_SetDisplayData_MI != null)
            {
                new Hook(EquipmentIcon_SetDisplayData_MI, EquipmentIcon_SetDisplayData);
            }
            else
            {
                Log.Error("Could not find EquipmentIcon.SetDisplayData MethodInfo");
            }

            _hasAppliedPatches = true;
        }

        static Rect? _defaultEquipmentIconRect;

        delegate void orig_SetDisplayData(EquipmentIcon self, EquipmentIcon.DisplayData displayData);
        static void EquipmentIcon_SetDisplayData(orig_SetDisplayData orig, EquipmentIcon self, EquipmentIcon.DisplayData displayData)
        {
            orig(self, displayData);

            // Bad way of doing it, don't really care, odds of this causing a conflict is super low anyway
            if (self.iconImage)
            {
                if (!_defaultEquipmentIconRect.HasValue)
                {
                    _defaultEquipmentIconRect = self.iconImage.uvRect;
                }
                else
                {
                    self.iconImage.uvRect = _defaultEquipmentIconRect.Value;
                }
            }

            IconOverrideInfo iconOverride = new IconOverrideInfo(self);
            _overrideEquipmentIcon?.Invoke(displayData, iconOverride);
        }
    }
}
