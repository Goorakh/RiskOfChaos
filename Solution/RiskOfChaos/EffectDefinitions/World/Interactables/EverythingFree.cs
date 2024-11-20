using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Cost;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosTimedEffect("everything_free", 30f, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: All Chests are Free")]
    public sealed class EverythingFree : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.CostModificationProvider;
        }

        ValueModificationController _costModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _costModificationController = Instantiate(RoCContent.NetworkedPrefabs.CostModificationProvider).GetComponent<ValueModificationController>();

                CostModificationProvider costModificationProvider = _costModificationController.GetComponent<CostModificationProvider>();
                costModificationProvider.CostMultiplier = 0f;
                costModificationProvider.IgnoreZeroCostRestriction = true;

                NetworkServer.Spawn(_costModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_costModificationController)
            {
                _costModificationController.Retire();
                _costModificationController = null;
            }
        }
    }
}
