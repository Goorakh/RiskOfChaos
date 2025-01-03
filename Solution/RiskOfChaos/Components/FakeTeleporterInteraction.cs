﻿using EntityStates;
using RiskOfChaos.Content;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class FakeTeleporterInteraction : NetworkBehaviour, IInteractable
    {
        public static readonly SerializableEntityStateType IdleStateType = new SerializableEntityStateType(typeof(IdleState));

        public PerpetualBossController BossController;

        public EntityStateMachine MainStateMachine;

        public string BeginContextString;

        public float DiscoveryRadius;

        public string[] SyncTeleporterChildActivations = [];

        public float SyncChildActivationsInterval = 0.5f;

        float _syncChildActivationsTimer;

        ModelLocator _modelLocator;

        ChildLocator _modelChildLocator;

        Particle_SetMinSize _particleScalerController;

        BaseTeleporterState currentState => MainStateMachine ? MainStateMachine.state as BaseTeleporterState : null;

        void Awake()
        {
            _modelLocator = GetComponent<ModelLocator>();
            if (_modelLocator && _modelLocator.modelTransform)
            {
                _modelChildLocator = _modelLocator.modelTransform.GetComponent<ChildLocator>();
            }

            if (_modelChildLocator)
            {
                Transform passiveParticleSphereTransform = _modelChildLocator.FindChild("PassiveParticleSphere");
                if (passiveParticleSphereTransform)
                {
                    _particleScalerController = passiveParticleSphereTransform.GetComponent<Particle_SetMinSize>();
                }
            }
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        void FixedUpdate()
        {
            if (_particleScalerController)
            {
                bool shouldScaleParticles = false;

                LocalUser localUser = LocalUserManager.GetFirstLocalUser();
                if (localUser != null)
                {
                    UserProfile localUserProfile = localUser.userProfile;
                    if (localUserProfile != null)
                    {
                        shouldScaleParticles = localUserProfile.useTeleporterParticleScaling;
                    }
                }

                if (TeleporterInteraction.instance)
                {
                    Particle_SetMinSize teleporterParticleMinSizeController = TeleporterInteraction.instance.cachedParticleMinSizeScript;
                    if (teleporterParticleMinSizeController)
                    {
                        shouldScaleParticles = teleporterParticleMinSizeController._isEnabled;
                    }
                }

                if (_particleScalerController._isEnabled != shouldScaleParticles)
                {
                    _particleScalerController.SetEnabled(shouldScaleParticles);
                }
            }

            if (SyncTeleporterChildActivations.Length > 0 && _modelChildLocator)
            {
                _syncChildActivationsTimer -= Time.fixedDeltaTime;
                if (_syncChildActivationsTimer < 0f)
                {
                    _syncChildActivationsTimer = SyncChildActivationsInterval;

                    ChildLocator teleporterModelChildLocator = TeleporterInteraction.instance ? TeleporterInteraction.instance.modelChildLocator : null;

                    foreach (string childName in SyncTeleporterChildActivations)
                    {
                        Transform childTransform = _modelChildLocator.FindChild(childName);
                        if (childTransform)
                        {
                            bool shouldBeActive = false;
                            if (teleporterModelChildLocator)
                            {
                                Transform teleporterChildTransform = teleporterModelChildLocator.FindChild(childName);
                                if (teleporterChildTransform)
                                {
                                    shouldBeActive = teleporterChildTransform.gameObject.activeSelf;
                                }
                            }

                            childTransform.gameObject.SetActive(shouldBeActive);
                        }
                    }
                }
            }
        }

        public string GetContextString(Interactor activator)
        {
            return Language.GetString(BeginContextString);
        }

        public Interactability GetInteractability(Interactor activator)
        {
            if (TeleporterInteraction.instance)
            {
                Interactability realTeleporterInteractability = TeleporterInteraction.instance.GetInteractability(activator);
                if (realTeleporterInteractability != Interactability.Available)
                {
                    return realTeleporterInteractability;
                }
            }

            BaseTeleporterState teleporterState = currentState;
            if (teleporterState != null)
            {
                return teleporterState.GetInteractability(activator);
            }

            return Interactability.Disabled;
        }

        public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
        {
            if (TeleporterInteraction.instance)
            {
                return TeleporterInteraction.instance.ShouldIgnoreSpherecastForInteractibility(activator);
            }

            return false;
        }

        public bool ShouldProximityHighlight()
        {
            if (TeleporterInteraction.instance)
            {
                return TeleporterInteraction.instance.ShouldProximityHighlight();
            }

            return true;
        }

        public bool ShouldShowOnScanner()
        {
            if (TeleporterInteraction.instance)
            {
                return TeleporterInteraction.instance.ShouldShowOnScanner();
            }

            return true;
        }

        public void OnInteractionBegin(Interactor activator)
        {
            RpcClientOnActivated();
            currentState?.OnInteractionBegin(activator);
        }

        [ClientRpc]
        void RpcClientOnActivated()
        {
            Util.PlaySound("Play_env_teleporter_active_button", gameObject);
        }

        abstract class BaseTeleporterState : BaseState
        {
            protected FakeTeleporterInteraction teleporterInteraction { get; private set; }

            public override void OnEnter()
            {
                base.OnEnter();
                teleporterInteraction = GetComponent<FakeTeleporterInteraction>();
            }

            public virtual Interactability GetInteractability(Interactor activator)
            {
                return Interactability.Disabled;
            }

            public virtual void OnInteractionBegin(Interactor activator)
            {
            }

            protected void SetChildActive(string childLocatorName, bool newActive)
            {
                if (teleporterInteraction._modelChildLocator)
                {
                    Transform child = teleporterInteraction._modelChildLocator.FindChild(childLocatorName);
                    if (child)
                    {
                        child.gameObject.SetActive(newActive);
                    }
                }
            }
        }

        [EntityStateType]
        class IdleState : BaseTeleporterState
        {
            public override Interactability GetInteractability(Interactor activator)
            {
                return Interactability.Available;
            }

            public override void OnInteractionBegin(Interactor activator)
            {
                base.OnInteractionBegin(activator);

                Chat.SendBroadcastChat(new SubjectChatMessage
                {
                    subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
                    baseToken = "PLAYER_ACTIVATED_FAKE_TELEPORTER"
                });

                outer.SetNextState(new IdleToActiveState());
            }
        }

        [EntityStateType]
        class IdleToActiveState : BaseTeleporterState
        {
            public override void OnEnter()
            {
                base.OnEnter();

                SetChildActive("IdleToChargingEffect", true);
                SetChildActive("PPVolume", true);
            }

            public override void OnExit()
            {
                SetChildActive("IdleToChargingEffect", false);

                base.OnExit();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                
                if (fixedAge > 3f)
                {
                    if (NetworkServer.active)
                    {
                        outer.SetNextState(new ActiveState());
                    }
                }
            }
        }

        [EntityStateType]
        class ActiveState : BaseTeleporterState
        {
            public override void OnEnter()
            {
                base.OnEnter();

                SetChildActive("ChargingEffect", true);

                if (NetworkServer.active)
                {
                    if (teleporterInteraction.BossController)
                    {
                        teleporterInteraction.BossController.enabled = true;
                    }
                }
            }

            public override void OnExit()
            {
                if (teleporterInteraction.BossController)
                {
                    teleporterInteraction.BossController.enabled = false;
                }

                base.OnExit();
            }

            public override Interactability GetInteractability(Interactor activator)
            {
                return Interactability.ConditionsNotMet;
            }
        }
    }
}
