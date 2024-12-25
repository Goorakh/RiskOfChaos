using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2BepInExPack.Utilities;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    static class BulletBounceHook
    {
        public record class BulletBounceInfo(AttackInfo AttackInfo, BulletAttack.BulletHit LastHit, int MaxBounces, int BouncesCompleted, BulletBounceDelegate OnBounceHit)
        {
            public int BouncesRemaining => Mathf.Max(0, MaxBounces - BouncesCompleted);
        }

        public delegate void BulletBounceDelegate(BulletAttack bulletAttack, BulletBounceInfo bounceInfo);

        static bool isEnabled => bounceCount > 0;

        static int bounceCount
        {
            get
            {
                if (!ProjectileModificationManager.Instance)
                    return 0;

                return ProjectileModificationManager.Instance.BulletBounceCount;
            }
        }

        static readonly FixedConditionalWeakTable<BulletAttack, BulletBounceInfo> _bulletBounceInfos = new FixedConditionalWeakTable<BulletAttack, BulletBounceInfo>();

        public static bool TryStartBounce(BulletAttack bulletAttack, in AttackInfo attackInfo, BulletBounceDelegate onBounceHit = null)
        {
            if (!isEnabled)
                return false;

            if (bulletAttack.procChainMask.HasAnyProc())
                return false;

            if (_bulletBounceInfos.TryGetValue(bulletAttack, out _))
                return false;

            if (bulletAttack.procChainMask.HasModdedProc(CustomProcTypes.Bouncing) || bulletAttack.procChainMask.HasModdedProc(CustomProcTypes.BounceFinished))
                return false;

            bulletAttack.procChainMask.AddModdedProc(CustomProcTypes.Bouncing);

            BulletBounceInfo bounceInfo = new BulletBounceInfo(attackInfo, null, bounceCount, 0, onBounceHit);
            _bulletBounceInfos.Add(bulletAttack, bounceInfo);

            BulletAttack.HitCallback origHitCallback = bulletAttack.hitCallback;
            bulletAttack.hitCallback = tryBounce;
            bool tryBounce(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                bool stopBullet = origHitCallback(bulletAttack, ref hitInfo);

                if (!stopBullet)
                {
                    handleBulletBounce(bulletAttack, hitInfo);
                }

                bulletAttack.procChainMask.AddModdedProc(CustomProcTypes.BounceFinished);

                return stopBullet;
            }

            BulletAttack.FilterCallback origFilterCallback = bulletAttack.filterCallback;
            bulletAttack.filterCallback = filterCallback;
            bool filterCallback(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                return origFilterCallback(bulletAttack, ref hitInfo) && bounceFilterCallback(bulletAttack, hitInfo);
            }

            return true;
        }

        static void handleBulletBounce(BulletAttack bulletAttack, BulletAttack.BulletHit hitInfo)
        {
            Log.Debug($"BulletAttack from {Util.GetBestBodyName(bulletAttack.owner)} bouncing={_bulletBounceInfos.TryGetValue(bulletAttack, out _)}");

            if (_bulletBounceInfos.TryGetValue(bulletAttack, out BulletBounceInfo bounceInfo))
            {
                bounceInfo.Deconstruct(out AttackInfo attackInfo,
                                               out BulletAttack.BulletHit lastHit,
                                               out int maxBounces,
                                               out int bouncedCompleted,
                                               out BulletBounceDelegate onBounceHit);

                onBounceHit?.Invoke(bulletAttack, bounceInfo);

                if (bounceInfo.BouncesRemaining > 0)
                {
                    float bounceMaxDistance = bulletAttack.maxDistance - hitInfo.distance;
                    if (bounceMaxDistance > 0f)
                    {
                        BulletAttack bounceBulletAttack = AttackUtils.Clone(bulletAttack);
                        bounceBulletAttack.origin = hitInfo.point;
                        bounceBulletAttack.maxDistance = bounceMaxDistance;

                        bounceBulletAttack.weapon = null;
                        bounceBulletAttack.muzzleName = string.Empty;

                        BulletBounceInfo bouncedBounceInfo = new BulletBounceInfo(attackInfo, hitInfo, maxBounces, bouncedCompleted + 1, onBounceHit);

                        _bulletBounceInfos.Add(bounceBulletAttack, bouncedBounceInfo);

                        fireBounceBullet(bounceBulletAttack, bouncedBounceInfo);
                    }
                }

                _bulletBounceInfos.Remove(bulletAttack);
            }
        }

        static bool bounceFilterCallback(BulletAttack bulletAttack, BulletAttack.BulletHit currentHitInfo)
        {
            Log.Debug($"BulletAttack from {Util.GetBestBodyName(bulletAttack.owner)} bouncing={_bulletBounceInfos.TryGetValue(bulletAttack, out _)}");

            if (_bulletBounceInfos.TryGetValue(bulletAttack, out BulletBounceInfo bounceInfo))
            {
                BulletAttack.BulletHit lastHit = bounceInfo.LastHit;

                HurtBox currentHitEntity = currentHitInfo?.hitHurtBox;
                HurtBox lastHitEntity = lastHit?.hitHurtBox;

                if (currentHitEntity && lastHitEntity && currentHitEntity == lastHitEntity)
                {
                    return false;
                }
            }

            return true;
        }

        static void fireBounceBullet(BulletAttack bulletAttack, BulletBounceInfo bounceInfo)
        {
            BulletAttack.BulletHit lastHit = bounceInfo.LastHit;

            Vector3 bounceDirection = Vector3.Reflect(lastHit.direction, lastHit.surfaceNormal);

            // Slight "homing" on bullets to make it easier to bounce off walls and still hit
            if (bulletAttack.owner && bulletAttack.owner.TryGetComponent(out CharacterBody ownerBody) && ownerBody.isPlayerControlled)
            {
                TeamMask searchTeamMask = TeamMask.GetEnemyTeams(TeamComponent.GetObjectTeam(bulletAttack.owner));
                searchTeamMask.RemoveTeam(TeamIndex.Neutral);

                float maxAutoAimAngle = Mathf.Min(45f, bounceInfo.BouncesCompleted * 15f);
                if (maxAutoAimAngle > 0f)
                {
                    BullseyeSearch autoAimSearch = new BullseyeSearch
                    {
                        searchOrigin = lastHit.point,
                        filterByLoS = true,
                        minDistanceFilter = 0f,
                        maxDistanceFilter = bulletAttack.maxDistance,
                        minAngleFilter = 0f,
                        maxAngleFilter = maxAutoAimAngle,
                        searchDirection = bounceDirection,
                        queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                        sortMode = BullseyeSearch.SortMode.Angle,
                        viewer = ownerBody,
                        teamMaskFilter = searchTeamMask,
                    };

                    autoAimSearch.RefreshCandidates();

                    HurtBox overrideTargetHurtBox = autoAimSearch.GetResults().FirstOrDefault(h => h != lastHit.hitHurtBox);
                    if (overrideTargetHurtBox)
                    {
                        bounceDirection = (overrideTargetHurtBox.randomVolumePoint - autoAimSearch.searchOrigin).normalized;
                    }
                }
            }

            bulletAttack.aimVector = bounceDirection;

            bulletAttack.FireSingle(bounceDirection, -1);
        }
    }
}
