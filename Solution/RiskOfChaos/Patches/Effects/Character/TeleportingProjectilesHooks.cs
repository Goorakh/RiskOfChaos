using RiskOfChaos.EffectDefinitions.Character;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches.Effects.Character
{
    static class TeleportingProjectilesHooks
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GlobalEventManager.OnHitAll += GlobalEventManager_OnHitAll;

            On.RoR2.BlastAttack.Fire += BlastAttack_Fire;
        }

        static void GlobalEventManager_OnHitAll(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);

            if (!NetworkServer.active)
                return;

            TeleportingProjectiles.TryTeleportAttacker(damageInfo.attacker, damageInfo.position);
        }

        static BlastAttack.Result BlastAttack_Fire(On.RoR2.BlastAttack.orig_Fire orig, BlastAttack self)
        {
            BlastAttack.Result result = orig(self);

            if (result.hitCount == 0)
            {
                TeleportingProjectiles.TryTeleportAttacker(self.attacker, self.position);
            }

            return result;
        }
    }
}
