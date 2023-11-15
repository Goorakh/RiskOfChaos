using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.ModCompatibility;
using RiskOfChaos.Trackers;
using RoR2;
using System;
using System.Reflection;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("hide_all_pickup_displays", 60f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class HideAllPickupDisplays : TimedEffect
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static bool effectActive => TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(_effectInfo);

        static bool _hasAppliedPatches = false;

        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            IL.RoR2.PickupDisplay.RebuildModel += il =>
            {
                ILCursor c = new ILCursor(il);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                const string HIDDEN_FIELD_NAME = nameof(PickupDisplay.hidden);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                while (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<PickupDisplay>(HIDDEN_FIELD_NAME)))
                {
                    c.EmitDelegate((bool hidden) =>
                    {
                        return hidden || effectActive;
                    });
                }
            };

            MethodInfo ShopTerminalBehavior_get_pickupIndexIsHidden_MI = AccessTools.DeclaredPropertyGetter(typeof(ShopTerminalBehavior), nameof(ShopTerminalBehavior.pickupIndexIsHidden));
            if (ShopTerminalBehavior_get_pickupIndexIsHidden_MI is null)
            {
                Log.Warning("Unable to find ShopTerminalBehavior.pickupIndexIsHidden getter");
            }
            else
            {
                new Hook(ShopTerminalBehavior_get_pickupIndexIsHidden_MI, (Func<ShopTerminalBehavior, bool> orig, ShopTerminalBehavior self) =>
                {
                    return orig(self) || effectActive;
                });
            }

            On.RoR2.GenericPickupController.GetDisplayName += (orig, self) =>
            {
                string displayName = orig(self);
                return effectActive ? "???" : displayName;
            };

            // BetterUI can't be bothered to make a proper hook calling orig, so we have to hook their hook -_-
            if (BetterUICompat.Active)
            {
                Assembly betterUIAssembly = BetterUICompat.MainAssembly;
                if (betterUIAssembly is not null)
                {
                    Type miscType = betterUIAssembly.GetType("BetterUI.Misc");
                    if (miscType is not null)
                    {
                        MethodInfo getContextStringHookMethod = miscType.GetMethod("GenericPickupController_GetContextString", BindingFlags.NonPublic | BindingFlags.Static);
                        if (getContextStringHookMethod is not null)
                        {
                            // Yo dawg heard you like hooks so we put a hook in your hook
                            new Hook(getContextStringHookMethod, (Func<Func<GenericPickupController, Interactor, string>, GenericPickupController, Interactor, string> hook, Func<GenericPickupController, Interactor, string> orig, GenericPickupController self, Interactor interactor) =>
                            {
                                if (effectActive)
                                {
                                    return orig(self, interactor);
                                }
                                else
                                {
                                    return hook(orig, self, interactor);
                                }
                            });
                        }
                        else
                        {
                            Log.Error("Unable to find BetterUI.Misc.GenericPickupController_GetContextString method");
                        }
                    }
                    else
                    {
                        Log.Error("Unable to find BetterUI.Misc type");
                    }
                }
            }

            _hasAppliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();

            refreshAllPickupDisplays();
        }

        public override void OnEnd()
        {
            refreshAllPickupDisplays();
        }

        static void refreshAllPickupDisplays()
        {
            foreach (PickupDisplayTracker pickupDisplayTracker in InstanceTracker.GetInstancesList<PickupDisplayTracker>())
            {
                PickupDisplay pickupDisplay = pickupDisplayTracker.PickupDisplay;
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                pickupDisplay.RebuildModel();
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
