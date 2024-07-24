using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class OrbBounceHook
    {
        class OrbBounceChain
        {
            public readonly Xoroshiro128Plus RNG;

            public readonly int MaxBounces;

            public int CompletedBounces { get; private set; }

            public int BouncesRemaining => MaxBounces - CompletedBounces;

            readonly List<HealthComponent> _hitOrder;

            public readonly ReadOnlyCollection<HealthComponent> HitEntities;

            public readonly HashSet<HealthComponent> UniqueHitEntities;

            public readonly HashSet<HealthComponent> UsedDeadBacktrackEntities;

            public int CurrentDeadBacktrackCount;

            public OrbBounceChain(int maxBounces, Xoroshiro128Plus rng)
            {
                RNG = rng;

                MaxBounces = maxBounces;
                CompletedBounces = 0;

                _hitOrder = new List<HealthComponent>(MaxBounces);
                HitEntities = _hitOrder.AsReadOnly();

                UniqueHitEntities = [];

                UsedDeadBacktrackEntities = [];
            }

            public void RecordHit(HealthComponent hitEntity)
            {
                _hitOrder.Add(hitEntity);
                UniqueHitEntities.Add(hitEntity);
                CompletedBounces++;
            }
        }

        readonly record struct OrbTargetCandidate(HurtBox Target, float SqrDistance, float Weight);

        static readonly RaycastHit[] _sharedOrbTargetSearchRaycastHitsBuffer = new RaycastHit[32];

        static bool isEnabled => NetworkServer.active && bounceCount > 0;

        static int bounceCount
        {
            get
            {
                if (ProjectileModificationManager.Instance)
                {
                    return ClampedConversion.Int32(ProjectileModificationManager.Instance.OrbBounceCount);
                }
                else
                {
                    return 0;
                }
            }
        }

        static readonly Dictionary<Orb, OrbBounceChain> _orbBounceChains = [];

        static void clearBouncesRemaining()
        {
            _orbBounceChains.Clear();
        }

        [SystemInitializer]
        static void Init()
        {
            Run.onRunDestroyGlobal += _ =>
            {
                clearBouncesRemaining();
            };

            Stage.onServerStageBegin += _ =>
            {
                clearBouncesRemaining();
            };

            IL.RoR2.Orbs.OrbManager.FixedUpdate += hookOrbArrival;
            IL.RoR2.Orbs.OrbManager.ForceImmediateArrival += hookOrbArrival;
        }

        static void hookOrbArrival(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<Orb>(nameof(Orb.OnArrival))))
            {
                c.Emit(OpCodes.Dup);
                c.EmitDelegate(tryBounceOrb);

                c.Index++;
            }
        }

        static void tryBounceOrb(Orb orbInstance)
        {
            if (orbInstance == null || OrbUtils.IsTransferOrb(orbInstance) || orbInstance is VoidLightningOrb)
                return;

            if (_orbBounceChains.TryGetValue(orbInstance, out OrbBounceChain bounceChain))
            {
                _orbBounceChains.Remove(orbInstance);
                if (bounceChain.BouncesRemaining <= 0)
                    return;
            }
            else
            {
                bounceChain = null;
            }

            if (!isEnabled || !OrbManager.instance || !orbInstance.target)
                return;

            if (orbInstance.TryGetProcChainMask(out ProcChainMask orbProcChain) && !orbProcChain.Equals(default))
                return;

            if (bounceChain == null)
            {
                bounceChain = new OrbBounceChain(bounceCount, new Xoroshiro128Plus(RoR2Application.rng.nextUlong));
            }

            bounceChain.RecordHit(orbInstance.target.healthComponent);

            Vector3 oldOrbTargetPosition = orbInstance.target.transform.position;

            Vector3 newOrbTargetSearchPosition;
            if (orbInstance.target.healthComponent && orbInstance.target.healthComponent.body)
            {
                newOrbTargetSearchPosition = orbInstance.target.healthComponent.body.corePosition;
            }
            else
            {
                newOrbTargetSearchPosition = oldOrbTargetPosition;
            }

            if (!orbInstance.TryGetTeamIndex(out TeamIndex orbTeam))
            {
                orbTeam = TeamIndex.None;
            }

            CharacterBody attackerBody = orbInstance.GetAttacker();
            if (attackerBody)
            {
                if (orbTeam == TeamIndex.None || orbTeam == TeamIndex.Neutral)
                {
                    TeamIndex attackerTeam = attackerBody.teamComponent.teamIndex;
                    if (attackerTeam != TeamIndex.None && attackerTeam != TeamIndex.Neutral)
                    {
                        orbTeam = attackerTeam;
                    }
                }
            }

            float targetSearchDistance;
            switch (orbInstance)
            {
                case LightningOrb lightningOrb when lightningOrb.range > 0f:
                    targetSearchDistance = lightningOrb.range;
                    break;
                case ChainGunOrb chainGunOrb when chainGunOrb.bounceRange > 0f:
                    targetSearchDistance = chainGunOrb.bounceRange;
                    break;
                case HuntressArrowOrb when attackerBody && attackerBody.TryGetComponent(out HuntressTracker huntressTracker) && huntressTracker.maxTrackingDistance > 0f:
                    targetSearchDistance = huntressTracker.maxTrackingDistance;
                    break;
                default:
                    float estimatedOrbTravelDistance = Vector3.Distance(newOrbTargetSearchPosition, orbInstance.origin);
                    targetSearchDistance = Mathf.Max(20f, estimatedOrbTravelDistance * 1.5f);
                    break;
            }

            SphereSearch newTargetSearch = new SphereSearch
            {
                origin = newOrbTargetSearchPosition,
                radius = targetSearchDistance,
                queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                mask = LayerIndex.entityPrecise.mask
            };

            newTargetSearch.RefreshCandidates();

            newTargetSearch.FilterCandidatesByDistinctHurtBoxEntities();

            TeamMask targetSearchTeamMask;
            if (orbTeam != TeamIndex.None)
            {
                targetSearchTeamMask = TeamMask.GetEnemyTeams(orbTeam);
            }
            else
            {
                targetSearchTeamMask = new TeamMask();
                targetSearchTeamMask.AddTeam(orbInstance.target.teamIndex);
            }

            newTargetSearch.FilterCandidatesByHurtBoxTeam(targetSearchTeamMask);

            HurtBox[] potentialTargets = newTargetSearch.GetHurtBoxes();

            List<OrbTargetCandidate> targetCandidates = new List<OrbTargetCandidate>(potentialTargets.Length);

            foreach (HurtBox potentialTarget in potentialTargets)
            {
                if (potentialTarget.healthComponent == orbInstance.target.healthComponent)
                    continue;

                CharacterBody targetBody = potentialTarget.healthComponent.body;

                Vector3 targetCorePosition;
                if (targetBody)
                {
                    if (attackerBody)
                    {
                        if (attackerBody == targetBody)
                            continue;

                        if (targetBody.GetVisibilityLevel(attackerBody) < VisibilityLevel.Revealed)
                            continue;
                    }

                    targetCorePosition = targetBody.corePosition;
                }
                else
                {
                    targetCorePosition = potentialTarget.transform.position;
                }

                Vector3 losSearchDirection = targetCorePosition - newTargetSearch.origin;
                float targetDistance = losSearchDirection.magnitude;

                int hitCount = Physics.RaycastNonAlloc(new Ray(newTargetSearch.origin, losSearchDirection), _sharedOrbTargetSearchRaycastHitsBuffer, targetDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);

                bool losBlocked = false;
                for (int i = 0; i < hitCount; i++)
                {
                    Transform hitTransform = _sharedOrbTargetSearchRaycastHitsBuffer[i].transform;
                    if (!hitTransform)
                        continue;

                    if (!hitTransform.GetComponent<HurtBox>())
                    {
                        losBlocked = true;
                        break;
                    }
                }

                if (losBlocked)
                    continue;

                targetCandidates.Add(new OrbTargetCandidate(potentialTarget, targetDistance * targetDistance, 1f));
            }

            float sqrTargetSearchDistance = newTargetSearch.radius * newTargetSearch.radius;

            bool isDeadBacktrack = false;
            if (targetCandidates.Count == 0)
            {
                int uniqueHitCount = bounceChain.UniqueHitEntities.Count;
                List<OrbTargetCandidate> livingCandidates = new List<OrbTargetCandidate>(uniqueHitCount);
                List<OrbTargetCandidate> deadCandidates = new List<OrbTargetCandidate>(uniqueHitCount);

                foreach (HealthComponent hitHealthComponent in bounceChain.UniqueHitEntities)
                {
                    if (!hitHealthComponent || hitHealthComponent == orbInstance.target.healthComponent)
                        continue;

                    if (!hitHealthComponent.alive && bounceChain.UsedDeadBacktrackEntities.Contains(hitHealthComponent))
                        continue;

                    CharacterBody hitBody = hitHealthComponent.body;
                    if (!hitBody)
                        continue;

                    HurtBoxGroup hitHurtBoxGroup = hitBody.hurtBoxGroup;
                    if (!hitHurtBoxGroup)
                        continue;

                    HurtBox[] hitCharacterHurtBoxes = hitHurtBoxGroup.hurtBoxes;
                    if (hitCharacterHurtBoxes.Length == 0)
                        continue;

                    Vector3 hitPosition = hitBody.corePosition;

                    float hitSqrDistance = (hitPosition - newTargetSearch.origin).sqrMagnitude;
                    if (hitSqrDistance <= sqrTargetSearchDistance * (1.5f * 1.5f))
                    {
                        List<OrbTargetCandidate> candidatesList = hitHealthComponent.alive ? livingCandidates : deadCandidates;

                        HurtBox target = bounceChain.RNG.NextElementUniform(hitCharacterHurtBoxes);
                        candidatesList.Add(new OrbTargetCandidate(target, hitSqrDistance, 1f));
                    }
                }

                if (livingCandidates.Count > 0)
                {
                    targetCandidates = livingCandidates;
                }
                else if (deadCandidates.Count > 0 && bounceChain.CurrentDeadBacktrackCount < 1)
                {
                    targetCandidates = deadCandidates;
                    isDeadBacktrack = true;
                }
                else
                {
                    return;
                }
            }

            if (isDeadBacktrack)
            {
                bounceChain.CurrentDeadBacktrackCount++;
            }
            else
            {
                bounceChain.CurrentDeadBacktrackCount = 0;
            }

            WeightedSelection<HurtBox> targetSelection = new WeightedSelection<HurtBox>(Mathf.Max(8, targetCandidates.Count));
            foreach (OrbTargetCandidate candidate in targetCandidates)
            {
                float normalizedSqrDistance = Mathf.Clamp01(candidate.SqrDistance / sqrTargetSearchDistance);

                float effectiveTimesBouncedOnCandidate = 0;
                for (int i = 0; i < bounceChain.HitEntities.Count; i++)
                {
                    if (bounceChain.HitEntities[i] == candidate.Target.healthComponent)
                    {
                        float normalizedHitIndex = (bounceChain.HitEntities.Count - i) / (float)bounceChain.HitEntities.Count;
                        effectiveTimesBouncedOnCandidate += 1f - normalizedHitIndex;
                    }
                }

                const float MIN_DISTANCE_MULTIPLIER = 0.25f;
                float distanceWeightMultiplier = (Mathf.Pow(1f - normalizedSqrDistance, 2f) * (1f - MIN_DISTANCE_MULTIPLIER)) + MIN_DISTANCE_MULTIPLIER;

                float duplicateBounceWeightMultiplier = Mathf.Pow(2f, -effectiveTimesBouncedOnCandidate / 2f);

                targetSelection.AddChoice(candidate.Target, candidate.Weight * distanceWeightMultiplier * duplicateBounceWeightMultiplier);
            }

            HurtBox newTarget = targetSelection.GetRandom(bounceChain.RNG);

            if (newTarget && newTarget.healthComponent)
            {
                CharacterBody targetBody = newTarget.healthComponent.body;
                if (targetBody)
                {
                    HurtBoxGroup targetHurtBoxGroup = targetBody.hurtBoxGroup;
                    if (targetHurtBoxGroup && targetHurtBoxGroup.hurtBoxes.Length > 1)
                    {
                        newTarget = bounceChain.RNG.NextElementUniform(targetHurtBoxGroup.hurtBoxes);
                    }
                }

                if (isDeadBacktrack)
                {
                    if (!bounceChain.UsedDeadBacktrackEntities.Add(newTarget.healthComponent))
                    {
                        Log.Warning($"Duplicate backtrack target: {newTarget.healthComponent}");
                    }
                }
            }

            Orb newOrb = OrbUtils.Clone(orbInstance);

            newOrb.origin = oldOrbTargetPosition;
            newOrb.target = newTarget;

            _orbBounceChains.Add(newOrb, bounceChain);

            OrbManager.instance.AddOrb(newOrb);
        }
    }
}
