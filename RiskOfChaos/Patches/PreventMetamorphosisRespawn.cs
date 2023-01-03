using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RiskOfChaos.Patches
{
    static class PreventMetamorphosisRespawn
    {
        static bool _hasAppliedPatch;

        static readonly FieldInfo _preventionEnabled_FI = AccessTools.DeclaredField(typeof(PreventMetamorphosisRespawn), nameof(_preventionEnabled));
        static bool _preventionEnabled;

        internal static bool PreventionEnabled
        {
            get
            {
                return _preventionEnabled;
            }
            set
            {
                _preventionEnabled = value;

                if (!_hasAppliedPatch && _preventionEnabled)
                {
                    IL.RoR2.CharacterMaster.Respawn += CharacterMaster_Respawn;
                    _hasAppliedPatch = true;
                }
            }
        }

        static void CharacterMaster_Respawn(ILContext il)
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

                cursor.Emit(OpCodes.Ldsfld, _preventionEnabled_FI);
                cursor.Emit(OpCodes.Brtrue, afterIfLbl);
            }
            else
            {
                Log.Error("Unable to find patch location");
            }
        }
    }
}
