using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("void_clover_upgrade_random_item", DefaultSelectionWeight = 0.5f)]
    public sealed class VoidCloverUpgradeRandomItem : NetworkBehaviour
    {
        [HarmonyPatch]
        static class TryCloverVoidUpgradesReversePatch
        {
            [HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.TryCloverVoidUpgrades))]
            [HarmonyReversePatch(HarmonyReversePatchType.Original)]
            public static void TryCloverVoidUpgrades(CharacterMaster instance, int numItemTransformations, Xoroshiro128Plus rng)
            {
                const int NUM_TRANSFORMATIONS_ARG_INDEX = 1;
                const int RNG_ARG_INDEX = 2;

                static void Transpiler(ILContext il)
                {
                    ILCursor c = new ILCursor(il);

                    int replaceRngPatchCount = 0;
                    while (c.TryGotoNext(MoveType.After,
                                         x => x.MatchLdarg(0),
                                         x => x.MatchLdfld<CharacterMaster>(nameof(CharacterMaster.cloverVoidRng))))
                    {
                        // Removing instructions causes issues with labels, so just pop the value instead
                        c.Emit(OpCodes.Pop);
                        c.Emit(OpCodes.Ldarg, RNG_ARG_INDEX);

                        replaceRngPatchCount++;
                    }

                    if (replaceRngPatchCount == 0)
                    {
                        Log.Error("Found 0 clover rng field patch locations");
                    }
                    else
                    {
                        Log.Debug($"Found {replaceRngPatchCount} clover rng field patch locations");
                    }

                    c.Index = 0;

                    if (c.TryGotoNext(MoveType.After,
                                      x => x.MatchLdcI4(3),
                                      x => x.MatchMul(),
                                      x => x.MatchStloc(out _)))
                    {
                        c.Index--;
                        c.Emit(OpCodes.Pop);
                        c.Emit(OpCodes.Ldarg, NUM_TRANSFORMATIONS_ARG_INDEX);
                    }
                    else
                    {
                        Log.Error("Failed to find numTransformations patch location");
                    }
                }

                Transpiler(null);
            }
        }

        [EffectConfig]
        static readonly ConfigHolder<int> _transformItemCount =
            ConfigFactory<int>.CreateConfig("Transform Item Count", 1)
                              .Description("How many items should be transformed per player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return ExpansionUtils.DLC1Enabled && (!context.IsNow || PlayerUtils.GetAllPlayerMasters(true).Any(m => m.inventory.HasAtLeastXTotalRemovableOwnedItemsOfTier(ItemTier.Tier1, 1) || m.inventory.HasAtLeastXTotalRemovableOwnedItemsOfTier(ItemTier.Tier2, 1)));
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
                {
                    upgradeRandomItem(playerMaster, _rng.Branch());
                }, Util.GetBestMasterName);
            }
        }

        static void upgradeRandomItem(CharacterMaster playerMaster, Xoroshiro128Plus rng)
        {
            TryCloverVoidUpgradesReversePatch.TryCloverVoidUpgrades(playerMaster, _transformItemCount.Value, rng);
        }
    }
}
