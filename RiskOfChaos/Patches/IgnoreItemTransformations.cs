using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Items;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class IgnoreItemTransformations
    {
        static readonly HashSet<Inventory> _ignoreTransformationsFor = new HashSet<Inventory>();

        static IgnoreItemTransformations()
        {
            RoR2Application.onFixedUpdate += () =>
            {
                if (_ignoreTransformationsFor.Count > 0 && (!NetworkServer.active || !Run.instance))
                {
                    _ignoreTransformationsFor.Clear();
                }
            };
        }

        public static void IgnoreTransformationsFor(Inventory inventory)
        {
            if (!NetworkServer.active)
                return;

            if (_ignoreTransformationsFor.Add(inventory))
            {
                tryApplyPatches();
            }
        }

        public static void ResumeTransformationsFor(Inventory inventory)
        {
            if (!NetworkServer.active)
                return;

            _ignoreTransformationsFor.Remove(inventory);
        }

        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            IL.RoR2.Items.ContagiousItemManager.ProcessPendingChanges += ContagiousItemManager_ProcessPendingChanges;

            _hasAppliedPatches = true;
        }

        static void ContagiousItemManager_ProcessPendingChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            const string InventoryReplacementCandidate_time_NAME = nameof(ContagiousItemManager.InventoryReplacementCandidate.time);

            int inventoryReplacementCandidateLocalIndex = -1;
            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchLdloca(out inventoryReplacementCandidateLocalIndex),
                              x => x.MatchLdflda<ContagiousItemManager.InventoryReplacementCandidate>(InventoryReplacementCandidate_time_NAME),
                              x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Run.FixedTimeStamp), nameof(Run.FixedTimeStamp.hasPassed)))))
            {
                c.Emit(OpCodes.Ldloc, inventoryReplacementCandidateLocalIndex);
                c.EmitDelegate((ContagiousItemManager.InventoryReplacementCandidate inventoryReplacementCandidate) =>
                {
                    return !_ignoreTransformationsFor.Contains(inventoryReplacementCandidate.inventory);
                });

                c.Emit(OpCodes.And);
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }
    }
}
