using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Knockback;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Knockback
{
    [ChaosTimedEffect("disable_knockback", TimedEffectType.UntilStageEnd, AllowDuplicates = false, DefaultSelectionWeight = 0.8f)]
    public sealed class DisableKnockback : MonoBehaviour
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
                knockbackModificationProvider.KnockbackMultiplier = 0f;

                NetworkServer.Spawn(_knockbackModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_knockbackModificationController)
            {
                _knockbackModificationController.Retire();
                _knockbackModificationController = null;
            }
        }
    }
}
