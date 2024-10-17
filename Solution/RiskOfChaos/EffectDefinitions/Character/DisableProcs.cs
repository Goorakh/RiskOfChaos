using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("disable_procs", 45f, AllowDuplicates = false)]
    public sealed class DisableProcs : NetworkBehaviour
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.Cripple))))
                {
                    ILLabel afterIfLabel = null;
                    if (c.TryGotoPrev(MoveType.After,
                                      x => x.MatchBrfalse(out afterIfLabel)))
                    {
                        c.MoveAfterLabels();
                        c.EmitDelegate(isEffectActive);

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static bool isEffectActive()
                        {
                            return ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(_effectInfo);
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
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                DamageModificationHooks.ModifyDamageInfo += modifyDamage;
            }
        }

        void OnDestroy()
        {
            DamageModificationHooks.ModifyDamageInfo -= modifyDamage;
        }

        static void modifyDamage(DamageInfo damageInfo)
        {
            damageInfo.procCoefficient = 0f;

            if (damageInfo.attacker)
            {
                damageInfo.damageType &= ~(DamageTypeCombo)(DamageType.SlowOnHit | DamageType.ClayGoo | DamageType.Nullify | DamageType.CrippleOnHit | DamageType.ApplyMercExpose);
            }
        }
    }
}
