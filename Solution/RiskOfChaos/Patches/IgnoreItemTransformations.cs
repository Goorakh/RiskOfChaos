using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Items;
using RoR2BepInExPack.Utilities;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class IgnoreItemTransformations
    {
        class IgnoreTransformationInfo
        {
            public readonly HashSet<ItemIndex> IgnoreItems = [];
            public int IgnoreAllTransformationsCount = 0;

            public bool ShouldIgnoreTransformation(ItemIndex fromItemIndex)
            {
                return IgnoreAllTransformationsCount > 0 || IgnoreItems.Contains(fromItemIndex);
            }
        }

        static readonly FixedConditionalWeakTable<Inventory, IgnoreTransformationInfo> _ignoreTransformations = [];

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.Items.ContagiousItemManager.ProcessPendingChanges += ContagiousItemManager_ProcessPendingChanges;
        }

        static bool tryGetIgnoreTransformations(Inventory inventory, out IgnoreTransformationInfo ignoreTransformations)
        {
            return _ignoreTransformations.TryGetValue(inventory, out ignoreTransformations);
        }

        static IgnoreTransformationInfo getOrAddIgnoreTransformations(Inventory inventory)
        {
            if (!tryGetIgnoreTransformations(inventory, out IgnoreTransformationInfo ignoreTransformations))
            {
                ignoreTransformations = new IgnoreTransformationInfo();
                _ignoreTransformations.Add(inventory, ignoreTransformations);
            }

            return ignoreTransformations;
        }

        public static void IgnoreAllTransformationsFor(Inventory inventory)
        {
            if (!NetworkServer.active)
                return;

            IgnoreTransformationInfo ignoreTransformations = getOrAddIgnoreTransformations(inventory);
            ignoreTransformations.IgnoreAllTransformationsCount++;
        }

        public static void IgnoreTransformationsFor(Inventory inventory, ItemIndex fromItemIndex)
        {
            if (!NetworkServer.active)
                return;

            IgnoreTransformationInfo ignoreTransformations = getOrAddIgnoreTransformations(inventory);
            ignoreTransformations.IgnoreItems.Add(fromItemIndex);
        }

        public static void ResumeAllTransformationsFor(Inventory inventory)
        {
            if (!NetworkServer.active)
                return;

            if (tryGetIgnoreTransformations(inventory, out IgnoreTransformationInfo ignoreTransformations))
            {
                ignoreTransformations.IgnoreAllTransformationsCount--;
            }
        }

        public static void ResumeTransformationsFor(Inventory inventory, ItemIndex fromItemIndex)
        {
            if (!NetworkServer.active)
                return;

            if (tryGetIgnoreTransformations(inventory, out IgnoreTransformationInfo ignoreTransformations))
            {
                ignoreTransformations.IgnoreItems.Remove(fromItemIndex);
            }
        }

        static void ContagiousItemManager_ProcessPendingChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int inventoryReplacementCandidateLocalIndex = -1;
            if (!c.TryGotoNext(MoveType.After,
                               x => x.MatchLdloca(out inventoryReplacementCandidateLocalIndex),
                               x => x.MatchLdflda<ContagiousItemManager.InventoryReplacementCandidate>(nameof(ContagiousItemManager.InventoryReplacementCandidate.time)),
                               x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Run.FixedTimeStamp), nameof(Run.FixedTimeStamp.hasPassed)))))
            {
                Log.Error("Failed to find inventory replacement local");
                return;
            }

            ILLabel afterIfLabel = null;
            if (!c.TryGotoNext(MoveType.After,
                               x => x.MatchBrfalse(out afterIfLabel)))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Emit(OpCodes.Ldloca, inventoryReplacementCandidateLocalIndex);
            c.EmitDelegate(canProcessReplacement);

            static bool canProcessReplacement(ref ContagiousItemManager.InventoryReplacementCandidate inventoryReplacementCandidate)
            {
                Inventory inventory = inventoryReplacementCandidate.inventory;
                bool ignored = tryGetIgnoreTransformations(inventory, out IgnoreTransformationInfo ignoreTransformations) &&
                               ignoreTransformations.ShouldIgnoreTransformation(inventoryReplacementCandidate.originalItem);

                if (ignored)
                {
                    inventoryReplacementCandidate.time += ContagiousItemManager.transformDelay;
                }

                return !ignored;
            }

            c.Emit(OpCodes.Brfalse, afterIfLabel);
        }
    }
}
