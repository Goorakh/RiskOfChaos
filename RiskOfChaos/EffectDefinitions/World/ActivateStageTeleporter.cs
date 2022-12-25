using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utility;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("ActivateStageTeleporter")]
    public class ActivateStageTeleporter : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            Interactor localUserInteractor = PlayerUtils.GetLocalUserInteractor();
            if (!localUserInteractor)
                return false;

            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;
            return tpInteraction && tpInteraction.GetInteractability(localUserInteractor) >= Interactability.Available;
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
            TeleporterInteraction.instance.OnInteractionBegin(PlayerUtils.GetLocalUserInteractor());
        }
    }
}
