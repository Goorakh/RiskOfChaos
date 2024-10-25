using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("disable_fall_damage", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class DisableFallDamage : MonoBehaviour
    {
        void Start()
        {
            if (NetworkServer.active)
            {
                DamageModificationHooks.ModifyDamageInfo += modifyDamage;
            }
        }

        void OnDestroy()
        {
            DamageModificationHooks.ModifyDamageInfo -= modifyDamage;
        }

        static void modifyDamage(DamageInfo damageInfo)
        {
            if ((damageInfo.damageType & DamageType.FallDamage) != DamageType.FallDamage)
                return;
            
            damageInfo.damage = 0f;
            damageInfo.rejected = true;
            damageInfo.canRejectForce = true;
        }
    }
}
