using RiskOfChaos.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ForceModelOverlayPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays;
        }

        static void CharacterModel_UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig(self);

            foreach (Material overlayMaterial in self.GetComponents<ForceModelOverlay>().Select(f => f.Overlay).Distinct())
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                if (self.activeOverlayCount >= CharacterModel.maxOverlays)
                    break;

                self.currentOverlays[self.activeOverlayCount++] = overlayMaterial;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
