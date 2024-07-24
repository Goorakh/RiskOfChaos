using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("swap_health_shield", 60f, AllowDuplicates = false)]
    public sealed class SwapHealthShield : TimedEffect
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        static bool _appliedPatches;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            IL.RoR2.CharacterBody.RecalculateStats += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertySetter(typeof(CharacterBody), nameof(CharacterBody.maxShield)))))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((CharacterBody body) =>
                    {
                        if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                            return;

                        float maxHealth = body.maxHealth;
                        float maxShield = body.maxShield;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        body.maxHealth = Mathf.Max(1f, maxShield);
                        body.maxShield = maxHealth;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    });
                }
            };

            _appliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }

        public override void OnEnd()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }
    }
}
