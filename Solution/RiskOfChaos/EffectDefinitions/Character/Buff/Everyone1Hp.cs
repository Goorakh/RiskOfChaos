using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosTimedEffect("everyone_1hp", 30f, AllowDuplicates = false)]
    [RequiredComponents(typeof(ApplyBuffEffect))]
    public sealed class Everyone1Hp : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return ApplyBuffEffect.CanSelectBuff(RoCContent.Buffs.SetTo1Hp.buffIndex);
        }

        ApplyBuffEffect _applyBuffEffect;

        void Awake()
        {
            _applyBuffEffect = GetComponent<ApplyBuffEffect>();
            _applyBuffEffect.OnBuffAppliedServer += onBuffAppliedServer;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _applyBuffEffect.BuffIndex = RoCContent.Buffs.SetTo1Hp.buffIndex;
            _applyBuffEffect.BuffStackCount = 1;
        }

        static void onBuffAppliedServer(CharacterBody body)
        {
            HealthComponent healthComponent = body.healthComponent;
            if (healthComponent)
            {
                healthComponent.Networkbarrier = 0f;
            }

            if (body.isPlayerControlled)
            {
                body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1f);

                Util.CleanseBody(body, false, false, false, true, false, false);
            }
        }
    }
}
