using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    /*
    [ChaosTimedEffect("everyone_invisible", 30f, AllowDuplicates = false)]
    [RequiredComponents(typeof(ApplyBuffEffect))]
    public sealed class EveryoneInvisible : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return ApplyBuffEffect.CanSelectBuff(RoR2Content.Buffs.Cloak.buffIndex);
        }

        ApplyBuffEffect _applyBuffEffect;

        void Awake()
        {
            _applyBuffEffect = GetComponent<ApplyBuffEffect>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _applyBuffEffect.BuffIndex = RoR2Content.Buffs.Cloak.buffIndex;
            _applyBuffEffect.BuffStackCount = 1;
        }
    }
    */
}
