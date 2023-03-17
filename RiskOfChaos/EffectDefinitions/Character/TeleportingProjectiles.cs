using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("teleporting_projectiles", DefaultSelectionWeight = 0.7f, EffectActivationCountHardCap = 1)]
    public class TeleportingProjectiles : TimedEffect
    {
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
        static bool _effectActive = false;

        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            On.RoR2.GlobalEventManager.OnHitAll += (orig, self, damageInfo, hitObject) =>
            {
                orig(self, damageInfo, hitObject);

                if (!NetworkServer.active)
                    return;

                if (!_effectActive)
                    return;

                GameObject attackerObj = damageInfo.attacker;
                if (!attackerObj)
                    return;

                CharacterBody attackerBody = attackerObj.GetComponent<CharacterBody>();
                if (!attackerBody)
                    return;

                CharacterMaster master = attackerBody.master;
                if (!master || Array.IndexOf(_teleportBlacklist, master.masterIndex) != -1)
                    return;

                TeleportUtils.TeleportBody(attackerBody, damageInfo.position);
            };

            _appliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();
            _effectActive = true;
        }

        public override void OnEnd()
        {
            _effectActive = false;
        }
    }
}
