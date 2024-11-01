using RoR2;

namespace RiskOfChaos.Patches
{
    // Some elite aspects are implemented as item behaviors.
    // Changing buffs doesn't invoke the inventory changed event however,
    // so by default most elite aspects don't "activate" fully if just given the buff
    static class BuffItemBehaviorFix
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterBody.SetBuffCount += CharacterBody_SetBuffCount;
        }

        static void CharacterBody_SetBuffCount(On.RoR2.CharacterBody.orig_SetBuffCount orig, CharacterBody self, BuffIndex buffType, int newCount)
        {
            orig(self, buffType, newCount);

            BuffDef buffDef = BuffCatalog.GetBuffDef(buffType);
            if (buffDef && buffDef.isElite)
            {
                self.OnInventoryChanged();
            }
        }
    }
}
