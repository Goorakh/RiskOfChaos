using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class VoidCampOverrideRNGSeedPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CampDirector.Start += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchLdfld<Run>(nameof(Run.stageRng))))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((Xoroshiro128Plus stageRNG, CampDirector instance) =>
                    {
                        RNGOverrideTracker rngOverride = instance.GetComponentInParent<RNGOverrideTracker>();
                        if (rngOverride && rngOverride.RNG != null)
                            return rngOverride.RNG;

                        return stageRNG;
                    });
                }
            };
        }

        public static void OverrideRNG(GameObject campObj, Xoroshiro128Plus overrideRNG)
        {
            if (!campObj.TryGetComponent(out RNGOverrideTracker rngTracker))
            {
                rngTracker = campObj.AddComponent<RNGOverrideTracker>();
            }

            rngTracker.RNG = overrideRNG;
        }

        class RNGOverrideTracker : MonoBehaviour
        {
            public Xoroshiro128Plus RNG;
        }
    }
}
