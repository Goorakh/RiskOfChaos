using RiskOfChaos.Components;
using RoR2;
using System.Linq;
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
                if (self.activeOverlayCount >= CharacterModel.maxOverlays)
                    break;

                self.currentOverlays[self.activeOverlayCount++] = overlayMaterial;
            }
        }
    }
}
