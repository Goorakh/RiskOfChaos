using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class BulletBounceHook
    {
        class BulletBounceInfo
        {
            public readonly BulletAttack BulletAttack;
            public Vector3 CurrentBulletDirection;
            public int MuzzleIndex;
            public BulletAttack.BulletHit CurrentBounceHit;
            public int BounceDepth;

            public int BouncesRemaining => bounceCount - BounceDepth;

            public BulletBounceInfo(BulletAttack bulletAttack, Vector3 currentBulletDirection, int muzzleIndex, BulletAttack.BulletHit currentBounceHit, int bounceDepth)
            {
                BulletAttack = bulletAttack;
                CurrentBulletDirection = currentBulletDirection;
                MuzzleIndex = muzzleIndex;
                CurrentBounceHit = currentBounceHit;
                BounceDepth = bounceDepth;
            }
        }

        static bool isEnabled => bounceCount > 0;

        static int bounceCount
        {
            get
            {
                if (ProjectileModificationManager.Instance)
                {
                    return ProjectileModificationManager.Instance.BulletBounceCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        static BulletBounceInfo _currentBounceInfo;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.BulletAttack.FireSingle += BulletAttack_InitializeBounce;
            IL.RoR2.BulletAttack.FireSingle_ReturnHit += BulletAttack_InitializeBounce;
            IL.RoR2.BulletAttack.FireMulti += BulletAttack_InitializeBounce;

            IL.RoR2.BulletAttack.ProcessHitList += BulletAttack_ProcessHitList_TryFireBounces;

            On.RoR2.BulletAttack.DefaultFilterCallbackImplementation += BulletAttack_DefaultFilterCallbackImplementation_IgnoreBounceSource;

            OverrideBulletTracerOriginExplicitPatch.UseExplicitOriginPosition += attack => _currentBounceInfo != null && _currentBounceInfo.BulletAttack == attack && _currentBounceInfo.BounceDepth > 0;
        }

        static void BulletAttack_InitializeBounce(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<Vector3>("normal", out ParameterDefinition normalParameter))
            {
                Log.Error("Failed to find normal parameter");
                return;
            }

            if (!il.Method.TryFindParameter<int>("muzzleIndex", out ParameterDefinition muzzleIndexParameter))
            {
                Log.Error("Failed to find muzzleIndex parameter");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg, normalParameter);
            c.Emit(OpCodes.Ldarg, muzzleIndexParameter);
            c.EmitDelegate(initBounce);
            static void initBounce(BulletAttack bulletAttack, Vector3 normal, int muzzleIndex)
            {
                if (!isEnabled)
                {
                    _currentBounceInfo = null;
                    return;
                }

                if (_currentBounceInfo != null)
                {
                    if (!ReferenceEquals(_currentBounceInfo.BulletAttack, bulletAttack))
                    {
                        Log.Warning("Current bounce belongs to a different BulletAttack instance");
                        _currentBounceInfo = null;
                    }
                }

                _currentBounceInfo ??= new BulletBounceInfo(bulletAttack, normal, muzzleIndex, null, 0);
            }
        }

        static void BulletAttack_ProcessHitList_TryFireBounces(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            VariableDefinition finalHitVar = il.AddVariable<BulletAttack.BulletHit>();

            c.Emit(OpCodes.Ldnull);
            c.Emit(OpCodes.Stloc, finalHitVar);

            MethodInfo bulletHitListGetter = typeof(List<BulletAttack.BulletHit>).GetProperty("Item").GetMethod;

            if (c.TryFindNext(out ILCursor[] cursors,
                              x => x.MatchCallOrCallvirt<BulletAttack>(nameof(BulletAttack.ProcessHit)),
                              x => x.MatchCallOrCallvirt(bulletHitListGetter)))
            {
                ILCursor getFinalHitCursor = cursors[1];

                getFinalHitCursor.Index++;
                getFinalHitCursor.EmitStoreStack(finalHitVar);
            }
            else
            {
                Log.Error("Failed to find hit patch location");
            }

            int retPatchCount = 0;

            c.Index = 0;
            while (c.TryGotoNext(MoveType.Before,
                                 x => x.MatchRet()))
            {
                c.Emit(OpCodes.Ldarg_0)
                 .Emit(OpCodes.Ldloc, finalHitVar)
                 .EmitDelegate(tryHandleBounce);

                static void tryHandleBounce(BulletAttack instance, BulletAttack.BulletHit hit)
                {
                    if (_currentBounceInfo == null)
                        return;

                    if (!isEnabled || hit == null || !hit.entityObject || _currentBounceInfo.BulletAttack != instance || _currentBounceInfo.BouncesRemaining <= 0)
                    {
                        _currentBounceInfo = null;
                        return;
                    }

                    _currentBounceInfo.CurrentBounceHit = hit;
                    tryFireBulletBounce(_currentBounceInfo);

                    if (_currentBounceInfo != null)
                    {
                        if (_currentBounceInfo.BouncesRemaining <= 0)
                        {
                            _currentBounceInfo = null;
                        }
                    }
                }

                c.SearchTarget = SearchTarget.Next;

                retPatchCount++;
            }

            if (retPatchCount == 0)
            {
                Log.Error("Found 0 ret patch locations");
            }
#if DEBUG
            else
            {
                Log.Debug($"Found {retPatchCount} ret patch location(s)");
            }
#endif
        }

        static void tryFireBulletBounce(BulletBounceInfo bounceInfo)
        {
            bounceInfo.BounceDepth++;

            BulletAttack bulletAttack = bounceInfo.BulletAttack;
            BulletAttack.BulletHit bulletHit = bounceInfo.CurrentBounceHit;

            Vector3 oldBulletOrigin = bulletAttack.origin;
            bulletAttack.origin = bulletHit.point;

            bulletAttack.maxDistance -= bulletHit.distance;
            if (bulletAttack.maxDistance > 0f)
            {
                Vector3 bounceDirection = Vector3.Reflect(bounceInfo.CurrentBulletDirection, bulletHit.surfaceNormal);

                // Slight "homing" on bullets to make it easier to bounce off walls and still hit
                if (bulletAttack.owner && bulletAttack.owner.TryGetComponent(out CharacterBody ownerBody) && ownerBody.isPlayerControlled)
                {
                    TeamMask searchTeamMask = TeamMask.GetEnemyTeams(TeamComponent.GetObjectTeam(bulletAttack.owner));
                    searchTeamMask.RemoveTeam(TeamIndex.Neutral);

                    BullseyeSearch autoAimSearch = new BullseyeSearch
                    {
                        searchOrigin = bulletAttack.origin,
                        filterByLoS = true,
                        minDistanceFilter = 0f,
                        maxDistanceFilter = bulletAttack.maxDistance,
                        minAngleFilter = 0f,
                        maxAngleFilter = 35f,
                        searchDirection = bounceDirection,
                        queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                        sortMode = BullseyeSearch.SortMode.Angle,
                        viewer = ownerBody,
                        teamMaskFilter = searchTeamMask,
                    };

                    autoAimSearch.RefreshCandidates();

                    HurtBox overrideTargetHurtBox = autoAimSearch.GetResults().FirstOrDefault(h => HurtBox.FindEntityObject(h) != bulletHit.entityObject);
                    if (overrideTargetHurtBox)
                    {
                        bounceDirection = (overrideTargetHurtBox.randomVolumePoint - bulletAttack.origin).normalized;
                    }
                }

                bulletAttack.FireSingle(bounceDirection, bounceInfo.MuzzleIndex);
            }

            bulletAttack.origin = oldBulletOrigin;
        }

        static bool BulletAttack_DefaultFilterCallbackImplementation_IgnoreBounceSource(On.RoR2.BulletAttack.orig_DefaultFilterCallbackImplementation orig, BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            bool result = orig(bulletAttack, ref hitInfo);
            if (result)
            {
                if (isEnabled && _currentBounceInfo != null)
                {
                    if (_currentBounceInfo.CurrentBounceHit == hitInfo)
                    {
                        const float MIN_DISTANCE_TO_ALLOW = 1f;
                        const float MIN_SQR_DISTANCE_TO_ALLOW = MIN_DISTANCE_TO_ALLOW * MIN_DISTANCE_TO_ALLOW;
                        return (_currentBounceInfo.CurrentBounceHit.point - hitInfo.point).sqrMagnitude >= MIN_SQR_DISTANCE_TO_ALLOW;
                    }
                }
            }

            return result;
        }
    }
}
