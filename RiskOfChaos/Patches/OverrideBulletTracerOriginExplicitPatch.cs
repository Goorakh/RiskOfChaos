using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class OverrideBulletTracerOriginExplicitPatch
    {
        public delegate bool UseExplicitOriginPositionDelegate(BulletAttack bulletAttack);
        public static event UseExplicitOriginPositionDelegate UseExplicitOriginPosition;

        static bool shouldUseExplicitOriginPosition(BulletAttack bulletAttack)
        {
            foreach (UseExplicitOriginPositionDelegate useExplicitPositionDelegate in UseExplicitOriginPosition.GetInvocationList())
            {
                if (useExplicitPositionDelegate(bulletAttack))
                    return true;
            }

            return false;
        }

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle_FixTracerEffectOrigin;
        }

        static void BulletAttack_FireSingle_FixTracerEffectOrigin(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetChildLocatorTransformReference))))
            {
                if (c.TryGotoPrev(MoveType.After, x => x.MatchLdfld<BulletAttack>(nameof(BulletAttack.weapon))))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(overrideEffectOrigin);
                    static GameObject overrideEffectOrigin(GameObject weapon, BulletAttack instance)
                    {
                        return shouldUseExplicitOriginPosition(instance) ? null : weapon;
                    }
                }
                else
                {
                    Log.Error("Failed to find weapon ldfld");
                }
            }
            else
            {
                Log.Error("Failed to find EffectData.SetChildLocatorTransformReference call");
            }
        }
    }
}
