using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosEffect("activate_stage_teleporter")]
    public sealed class ActivateStageTeleporter : NetworkBehaviour
    {
        static List<IInteractable> getTeleporterInteractables()
        {
            List<IInteractable> teleporterInteractables = [];

            if (TeleporterInteraction.instance)
            {
                teleporterInteractables.Add(TeleporterInteraction.instance);
            }

            teleporterInteractables.AddRange(InstanceTracker.GetInstancesList<FakeTeleporterInteraction>());

            return teleporterInteractables;
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            Interactor interactor = ChaosInteractor.GetInteractor();
            if (!interactor)
                return false;

            foreach (IInteractable teleporterInteractable in getTeleporterInteractables())
            {
                Interactability interactability = teleporterInteractable.GetInteractability(interactor);
                if (interactability == Interactability.Available || (teleporterInteractable is TeleporterInteraction && !context.IsNow))
                {
                    return true;
                }
            }

            return false;
        }

        [EffectWeightMultiplierSelector]
        static float GetWeightMult()
        {
            if (!Stage.instance)
                return 1f;

            bool isTeleporterCharged = false;
            if (TeleporterInteraction.instance)
            {
                isTeleporterCharged = TeleporterInteraction.instance.activationState >= TeleporterInteraction.ActivationState.Charged;
            }

            float timeOnStage = Stage.instance.entryTime.timeSince;
            return Mathf.Min(1f, timeOnStage / (60f * 5f * (isTeleporterCharged ? 1.5f : 1f)));
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;
            
            List<IInteractable> teleporterInteractables = getTeleporterInteractables();
            WeightedSelection<IInteractable> teleporterSelection = new WeightedSelection<IInteractable>();
            teleporterSelection.EnsureCapacity(teleporterInteractables.Count);

            foreach (IInteractable teleporterInteractable in teleporterInteractables)
            {
                float weight = 1f;
                if (teleporterInteractable is FakeTeleporterInteraction)
                {
                    weight = 0.5f;
                }

                teleporterSelection.AddChoice(teleporterInteractable, weight);
            }

            IInteractable selectedTeleporterInteraction = null;
            if (teleporterSelection.Count > 0)
            {
                selectedTeleporterInteraction = teleporterSelection.GetRandom(_rng);
            }

            if (selectedTeleporterInteraction == null)
            {
                Log.Error("No teleporter instance available");
                return;
            }

            selectedTeleporterInteraction.OnInteractionBegin(ChaosInteractor.GetInteractor());
        }
    }
}
