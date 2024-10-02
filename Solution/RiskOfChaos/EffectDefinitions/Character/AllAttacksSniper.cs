using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("all_attacks_sniper", 90f, AllowDuplicates = false)]
    public sealed class AllAttacksSniper : TimedEffect
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        static GameObject sniperTargetHitEffect => BulletAttack.sniperTargetHitEffect;

        static bool _appliedPatches = false;

        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            On.RoR2.BulletAttack.Fire += (orig, self) =>
            {
                if (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                {
                    self.sniper = true;
                }

                orig(self);
            };

            IL.RoR2.BlastAttack.HandleHits += il =>
            {
                ILCursor c = new ILCursor(il);

                int hitPointLocalIndex = -1;
                if (!c.TryGotoNext(x => x.MatchLdarg(1), // HitPoint[] hitPoints
                                   x => x.MatchLdloc(out _), // iterator variable
                                   x => x.MatchLdelemAny<BlastAttack.HitPoint>(),
                                   x => x.MatchStloc(out hitPointLocalIndex)))
                {
                    Log.Error("Unable to find hitPoint local index");
                    return;
                }

                int constructingBlastAttackDamageInfoLocalIndex = -1;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchLdloca(out constructingBlastAttackDamageInfoLocalIndex),
                                  x => x.MatchInitobj<BlastAttack.BlastAttackDamageInfo>()))
                {
                    if (c.TryGotoNext(MoveType.Before,
                                      x => x.MatchLdloc(constructingBlastAttackDamageInfoLocalIndex),
                                      x => x.MatchStloc(out _)))
                    {
                        c.Index++;
                        c.Emit(OpCodes.Ldloc, hitPointLocalIndex);

                        c.EmitDelegate(trySniperHit);
                        static BlastAttack.BlastAttackDamageInfo trySniperHit(BlastAttack.BlastAttackDamageInfo blastDamageInfo, BlastAttack.HitPoint hitPoint)
                        {
                            if (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                            {
                                if (hitPoint.hurtBox.isSniperTarget)
                                {
                                    if (sniperTargetHitEffect)
                                    {
                                        EffectData effectData = new EffectData
                                        {
                                            origin = hitPoint.hitPosition,
                                            rotation = Util.QuaternionSafeLookRotation(hitPoint.hitNormal)
                                        };
                                        effectData.SetHurtBoxReference(hitPoint.hurtBox);

                                        EffectManager.SpawnEffect(sniperTargetHitEffect, effectData, true);
                                    }

                                    blastDamageInfo.crit = true;
                                    blastDamageInfo.damageColorIndex = DamageColorIndex.Sniper;
                                }
                            }

                            return blastDamageInfo;
                        }
                    }
                    else
                    {
                        Log.Error("Failed to find patch location");
                    }
                }
                else
                {
                    Log.Error("Failed to find blastDamageInfo local index");
                }
            };

            On.RoR2.OverlapAttack.ProcessHits += (orig, self, boxedHitList) =>
            {
                orig(self, boxedHitList);

                if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                    return;

                if (boxedHitList is List<OverlapAttack.OverlapInfo> hitList)
                {
                    if (sniperTargetHitEffect)
                    {
                        foreach (OverlapAttack.OverlapInfo overlapInfo in hitList)
                        {
                            if (overlapInfo.hurtBox && overlapInfo.hurtBox.isSniperTarget)
                            {
                                EffectData effectData = new EffectData
                                {
                                    origin = overlapInfo.hitPosition,
                                    rotation = Util.QuaternionSafeLookRotation(-overlapInfo.pushDirection)
                                };
                                effectData.SetHurtBoxReference(overlapInfo.hurtBox);

                                EffectManager.SpawnEffect(sniperTargetHitEffect, effectData, true);
                            }
                        }
                    }
                }
            };

            IL.RoR2.OverlapAttack.PerformDamage += il =>
            {
                ILCursor c = new ILCursor(il);

                int overlapInfoLocalIndex = -1;
                if (!c.TryGotoNext(x => x.MatchLdloc(out overlapInfoLocalIndex),
                                   x => x.MatchLdfld<OverlapAttack.OverlapInfo>(nameof(OverlapAttack.OverlapInfo.hurtBox))))
                {
                    Log.Error("Unable to find overlapInfo local index");
                    return;
                }

                if (c.TryGotoNext(MoveType.Before,
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<HealthComponent>(_ => _.TakeDamage(default)))))
                {
                    int patchLocation = c.Index;

                    int damageInfoLocalIndex = -1;
                    if (!c.TryGotoPrev(x => x.MatchLdloc(out damageInfoLocalIndex)))
                    {
                        Log.Error("Unable to find damageInfo local index");
                        return;
                    }

                    c.Index = patchLocation;

                    c.Emit(OpCodes.Ldloc, overlapInfoLocalIndex);
                    c.Emit(OpCodes.Ldloc, damageInfoLocalIndex);
                    c.EmitDelegate(applySniperDamage);
                    void applySniperDamage(OverlapAttack.OverlapInfo overlapInfo, DamageInfo damageInfo)
                    {
                        if (!overlapInfo.hurtBox || !overlapInfo.hurtBox.isSniperTarget)
                            return;

                        if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                            return;

                        damageInfo.crit = true;
                        damageInfo.damageColorIndex = DamageColorIndex.Sniper;
                    }
                }
                else
                {
                    Log.Error("Unable to find patch location");
                }
            };

            _appliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();
        }

        public override void OnEnd()
        {
        }
    }
}
