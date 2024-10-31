using RiskOfChaos.Components;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class MinCharacterBuffCountPatch
    {
        static readonly List<KeepBuff> _keepBuffComponentsBuffer = [];

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterBody.SetBuffCount += CharacterBody_SetBuffCount;
        }

        static void CharacterBody_SetBuffCount(On.RoR2.CharacterBody.orig_SetBuffCount orig, CharacterBody self, BuffIndex buffType, int newCount)
        {
            int totalMinBuffCount = 0;

            self.GetComponents(_keepBuffComponentsBuffer);
            foreach (KeepBuff keepBuff in _keepBuffComponentsBuffer)
            {
                if (keepBuff.enabled && keepBuff.BuffIndex == buffType)
                {
                    if (keepBuff.MinBuffCount >= 0)
                    {
                        totalMinBuffCount += keepBuff.MinBuffCount;
                    }
                }
            }

            orig(self, buffType, Mathf.Max(totalMinBuffCount, newCount));
        }
    }
}
