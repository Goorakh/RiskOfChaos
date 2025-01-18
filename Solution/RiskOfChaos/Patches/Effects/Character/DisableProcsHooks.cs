using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectDefinitions.Character;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.Patches.Effects.Character
{
    static class DisableProcsHooks
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool isEffectActive()
        {
            return ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(DisableProcs.EffectInfo);
        }

        static void HealthComponent_TakeDamageProcess(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryGotoNext(x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.Cripple))))
            {
                Log.Error("Failed to find Cripple apply location");
                return;
            }

            ILLabel afterIfLabel = null;
            if (!c.TryGotoPrev(MoveType.After,
                               x => x.MatchBrfalse(out afterIfLabel)))
            {
                Log.Error("Failed to find Cripple patch location");
                return;
            }

            c.MoveAfterLabels();
            c.EmitDelegate(isEffectActive);

            c.Emit(OpCodes.Brtrue, afterIfLabel);
        }
    }
}
