using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.Patches
{
    // Fixes PickupDisplays destroying their existing models and leaving nothing left if it doesn't instantiate pickup models
    static class PickupDisplayModelFixPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.PickupDisplay.RebuildModel += PickupDisplay_RebuildModel;
        }

        static void PickupDisplay_RebuildModel(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.Before,
                               x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<PickupDisplay>(_ => _.DestroyModel()))))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(PickupDisplay), nameof(PickupDisplay.dontInstantiatePickupModel)));
            c.EmitSkipMethodCall(OpCodes.Brtrue);
        }
    }
}
