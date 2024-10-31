using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("teleporting_projectiles", 30f, AllowDuplicates = false, DefaultSelectionWeight = 0.7f)]
    [EffectConfigBackwardsCompatibility("Effect: Teleporting Attacks (Lasts until next effect)")]
    public sealed class TeleportingProjectiles : MonoBehaviour
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static readonly MasterIndexCollection _teleportBlacklist = new MasterIndexCollection("ArtifactShellMaster", "BrotherHauntMaster");

        public static void TryTeleportAttacker(GameObject attackerObj, Vector3 teleportPosition)
        {
            if (!attackerObj)
                return;

            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(_effectInfo))
                return;

            CharacterBody attackerBody = attackerObj.GetComponent<CharacterBody>();
            if (!attackerBody)
                return;

            CharacterMaster master = attackerBody.master;
            if (!master || _teleportBlacklist.Contains(master.masterIndex))
                return;

            TeleportUtils.TeleportBody(attackerBody, teleportPosition);
        }
    }
}
