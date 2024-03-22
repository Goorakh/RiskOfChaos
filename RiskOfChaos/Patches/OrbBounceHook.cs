using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class OrbBounceHook
    {
        static bool isEnabled => NetworkServer.active && bounceCount > 0;

        static int bounceCount
        {
            get
            {
                if (ProjectileModificationManager.Instance)
                {
                    return (int)ProjectileModificationManager.Instance.NetworkedOrbBounceCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        static readonly Dictionary<Orb, int> _orbBouncesRemaining = [];

        [SystemInitializer]
        static void Init()
        {
            Run.onRunStartGlobal += _ =>
            {
                _orbBouncesRemaining.Clear();
            };

            Stage.onServerStageComplete += _ =>
            {
                _orbBouncesRemaining.Clear();
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
            if (!isEnabled || !OrbManager.instance || orbInstance == null)
                return;

            if (OrbUtils.IsTransferOrb(orbInstance) || orbInstance is VoidLightningOrb)
                return;

            if (_orbBouncesRemaining.TryGetValue(orbInstance, out int bouncesRemaining))
            {
                _orbBouncesRemaining.Remove(orbInstance);
                bouncesRemaining--;

                if (bouncesRemaining <= 0)
                {
                    return;
                }
            }
            else
            {
                bouncesRemaining = bounceCount;
            }

            if (!orbInstance.target)
                return;

            Vector3 newOrbOrigin = orbInstance.target.transform.position;

            SphereSearch newTargetSearch = new SphereSearch
            {
                origin = newOrbOrigin,
                radius = 75f,
                queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                mask = LayerIndex.entityPrecise.mask
            };

            newTargetSearch.RefreshCandidates();

            newTargetSearch.FilterCandidatesByDistinctHurtBoxEntities();

            TeamMask teamMask = new TeamMask();
            teamMask.AddTeam(orbInstance.target.teamIndex);
            newTargetSearch.FilterCandidatesByHurtBoxTeam(teamMask);

            newTargetSearch.OrderCandidatesByDistance();

            CharacterBody attackerBody = orbInstance.GetAttacker();

            List<HurtBox> validTargets = newTargetSearch.GetHurtBoxes().Where(h =>
            {
                if (h.healthComponent == orbInstance.target.healthComponent)
                    return false;

                Vector3 losCheckDirection = h.transform.position - newTargetSearch.origin;
                if (Physics.Raycast(newTargetSearch.origin, losCheckDirection, out _, losCheckDirection.magnitude, LayerIndex.world.mask, newTargetSearch.queryTriggerInteraction))
                {
                    return false;
                }

                if (attackerBody)
                {
                    CharacterBody targetBody = h.healthComponent.body;
                    if (targetBody && targetBody.GetVisibilityLevel(attackerBody) < VisibilityLevel.Revealed)
                        return false;
                }

                return true;
            }).ToList();

            if (validTargets.Count == 0)
                return;

            float targetIndexFraction = RoR2Application.rng.nextNormalizedFloat;
            HurtBox newTarget = validTargets[Mathf.RoundToInt(Mathf.Pow(targetIndexFraction, 4f) * (validTargets.Count - 1))];

            Orb newOrb = OrbUtils.Clone(orbInstance);

            newOrb.origin = newOrbOrigin;
            newOrb.target = newTarget;

            _orbBouncesRemaining[newOrb] = bouncesRemaining;

            OrbManager.instance.AddOrb(newOrb);
        }
    }
}
