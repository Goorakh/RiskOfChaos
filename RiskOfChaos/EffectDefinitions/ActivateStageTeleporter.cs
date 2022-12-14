using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions
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
            if (!stage)
                return 1f;
            
            float timeOnStage = stage.entryTime.timeSince;
            return Mathf.Min(1f, timeOnStage / (60f * 5f));
        }

        public override void OnStart()
        {
            TeleporterInteraction.instance.OnInteractionBegin(PlayerUtils.GetLocalUserInteractor());
        }
    }
}
