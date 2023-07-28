using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("activate_stage_teleporter")]
    public sealed class ActivateStageTeleporter : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            Interactor interactor = ChaosInteractor.GetInteractor();
            if (!interactor)
                return false;

            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;
            return tpInteraction && (!context.IsNow || tpInteraction.GetInteractability(interactor) >= Interactability.Available);
        }

        [EffectWeightMultiplierSelector]
        static float GetWeightMult()
        {
            Stage stage = Stage.instance;
            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;
            if (!stage || !tpInteraction)
                return 0f;

            float timeOnStage = stage.entryTime.timeSince;
            return Mathf.Min(1f, timeOnStage / (60f * 7.5f * (tpInteraction.activationState >= TeleporterInteraction.ActivationState.Charged ? 1.5f : 1f)));
        }

        public override void OnStart()
        {
            TeleporterInteraction.instance.OnInteractionBegin(ChaosInteractor.GetInteractor());
        }
    }
}
