using RiskOfChaos.Components;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class MinCharacterBuffCountPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterBody.SetBuffCount += CharacterBody_SetBuffCount;
        }

        static void CharacterBody_SetBuffCount(On.RoR2.CharacterBody.orig_SetBuffCount orig, CharacterBody self, BuffIndex buffType, int newCount)
        {
            if (self)
            {
                foreach (KeepBuff keepBuff in KeepBuff.Instances)
                {
                    if (keepBuff.Body == self)
                    {
                        keepBuff.EnsureValidBuffCount(buffType, ref newCount);
                    }
                }
            }

            orig(self, buffType, newCount);
        }
    }
}
