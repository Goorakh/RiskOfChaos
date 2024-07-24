using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Damage;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("disable_procs", 45f, AllowDuplicates = false)]
    public sealed class DisableProcs : TimedEffect, IDamageInfoModificationProvider
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        static bool _appliedPatches;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            IL.RoR2.HealthComponent.TakeDamage += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.Cripple))))
                {
                    ILLabel afterIfLabel = null;
                    if (c.TryGotoPrev(MoveType.After,
                                      x => x.MatchBrfalse(out afterIfLabel)))
                    {
                        c.EmitDelegate(isEffectActive);
                        static bool isEffectActive()
                        {
                            return TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo);
                        }

                        c.Emit(OpCodes.Brtrue, afterIfLabel);
                    }
                    else
                    {
                        Log.Error("Failed to find Cripple patch location");
                    }
                }
                else
                {
                    Log.Error("Failed to find Cripple apply location");
                }
            };

            _appliedPatches = true;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return DamageInfoModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref DamageInfo value)
        {
            value.procCoefficient = 0f;

            if (value.attacker)
            {
                value.damageType &= ~(DamageType.SlowOnHit | DamageType.ClayGoo | DamageType.Nullify | DamageType.CrippleOnHit | DamageType.ApplyMercExpose);
            }
        }

        public override void OnStart()
        {
            tryApplyPatches();
            DamageInfoModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (DamageInfoModificationManager.Instance)
            {
                DamageInfoModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
