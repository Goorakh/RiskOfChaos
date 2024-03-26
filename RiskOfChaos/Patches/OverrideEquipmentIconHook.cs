using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2.UI;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class OverrideEquipmentIconHook
    {
        public struct IconOverrideInfo
        {
            public Texture IconOverride;
            public Rect IconRectOverride;
            public Color IconColorOverride;
        }

        public delegate void OverrideEquipmentIconDelegate(in EquipmentIcon.DisplayData displayData, ref IconOverrideInfo info);

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

            IconOverrideInfo iconOverride = new IconOverrideInfo();

            if (self.iconImage)
            {
                if (!_defaultEquipmentIconRect.HasValue)
                {
                    _defaultEquipmentIconRect = self.iconImage.uvRect;
                }

                iconOverride.IconOverride = self.iconImage.texture;
                iconOverride.IconRectOverride = _defaultEquipmentIconRect.Value;
                iconOverride.IconColorOverride = self.iconImage.color;

                _overrideEquipmentIcon?.Invoke(displayData, ref iconOverride);

                self.iconImage.texture = iconOverride.IconOverride;
                self.iconImage.uvRect = iconOverride.IconRectOverride;
                self.iconImage.color = iconOverride.IconColorOverride;
            }
        }
    }
}
