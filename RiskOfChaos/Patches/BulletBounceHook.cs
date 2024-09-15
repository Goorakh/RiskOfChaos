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
                    return (int)ProjectileModificationManager.Instance.BulletBounceCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        static int _currentBulletBounceDepth;
        static int currentBulletBouncesRemaining => bounceCount - _currentBulletBounceDepth;

        static BulletAttack.BulletHit _currentBounceSourceHitInfo;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle_CreateBounceBullet;

            On.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle;

            On.RoR2.BulletAttack.DefaultFilterCallbackImplementation += BulletAttack_DefaultFilterCallbackImplementation_IgnoreBounceSource;

            OverrideBulletTracerOriginExplicitPatch.UseExplicitOriginPosition += _ => _currentBulletBounceDepth > 0;
        }

        static void BulletAttack_FireSingle(On.RoR2.BulletAttack.orig_FireSingle orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            bool isFirstBulletInBounceChain = isEnabled && _currentBulletBounceDepth <= 0;
            if (isFirstBulletInBounceChain)
            {
                _currentBulletBounceDepth = 0;
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
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchCallOrCallvirt<BulletAttack>(nameof(BulletAttack.ProcessHitList))))
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
                        c.EmitDelegate(fireBulletBounce);
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

        static void fireBulletBounce(GameObject hitEntity, BulletAttack instance, Vector3 fireDirection, int muzzleIndex, Vector3 hitPosition, List<BulletAttack.BulletHit> hitList)
        {
            if (!isEnabled || currentBulletBouncesRemaining <= 0)
                return;

            if (!hitEntity) // If the bullet hit nothing, bouncing can't happen
                return;

            foreach (BulletAttack.BulletHit hit in hitList)
            {
                if (hit != null && hit.entityObject == hitEntity && hit.point == hitPosition)
                {
                    _currentBulletBounceDepth++;

                    _currentBounceSourceHitInfo = hit;

                    Vector3 oldBulletOrigin = instance.origin;
                    instance.origin = hit.point;

                    instance.maxDistance -= hit.distance;
                    if (instance.maxDistance > 0f)
                    {
                        Vector3 bounceDirection = Vector3.Reflect(fireDirection, hit.surfaceNormal);

                        // Slight "homing" on bullets to make it easier to bounce off walls and still hit
                        if (instance.owner && instance.owner.TryGetComponent(out CharacterBody ownerBody) && ownerBody.isPlayerControlled)
                        {
                            TeamMask searchTeamMask = TeamMask.GetEnemyTeams(TeamComponent.GetObjectTeam(instance.owner));
                            searchTeamMask.RemoveTeam(TeamIndex.Neutral);

                            BullseyeSearch autoAimSearch = new BullseyeSearch
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
                                teamMaskFilter = searchTeamMask,
                            };

                            autoAimSearch.RefreshCandidates();

                            HurtBox overrideTargetHurtBox = autoAimSearch.GetResults().FirstOrDefault(h => HurtBox.FindEntityObject(h) != hit.entityObject);
                            if (overrideTargetHurtBox)
                            {
                                bounceDirection = (overrideTargetHurtBox.randomVolumePoint - instance.origin).normalized;
                            }
                        }

                        instance.FireSingle(bounceDirection, muzzleIndex);
                    }

                    instance.origin = oldBulletOrigin;

                    _currentBounceSourceHitInfo = null;

                    _currentBulletBounceDepth--;
                    break;
                }
            }
        }

        static bool BulletAttack_DefaultFilterCallbackImplementation_IgnoreBounceSource(On.RoR2.BulletAttack.orig_DefaultFilterCallbackImplementation orig, BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            if (!orig(bulletAttack, ref hitInfo))
                return false;

            if (isEnabled && _currentBounceSourceHitInfo != null)
            {
                if (_currentBounceSourceHitInfo.collider == hitInfo.collider || _currentBounceSourceHitInfo.entityObject == hitInfo.entityObject)
                {
                    const float MIN_DISTANCE_TO_ALLOW = 1f;
                    const float MIN_SQR_DISTANCE_TO_ALLOW = MIN_DISTANCE_TO_ALLOW * MIN_DISTANCE_TO_ALLOW;
                    return (_currentBounceSourceHitInfo.point - hitInfo.point).sqrMagnitude >= MIN_SQR_DISTANCE_TO_ALLOW;
                }
            }

            return true;
        }
    }
}
