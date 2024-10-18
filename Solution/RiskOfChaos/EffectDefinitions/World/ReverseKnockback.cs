using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Knockback;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("reverse_knockback", TimedEffectType.UntilStageEnd)]
    [IncompatibleEffects(typeof(DisableKnockback))]
    public sealed class ReverseKnockback : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.KnockbackModificationProvider;
        }

        ValueModificationController _knockbackModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _knockbackModificationController = Instantiate(RoCContent.NetworkedPrefabs.KnockbackModificationProvider).GetComponent<ValueModificationController>();

                KnockbackModificationProvider knockbackModificationProvider = _knockbackModificationController.GetComponent<KnockbackModificationProvider>();
                knockbackModificationProvider.KnockbackMultiplier = -1f;

                NetworkServer.Spawn(_knockbackModificationController.gameObject);
            }
        }

        void OnDisable()
        {
            if (_knockbackModificationController)
            {
                _knockbackModificationController.Retire();
                _knockbackModificationController = null;
            }
        }
    }
}
