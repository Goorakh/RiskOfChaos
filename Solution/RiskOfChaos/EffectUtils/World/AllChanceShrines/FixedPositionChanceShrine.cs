using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectUtils.World.AllChanceShrines
{
    public static class FixedPositionChanceShrine
    {
        public static InteractableSpawnCard SpawnCard { get; private set; }

        [SystemInitializer]
        static IEnumerator Init()
        {
            AsyncOperationHandle<InteractableSpawnCard> iscShrineChanceLoad = AddressableUtil.LoadTempAssetAsync<InteractableSpawnCard>(AddressableGuids.RoR2_Base_ShrineChance_iscShrineChance_asset);
            iscShrineChanceLoad.OnSuccess(iscChanceShrine =>
            {
                // Make new instance of the spawn card so that settings can safely be changed without messing with the original behavior
                SpawnCard = ScriptableObject.Instantiate(iscChanceShrine);
                SpawnCard.name = "iscShrineChanceFixedPosition";

                // Make sure it'll always spawn no matter what
                SpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;

                // Make sure the shrine will be spawned at the exact position and rotation given
                SpawnCard.orientToFloor = false;
                SpawnCard.slightlyRandomizeOrientation = false;

                // Prevent random rotation around local y axis
                IL.RoR2.InteractableSpawnCard.Spawn += il =>
                {
                    ILCursor c = new ILCursor(il);

                    ILLabel patchLocationLbl = null;
                    if (c.TryGotoNext(MoveType.After,
                                      x => x.MatchLdfld<InteractableSpawnCard>(nameof(InteractableSpawnCard.orientToFloor)),
                                      x => x.MatchBrfalse(out patchLocationLbl)))
                    {
                        c.Goto(patchLocationLbl.Target, MoveType.AfterLabel);

                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitDelegate(allowRandomRotation);

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static bool allowRandomRotation(InteractableSpawnCard instance)
                        {
                            return instance != SpawnCard;
                        }

                        ILLabel afterRotateLbl = c.DefineLabel();
                        c.Emit(OpCodes.Brfalse, afterRotateLbl);
                        Instruction fallbackAfterRotateLblTarget = c.Next;

                        if (c.TryGotoNext(MoveType.After,
                                          x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Transform>(_ => _.Rotate(default(Vector3), default, default)))))
                        {
                            afterRotateLbl.Target = c.Next;
                        }
                        else
                        {
                            afterRotateLbl.Target = fallbackAfterRotateLblTarget;
                            Log.Error($"Failed to find {nameof(afterRotateLbl)} target location");
                        }
                    }
                    else
                    {
                        Log.Warning("Failed to find patch location");
                    }
                };
            });

            return iscShrineChanceLoad;
        }
    }
}
