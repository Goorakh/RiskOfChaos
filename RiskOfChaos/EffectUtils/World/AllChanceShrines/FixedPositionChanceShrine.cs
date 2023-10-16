using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectUtils.World.AllChanceShrines
{
    public static class FixedPositionChanceShrine
    {
        public static InteractableSpawnCard SpawnCard { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            InteractableSpawnCard iscChanceShrine = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/ShrineChance/iscShrineChance.asset").WaitForCompletion();

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
                    c.Goto(patchLocationLbl.Target, MoveType.Before);

                    int beforeDelegateIndex = c.Index;

                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((InteractableSpawnCard instance) =>
                    {
                        return instance != SpawnCard;
                    });

                    ILLabel afterRotateLbl = c.DefineLabel();

                    c.Emit(OpCodes.Brfalse, afterRotateLbl);

                    int afterDelegateIndex = c.Index;

                    c.Goto(beforeDelegateIndex, MoveType.Before);
                    patchLocationLbl.Target = c.Next;

                    c.Index = afterDelegateIndex;

                    if (c.TryGotoNext(MoveType.After,
                                      x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Transform>(_ => _.Rotate(default(Vector3), default, default)))))
                    {
                        afterRotateLbl.Target = c.Next;
                    }
                    else
                    {
                        Log.Error($"Failed to find {nameof(afterRotateLbl)} target location");
                    }
                }
                else
                {
                    Log.Warning("Failed to find patch location");
                }
            };
        }
    }
}
