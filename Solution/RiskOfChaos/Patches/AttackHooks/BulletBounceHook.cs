using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2BepInExPack.Utilities;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    static class BulletBounceHook
    {
        record class BulletBounceInfo(int BulletMuzzleIndex, AttackHookMask UsedAttackHooks, BulletAttack.BulletHit LastHit, int MaxBounces, int BouncesCompleted)
        {
            public int BouncesRemaining => Mathf.Max(0, MaxBounces - BouncesCompleted);
        }

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

        [SystemInitializer]
        static void Init()
        {
            OverrideBulletTracerOriginExplicitPatch.UseExplicitOriginPosition += bulletAttack => _bulletBounceInfos.TryGetValue(bulletAttack, out BulletBounceInfo bounceInfo) && bounceInfo.BouncesCompleted > 0;
        }

        public static bool TryStartBounce(BulletAttack bulletAttack, Vector3 normal, int muzzleIndex, AttackHookMask activeAttackHooks)
        {
            _bulletBounceInfos.Remove(bulletAttack);

            if (!isEnabled)
                return false;

            BulletBounceInfo bounceInfo = new BulletBounceInfo(muzzleIndex, activeAttackHooks, null, bounceCount, 0);
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
            if (_bulletBounceInfos.TryGetValue(bulletAttack, out BulletBounceInfo bounceInfo))
            {
                _bulletBounceInfos.Remove(bulletAttack);

                if (bounceInfo.BouncesRemaining > 0)
                {
                    float bounceMaxDistance = bulletAttack.maxDistance - hitInfo.distance;
                    if (bounceMaxDistance > 0f)
                    {
                        BulletAttack bounceBulletAttack = AttackUtils.Clone(bulletAttack);
                        bounceBulletAttack.origin = hitInfo.point;
                        bounceBulletAttack.maxDistance = bounceMaxDistance;

                        BulletBounceInfo bouncedBounceInfo = new BulletBounceInfo(bounceInfo.BulletMuzzleIndex, bounceInfo.UsedAttackHooks, hitInfo, bounceInfo.MaxBounces, bounceInfo.BouncesCompleted + 1);

                        _bulletBounceInfos.Add(bounceBulletAttack, bouncedBounceInfo);

                        fireBounceBullet(bounceBulletAttack, bouncedBounceInfo);
                    }
                }
            }
        }

        static bool bounceFilterCallback(BulletAttack bulletAttack, BulletAttack.BulletHit hitInfo)
        {
            if (_bulletBounceInfos.TryGetValue(bulletAttack, out BulletBounceInfo bounceInfo))
            {
                BulletAttack.BulletHit lastHit = bounceInfo.LastHit;
                if (lastHit != null)
                {
                    if (lastHit.hitHurtBox && lastHit.hitHurtBox.healthComponent &&
                        hitInfo.hitHurtBox && hitInfo.hitHurtBox.healthComponent &&
                        lastHit.hitHurtBox.healthComponent == hitInfo.hitHurtBox.healthComponent)
                    {
                        const float MIN_DISTANCE_TO_ALLOW = 1f;
                        const float MIN_SQR_DISTANCE_TO_ALLOW = MIN_DISTANCE_TO_ALLOW * MIN_DISTANCE_TO_ALLOW;
                        return (lastHit.point - hitInfo.point).sqrMagnitude >= MIN_SQR_DISTANCE_TO_ALLOW;
                    }
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

                float maxAutoAimAngle = Mathf.Min(45f, bounceInfo.BouncesCompleted * 7.5f);
                if (maxAutoAimAngle > 0f)
                {
                    BullseyeSearch autoAimSearch = new BullseyeSearch
                    {
                        searchOrigin = bulletAttack.origin,
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

                    HurtBox overrideTargetHurtBox = autoAimSearch.GetResults().FirstOrDefault(h => HurtBox.FindEntityObject(h) != lastHit.entityObject);
                    if (overrideTargetHurtBox)
                    {
                        bounceDirection = (overrideTargetHurtBox.randomVolumePoint - bulletAttack.origin).normalized;
                    }
                }
            }

            bulletAttack.aimVector = bounceDirection;

            AttackHookManager.Context.Activate(bounceInfo.UsedAttackHooks | AttackHookMask.Bounced);
            bulletAttack.FireSingle(bounceDirection, bounceInfo.BulletMuzzleIndex);
        }
    }
}
