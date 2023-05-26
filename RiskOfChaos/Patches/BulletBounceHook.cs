using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.Projectile;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class BulletBounceHook
    {
        static bool isEnabled => bounceCount > 0;

        static int bounceCount
        {
            get
            {
                if (ProjectileModificationManager.Instance)
                {
                    return (int)ProjectileModificationManager.Instance.NetworkedBulletBounceCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        static int _currentBulletBounceDepth;
        static int _currentBulletBouncesRemaining;
        static BulletAttack.BulletHit? _currentBounceSourceHitInfo;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle_CreateBounceBullet;
            IL.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle_FixTracerEffectOrigin;

            On.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle;

            On.RoR2.BulletAttack.DefaultFilterCallbackImplementation += BulletAttack_DefaultFilterCallbackImplementation_IgnoreBounceSource;
        }

        static void BulletAttack_FireSingle(On.RoR2.BulletAttack.orig_FireSingle orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            bool isFirstBulletInBounceChain = isEnabled && _currentBulletBounceDepth <= 0;
            if (isFirstBulletInBounceChain)
            {
                _currentBulletBouncesRemaining = bounceCount;
            }

            // Ensure all values are reset afterwards even in the case of an exception
            try
            {
                orig(self, normal, muzzleIndex);
            }
            finally
            {
                if (isFirstBulletInBounceChain || !isEnabled)
                {
                    // The bullet may have bounced less than the max count, such as if it bounces in a direction with no hit point (sky)
                    _currentBulletBouncesRemaining = 0;

                    // Should be properly reset by the bounce hook, bust just in case
                    _currentBulletBounceDepth = 0;
                }

                _currentBounceSourceHitInfo = null;
            }
        }

        static void BulletAttack_FireSingle_CreateBounceBullet(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int hitListLocalIndex = -1;
            if (c.TryFindNext(out _,
                              x => x.MatchNewobj<List<BulletAttack.BulletHit>>(),
                              x => x.MatchStloc(out hitListLocalIndex)))
            {
                const string BULLET_ATTACK_PROCESS_HIT_LIST_NAME = nameof(BulletAttack.ProcessHitList);
                if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<BulletAttack>(BULLET_ATTACK_PROCESS_HIT_LIST_NAME)))
                {
                    int hitPositionLocalIndex = -1;
                    if (new ILCursor(c).TryGotoPrev(x => x.MatchLdloca(out hitPositionLocalIndex)))
                    {
                        c.Emit(OpCodes.Dup);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldarg_1);
                        c.Emit(OpCodes.Ldarg_2);
                        c.Emit(OpCodes.Ldloc, hitPositionLocalIndex);
                        c.Emit(OpCodes.Ldloc, hitListLocalIndex);
                        c.EmitDelegate((GameObject hitEntity, BulletAttack instance, Vector3 fireDirection, int muzzleIndex, Vector3 hitPosition, List<BulletAttack.BulletHit> hitList) =>
                        {
                            if (!isEnabled || _currentBulletBouncesRemaining <= 0)
                                return;

                            if (!hitEntity) // If the bullet hit nothing, bouncing can't happen
                                return;

                            foreach (BulletAttack.BulletHit hit in hitList)
                            {
                                if (hit.entityObject == hitEntity && hit.point == hitPosition)
                                {
                                    _currentBulletBouncesRemaining--;

                                    _currentBulletBounceDepth++;

                                    _currentBounceSourceHitInfo = hit;

                                    Vector3 oldBulletOrigin = instance.origin;
                                    instance.origin = hit.point;

                                    instance.maxDistance -= hit.distance;
                                    if (instance.maxDistance > 0f)
                                    {
                                        Vector3 bounceDirection = Vector3.Reflect(fireDirection, hit.surfaceNormal);

                                        // Slight "homing" on bullets to make it easier to bounce off walls and still hit
                                        if (instance.owner)
                                        {
                                            CharacterBody ownerBody = instance.owner.GetComponent<CharacterBody>();

                                            if (ownerBody.isPlayerControlled)
                                            {
                                                BullseyeSearch bullseyeSearch = new BullseyeSearch
                                                {
                                                    searchOrigin = instance.origin,
                                                    filterByLoS = true,
                                                    minDistanceFilter = 0f,
                                                    maxDistanceFilter = instance.maxDistance,
                                                    minAngleFilter = 0f,
                                                    maxAngleFilter = 35f,
                                                    searchDirection = bounceDirection,
                                                    queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                                                    sortMode = BullseyeSearch.SortMode.Angle,
                                                    viewer = ownerBody,
                                                    teamMaskFilter = TeamMask.allButNeutral
                                                };

                                                bullseyeSearch.teamMaskFilter.RemoveTeam(TeamComponent.GetObjectTeam(instance.owner));

                                                bullseyeSearch.RefreshCandidates();

                                                HurtBox overrideTargetHurtBox = bullseyeSearch.GetResults().FirstOrDefault(h => h.healthComponent.gameObject != hit.entityObject);
                                                if (overrideTargetHurtBox)
                                                {
                                                    bounceDirection = (overrideTargetHurtBox.randomVolumePoint - instance.origin).normalized;
                                                }
                                            }
                                        }

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                                        instance.FireSingle(bounceDirection, muzzleIndex);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                                    }

                                    instance.origin = oldBulletOrigin;

                                    _currentBounceSourceHitInfo = null;

                                    _currentBulletBounceDepth--;
                                    return;
                                }
                            }
                        });
                    }
                    else
                    {
                        Log.Error("Failed to find hitPosition local index");
                    }
                }
                else
                {
                    Log.Error("Failed to find patch location");
                }
            }
            else
            {
                Log.Error("Failed to find hitList local index");
            }
        }

        static void BulletAttack_FireSingle_FixTracerEffectOrigin(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetChildLocatorTransformReference))))
            {
                if (c.TryGotoPrev(MoveType.After, x => x.MatchLdfld<BulletAttack>(nameof(BulletAttack.weapon))))
                {
                    c.EmitDelegate((GameObject weapon) =>
                    {
                        // If an object reference is passed here, the tracer effect origin will always be at the barrel of the gun that fired it, so just pretend it doesn't exist if the current bullet is bounced :)
                        return _currentBulletBounceDepth > 0 ? null : weapon;
                    });
                }
                else
                {
                    Log.Error("Failed to find weapon ldfld");
                }
            }
            else
            {
                Log.Error("Failed to find EffectData.SetChildLocatorTransformReference call");
            }
        }

        static bool BulletAttack_DefaultFilterCallbackImplementation_IgnoreBounceSource(On.RoR2.BulletAttack.orig_DefaultFilterCallbackImplementation orig, BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            if (!orig(bulletAttack, ref hitInfo))
                return false;

            if (isEnabled && _currentBounceSourceHitInfo.HasValue)
            {
                if (_currentBounceSourceHitInfo.Value.collider == hitInfo.collider || _currentBounceSourceHitInfo.Value.entityObject == hitInfo.entityObject)
                {
                    const float MIN_DISTANCE_TO_ALLOW = 1f;
                    const float MIN_SQR_DISTANCE_TO_ALLOW = MIN_DISTANCE_TO_ALLOW * MIN_DISTANCE_TO_ALLOW;
                    return (_currentBounceSourceHitInfo.Value.point - hitInfo.point).sqrMagnitude >= MIN_SQR_DISTANCE_TO_ALLOW;
                }
            }

            return true;
        }
    }
}
