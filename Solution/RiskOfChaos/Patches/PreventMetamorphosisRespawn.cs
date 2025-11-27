using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches
{
    static class PreventMetamorphosisRespawn
    {
        static readonly FieldInfo _preventionEnabled_FI = AccessTools.DeclaredField(typeof(PreventMetamorphosisRespawn), nameof(PreventionEnabled));
        public static bool PreventionEnabled;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CharacterMaster.Respawn_Vector3_Quaternion_bool += CharacterMaster_Respawn;
        }

        static void CharacterMaster_Respawn(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILLabel afterIfLbl = null;

            if (!c.TryFindNext(out ILCursor[] foundCursors,
                              x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(RoR2Content.Artifacts), nameof(RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef))),
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<RunArtifactManager>(_ => _.IsArtifactEnabled(default(ArtifactDef)))),
                              x => x.MatchBrfalse(out afterIfLbl)))
            {
                Log.Error("Unable to find patch location");
                return;
            }

            c.Goto(foundCursors[2].Next, MoveType.After); // brfalse

            c.Emit(OpCodes.Ldsfld, _preventionEnabled_FI);
            c.Emit(OpCodes.Brtrue, afterIfLbl);
        }
    }
}
