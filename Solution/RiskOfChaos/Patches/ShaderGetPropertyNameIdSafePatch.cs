using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ShaderGetPropertyNameIdSafePatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.EntityStates.ShrineHalcyonite.ShrineHalcyoniteBaseState.OnEnter += safeGetPropertyNameIdPatch;
            IL.EntityStates.Geode.GeodeEntityStates.OnEnter += safeGetPropertyNameIdPatch;
        }

        static void safeGetPropertyNameIdPatch(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            VariableDefinition shaderVar = il.AddVariable<Shader>();
            VariableDefinition propertyIndexVar = il.AddVariable<int>();

            int patchCount = 0;

            while (c.TryGotoNext(MoveType.Before,
                                 x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Shader>(_ => _.GetPropertyNameId(default)))))
            {
                c.EmitStoreStack(shaderVar, propertyIndexVar);

                c.Emit(OpCodes.Ldloc, shaderVar);
                c.Emit(OpCodes.Ldloc, propertyIndexVar);
                c.EmitDelegate(isValidPropertyIndex);
                static bool isValidPropertyIndex(Shader shader, int propertyIndex)
                {
                    return propertyIndex >= 0 && propertyIndex < shader.GetPropertyCount();
                }

                c.EmitSkipMethodCall(OpCodes.Brfalse, c =>
                {
                    c.Emit(OpCodes.Ldc_I4_M1);
                });

                c.SearchTarget = SearchTarget.Next;

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error($"Failed to find any patch location(s) for {il.Method.FullName}");
            }
            else
            {
                Log.Debug($"Found {patchCount} patch location(s) for {il.Method.FullName}");
            }
        }
    }
}
