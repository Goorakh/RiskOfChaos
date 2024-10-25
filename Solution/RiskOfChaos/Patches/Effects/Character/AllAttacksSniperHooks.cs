using EntityStates.Merc;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RiskOfChaos.EffectDefinitions.Character;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;
using RoR2.Orbs;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches.Effects.Character
{
    static class AllAttacksSniperHooks
    {
        static GameObject sniperTargetHitEffect => BulletAttack.sniperTargetHitEffect;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.BulletAttack.Fire += BulletAttack_Fire;

            IL.RoR2.BlastAttack.HandleHits += BlastAttack_HandleHits;

            IL.RoR2.OverlapAttack.PerformDamage += OverlapAttack_PerformDamage;
            On.RoR2.OverlapAttack.ProcessHits += OverlapAttack_ProcessHits;

            IL.EntityStates.Merc.Evis.FixedUpdate += Evis_FixedUpdate;

            IL.RoR2.Orbs.BounceOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.ChainGunOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.DamageOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.DevilOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.GenericDamageOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.LightningOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.LunarDetonatorOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.SquidOrb.OnArrival += hookOrbDamage;
            IL.RoR2.Orbs.VoidLightningOrb.Strike += hookOrbDamage;
        }

        static void BulletAttack_Fire(On.RoR2.BulletAttack.orig_Fire orig, BulletAttack self)
        {
            if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(AllAttacksSniper.EffectInfo))
            {
                self.sniper = true;
            }

            orig(self);
        }

        static void BlastAttack_HandleHits(ILContext il)
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
                        if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(AllAttacksSniper.EffectInfo))
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
        }

        static void OverlapAttack_PerformDamage(ILContext il)
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
                int damageInfoLocalIndex = -1;
                if (!c.Clone().TryGotoPrev(x => x.MatchLdloc(out damageInfoLocalIndex)))
                {
                    Log.Error("Unable to find damageInfo local index");
                    return;
                }

                c.Emit(OpCodes.Ldloc, overlapInfoLocalIndex);
                c.Emit(OpCodes.Ldloc, damageInfoLocalIndex);
                c.EmitDelegate(applySniperDamage);
                void applySniperDamage(OverlapAttack.OverlapInfo overlapInfo, DamageInfo damageInfo)
                {
                    if (!overlapInfo.hurtBox || !overlapInfo.hurtBox.isSniperTarget)
                        return;

                    if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(AllAttacksSniper.EffectInfo))
                    {
                        damageInfo.crit = true;
                        damageInfo.damageColorIndex = DamageColorIndex.Sniper;
                    }
                }
            }
            else
            {
                Log.Error("Unable to find patch location");
            }
        }

        static void OverlapAttack_ProcessHits(On.RoR2.OverlapAttack.orig_ProcessHits orig, OverlapAttack self, object boxedHitList)
        {
            orig(self, boxedHitList);

            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(AllAttacksSniper.EffectInfo))
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
        }

        static void Evis_FixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int hurtBoxLocalIndex = -1;
            if (!c.TryFindNext(out _,
                               x => x.MatchLdfld<HurtBoxGroup>(nameof(HurtBoxGroup.hurtBoxes)),
                               x => x.MatchLdelemRef(),
                               x => x.MatchStloc(out hurtBoxLocalIndex)))
            {
                Log.Error("Failed to find hurtBox local index");
                return;
            }

            c.Index = 0;

            if (c.TryGotoNext(MoveType.Before,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<HealthComponent>(_ => _.TakeDamage(default)))))
            {
                int damageInfoLocalIndex = -1;
                if (!c.Clone().TryGotoPrev(x => x.MatchLdloc(out damageInfoLocalIndex) && il.Method.Body.Variables[damageInfoLocalIndex].VariableType.Is(typeof(DamageInfo))))
                {
                    Log.Error("Unable to find damageInfo local index");
                    return;
                }

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, hurtBoxLocalIndex);
                c.Emit(OpCodes.Ldloc, damageInfoLocalIndex);
                c.EmitDelegate(applySniperDamage);
                static void applySniperDamage(Evis evisState, HurtBox hurtBox, DamageInfo damageInfo)
                {
                    if (!hurtBox || !hurtBox.isSniperTarget)
                        return;

                    if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(AllAttacksSniper.EffectInfo))
                    {
                        damageInfo.crit = true;
                        damageInfo.damageColorIndex = DamageColorIndex.Sniper;

                        EffectData effectData = new EffectData
                        {
                            origin = hurtBox.transform.position,
                            rotation = Util.QuaternionSafeLookRotation(evisState.transform.position - hurtBox.transform.position)
                        };
                        effectData.SetHurtBoxReference(hurtBox);

                        EffectManager.SpawnEffect(sniperTargetHitEffect, effectData, true);
                    }
                }
            }
            else
            {
                Log.Error("Unable to find patch location");
            }
        }

        static void hookOrbDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<HealthComponent>(_ => _.TakeDamage(default)))))
            {
                int damageInfoLocalIndex = -1;
                if (!c.Clone().TryGotoPrev(x => x.MatchLdloc(out damageInfoLocalIndex) && il.Method.Body.Variables[damageInfoLocalIndex].VariableType.Is(typeof(DamageInfo))))
                {
                    Log.Error($"({il.Method.FullName}) Unable to find damageInfo local index");
                    return;
                }

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, damageInfoLocalIndex);
                c.EmitDelegate(applySniperDamage);
                static void applySniperDamage(Orb orbInstance, DamageInfo damageInfo)
                {
                    if (!orbInstance.target || !orbInstance.target.isSniperTarget)
                        return;

                    if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(AllAttacksSniper.EffectInfo))
                    {
                        damageInfo.crit = true;
                        damageInfo.damageColorIndex = DamageColorIndex.Sniper;

                        EffectData effectData = new EffectData
                        {
                            origin = orbInstance.target.transform.position,
                            rotation = Util.QuaternionSafeLookRotation(orbInstance.origin - orbInstance.target.transform.position)
                        };
                        effectData.SetHurtBoxReference(orbInstance.target);

                        EffectManager.SpawnEffect(sniperTargetHitEffect, effectData, true);
                    }
                }
            }
            else
            {
                Log.Error($"({il.Method.FullName}) Unable to find patch location");
            }
        }
    }
}
