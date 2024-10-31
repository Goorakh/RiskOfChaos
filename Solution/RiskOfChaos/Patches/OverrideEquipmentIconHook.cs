using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.UI;
using System.Collections;
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
        public static event OverrideEquipmentIconDelegate OverrideEquipmentIcon;

        [SystemInitializer]
        static IEnumerator Init()
        {
            MethodInfo EquipmentIcon_SetDisplayData_MI = SymbolExtensions.GetMethodInfo<EquipmentIcon>(_ => _.SetDisplayData(default));

            if (EquipmentIcon_SetDisplayData_MI != null)
            {
                new Hook(EquipmentIcon_SetDisplayData_MI, EquipmentIcon_SetDisplayData);
            }
            else
            {
                Log.Error("Could not find EquipmentIcon.SetDisplayData MethodInfo");
            }

            yield return null;
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

                OverrideEquipmentIcon?.Invoke(displayData, ref iconOverride);

                self.iconImage.texture = iconOverride.IconOverride;
                self.iconImage.uvRect = iconOverride.IconRectOverride;
                self.iconImage.color = iconOverride.IconColorOverride;
            }
        }
    }
}
