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
        static readonly HashSet<Inventory> _ignoreTransformationsFor = [];

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

            int inventoryReplacementCandidateLocalIndex = -1;
            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchLdloca(out inventoryReplacementCandidateLocalIndex),
                              x => x.MatchLdflda<ContagiousItemManager.InventoryReplacementCandidate>(nameof(ContagiousItemManager.InventoryReplacementCandidate.time)),
                              x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Run.FixedTimeStamp), nameof(Run.FixedTimeStamp.hasPassed)))))
            {
                ILLabel afterIfLabel = null;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchBrfalse(out afterIfLabel)))
                {
                    c.Emit(OpCodes.Ldloca, inventoryReplacementCandidateLocalIndex);
                    c.EmitDelegate(canProcessReplacement);

                    static bool canProcessReplacement(ref ContagiousItemManager.InventoryReplacementCandidate inventoryReplacementCandidate)
                    {
                        bool ignored = _ignoreTransformationsFor.Contains(inventoryReplacementCandidate.inventory);

                        if (ignored)
                        {
                            inventoryReplacementCandidate.time += ContagiousItemManager.transformDelay;
                        }

                        return !ignored;
                    }

                    c.Emit(OpCodes.Brfalse, afterIfLabel);
                }
                else
                {
                    Log.Error("Failed to find patch location");
                }
            }
            else
            {
                Log.Error("Failed to find inventory replacement local");
            }
        }
    }
}
