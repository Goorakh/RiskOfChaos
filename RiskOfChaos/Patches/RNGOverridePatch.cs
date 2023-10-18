using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class RNGOverridePatch
    {
        [SystemInitializer]
        static void Init()
        {
            static void replaceRNGField(ILContext il, Type declaringType, string fieldName)
            {
                ILCursor c = new ILCursor(il);

                while (c.TryGotoNext(MoveType.After,
                                     x => x.MatchLdfld(declaringType, fieldName)))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((Xoroshiro128Plus originalRNG, MonoBehaviour instance) =>
                    {
                        return RNGOverrideTracker.GetOverrideRNG(instance, originalRNG);
                    });
                }
            }

            static void replaceStageRNG(ILContext il)
            {
                replaceRNGField(il, typeof(Run), nameof(Run.stageRng));
            }

            static void replaceTreasureRNG(ILContext il)
            {
                replaceRNGField(il, typeof(Run), nameof(Run.treasureRng));
            }

            IL.RoR2.CampDirector.Start += replaceStageRNG;

            IL.RoR2.ChestBehavior.Start += replaceTreasureRNG;
            IL.RoR2.MultiShopController.Start += replaceTreasureRNG;
            IL.RoR2.OptionChestBehavior.Start += replaceTreasureRNG;
            IL.RoR2.RouletteChestController.Start += replaceTreasureRNG;
            IL.RoR2.ShopTerminalBehavior.Start += replaceTreasureRNG;
            IL.RoR2.ShrineChanceBehavior.Start += replaceTreasureRNG;
            IL.RoR2.VoidSuppressorBehavior.Start += replaceTreasureRNG;

            IL.RoR2.CampDirector.PopulateCamp += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchCallOrCallvirt<DirectorCore>(nameof(DirectorCore.TrySpawnObject))))
                {
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((GameObject spawnedInteractable, CampDirector instance) =>
                    {
                        if (Configs.General.SeededEffectSelection.Value && spawnedInteractable)
                        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            Xoroshiro128Plus campDirectorRNG = instance.rng;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                            OverrideRNG(spawnedInteractable, new Xoroshiro128Plus(campDirectorRNG));
                        }
                    });

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    const string CAMP_DIRECTOR_RNG_FIELD_NAME = nameof(CampDirector.rng);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                    if (c.TryGotoPrev(MoveType.After,
                                      x => x.MatchLdfld<CampDirector>(CAMP_DIRECTOR_RNG_FIELD_NAME)))
                    {
                        c.EmitDelegate((Xoroshiro128Plus rng) =>
                        {
                            if (Configs.General.SeededEffectSelection.Value)
                            {
                                return new Xoroshiro128Plus(rng);
                            }
                            else
                            {
                                return rng;
                            }
                        });
                    }
                    else
                    {
                        Log.Error("Failed to find object spawn rng hook location");
                    }
                }
                else
                {
                    Log.Error("Failed to find TrySpawnObject hook location");
                }
            };
        }

        public static void OverrideRNG(GameObject obj, Xoroshiro128Plus overrideRNG)
        {
            if (!obj.TryGetComponent(out RNGOverrideTracker rngTracker))
            {
                rngTracker = obj.AddComponent<RNGOverrideTracker>();
            }

            rngTracker.RNG = overrideRNG;
        }

        class RNGOverrideTracker : MonoBehaviour
        {
            public Xoroshiro128Plus RNG;

            public static Xoroshiro128Plus GetOverrideRNG(MonoBehaviour instance, Xoroshiro128Plus defaultRNG)
            {
                RNGOverrideTracker rngOverride = instance.GetComponentInParent<RNGOverrideTracker>();
                if (rngOverride && rngOverride.RNG != null)
                {
                    return rngOverride.RNG;
                }

                if (instance is ShopTerminalBehavior shopTerminalBehavior && shopTerminalBehavior.serverMultiShopController)
                {
                    return GetOverrideRNG(shopTerminalBehavior.serverMultiShopController, defaultRNG);
                }

                return defaultRNG;
            }
        }
    }
}
