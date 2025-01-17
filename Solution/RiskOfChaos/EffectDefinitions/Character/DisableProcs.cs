using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("disable_procs", 45f, AllowDuplicates = false)]
    public sealed class DisableProcs : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

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
            damageInfo.procCoefficient = 0f;
            damageInfo.damageType &= ~(DamageTypeCombo)(DamageType.SlowOnHit | DamageType.ClayGoo | DamageType.Nullify | DamageType.CrippleOnHit | DamageType.ApplyMercExpose);
        }
    }
}
