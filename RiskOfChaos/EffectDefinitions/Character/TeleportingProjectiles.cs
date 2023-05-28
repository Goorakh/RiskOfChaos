using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("teleporting_projectiles", DefaultSelectionWeight = 0.7f, IsNetworked = true)]
    [ChaosTimedEffect(30f, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: Teleporting Attacks (Lasts until next effect)")]
    public sealed class TeleportingProjectiles : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static MasterCatalog.MasterIndex[] _teleportBlacklist = Array.Empty<MasterCatalog.MasterIndex>();

        [SystemInitializer(typeof(MasterCatalog))]
        static void InitMasterBlacklist()
        {
            _teleportBlacklist = new MasterCatalog.MasterIndex[]
            {
                MasterCatalog.FindMasterIndex("ArtifactShellMaster"),
                MasterCatalog.FindMasterIndex("BrotherHauntMaster")
            }.Where(i => i.isValid)
             .ToArray();
        }

        static bool _appliedPatches = false;

        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            On.RoR2.GlobalEventManager.OnHitAll += (orig, self, damageInfo, hitObject) =>
            {
                orig(self, damageInfo, hitObject);

                if (!NetworkServer.active)
                    return;

                tryTeleportAttacker(damageInfo.attacker, damageInfo.position);
            };

            On.RoR2.BlastAttack.Fire += (orig, self) =>
            {
                BlastAttack.Result result = orig(self);

                if (result.hitCount == 0)
                {
                    tryTeleportAttacker(self.attacker, self.position);
                }

                return result;
            };

            _appliedPatches = true;
        }

        static void tryTeleportAttacker(GameObject attackerObj, Vector3 teleportPosition)
        {
            if (!attackerObj)
                return;

            if (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(_effectInfo))
                return;

            CharacterBody attackerBody = attackerObj.GetComponent<CharacterBody>();
            if (!attackerBody)
                return;

            CharacterMaster master = attackerBody.master;
            if (!master || Array.IndexOf(_teleportBlacklist, master.masterIndex) != -1)
                return;

            /*
            const float MIN_TELEPORT_DISTANCE = 0.1f;
            float sqrTeleportDistance = (attackerBody.footPosition - teleportPosition).sqrMagnitude;
            if (sqrTeleportDistance < MIN_TELEPORT_DISTANCE * MIN_TELEPORT_DISTANCE)
            {
#if DEBUG
                Log.Debug($"Not teleporting {Util.GetBestBodyName(attackerObj)}: Teleport distance too short to consider ({Mathf.Sqrt(sqrTeleportDistance)} units)");
#endif
                return;
            }

#if DEBUG
            Log.Debug($"Teleporting {Util.GetBestBodyName(attackerObj)} {Mathf.Sqrt(sqrTeleportDistance)} units");
#endif
            */

            TeleportUtils.TeleportBody(attackerBody, teleportPosition);
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
