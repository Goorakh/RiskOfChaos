using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Patches
{
    static class PreventMetamorphosisRespawn
    {
        internal static bool PreventionEnabled;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CharacterMaster.Respawn += static il =>
            {
                ILCursor c = new ILCursor(il);

                ILCursor[] foundCursors;
                if (c.TryFindNext(out foundCursors,
                                  x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(RoR2Content.Artifacts), nameof(RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef))),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<RunArtifactManager>(_ => _.IsArtifactEnabled(default(ArtifactDef)))),
                                  x => x.MatchBrfalse(out _)))
                {
                    ILCursor cursor = foundCursors[2];
                    cursor.Next.MatchBrfalse(out ILLabel afterIfLbl);
                    cursor.Index++;

                    cursor.EmitDelegate(static () => PreventionEnabled);
                    cursor.Emit(OpCodes.Brtrue, afterIfLbl);
                }
            };
        }
    }
}
