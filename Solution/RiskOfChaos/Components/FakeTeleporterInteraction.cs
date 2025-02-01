using EntityStates;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class FakeTeleporterInteraction : NetworkBehaviour, IInteractable, ICustomPingBehavior
    {
        public static readonly SerializableEntityStateType IdleStateType = new SerializableEntityStateType(typeof(IdleState));

        [AddressableReference("RoR2/Base/Teleporters/TeleporterChargingPositionIndicator.prefab")]
        static readonly GameObject _chargingPositionIndicatorPrefab;

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

        PositionIndicator _positionIndicator;
        ChargeIndicatorController _chargeIndicator;

        readonly List<PingIndicator> _currentPings = [];

        bool _wasDiscovered;

        [SyncVar]
        bool _isDiscovered;

        BaseTeleporterState currentState => MainStateMachine ? MainStateMachine.state as BaseTeleporterState : null;

        public bool IsIdle => currentState is IdleState;

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

            if (_chargingPositionIndicatorPrefab)
            {
                GameObject positionIndicator = Instantiate(_chargingPositionIndicatorPrefab, transform.position, Quaternion.identity);
                _positionIndicator = positionIndicator.GetComponent<PositionIndicator>();
                if (_positionIndicator)
                {
                    _positionIndicator.targetTransform = transform;
                }

                _chargeIndicator = positionIndicator.GetComponent<ChargeIndicatorController>();

                positionIndicator.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (_positionIndicator)
            {
                Destroy(_positionIndicator.gameObject);
            }
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);

            if (_positionIndicator)
            {
                _positionIndicator.gameObject.SetActive(false);
            }
        }

        void FixedUpdate()
        {
            bool shouldScaleParticlesSetting = false;
            bool useTeleporterDiscoveryIndicatorSetting = false;

            LocalUser localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser != null)
            {
                UserProfile localUserProfile = localUser.userProfile;
                if (localUserProfile != null)
                {
                    shouldScaleParticlesSetting = localUserProfile.useTeleporterParticleScaling;
                    useTeleporterDiscoveryIndicatorSetting = localUserProfile.useTeleporterDiscoveryIndicator;
                }
            }

            if (_positionIndicator)
            {
                _positionIndicator.gameObject.SetActive(IsIdle && ((_isDiscovered && useTeleporterDiscoveryIndicatorSetting) || _currentPings.Count > 0));
            }

            if (_particleScalerController)
            {
                bool shouldScaleParticles = shouldScaleParticlesSetting;

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

            updateModelChildActivations();

            updateDiscovered();
        }

        void updateModelChildActivations()
        {
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

        void updateDiscovered()
        {
            if (NetworkServer.active)
            {
                if (!_isDiscovered && PlayerUtils.AnyPlayerInRadius(transform.position, DiscoveryRadius))
                {
                    _isDiscovered = true;
                }
            }

            if (_isDiscovered != _wasDiscovered)
            {
                if (_chargeIndicator)
                {
                    _chargeIndicator.isDiscovered = _isDiscovered;

                    if (_isDiscovered)
                    {
                        _chargeIndicator.TriggerPingAnimation();
                    }
                }

                _wasDiscovered = _isDiscovered;
            }
        }

        void ICustomPingBehavior.OnPingAdded(PingIndicator pingIndicator)
        {
            if (!pingIndicator || !IsIdle)
                return;

            pingIndicator.pingDuration = 30f;
            pingIndicator.fixedTimer = pingIndicator.pingDuration;

            if (pingIndicator.pingText)
            {
                pingIndicator.pingText.enabled = false;
            }

            if (pingIndicator.interactablePingGameObjects != null && pingIndicator.interactablePingGameObjects.Length > 0)
            {
                if (pingIndicator.interactablePingGameObjects[0].TryGetComponent(out SpriteRenderer spriteRenderer))
                {
                    spriteRenderer.enabled = false;
                }
            }

            _currentPings.Add(pingIndicator);

            updatePings(true);
        }

        void ICustomPingBehavior.OnPingRemoved(PingIndicator pingIndicator)
        {
            if (!IsIdle)
                return;

            _currentPings.Remove(pingIndicator);

            updatePings(false);
        }

        void clearPings()
        {
            for (int i = _currentPings.Count - 1; i >= 0; i--)
            {
                PingIndicator pingIndicator = _currentPings[i];
                if (pingIndicator)
                {
                    pingIndicator.DestroyPing();
                }
            }

            _currentPings.Clear();
            updatePings(false);
        }

        void updatePings(bool usePingedAnimation)
        {
            for (int i = _currentPings.Count - 1; i >= 0; i--)
            {
                if (!_currentPings[i])
                {
                    _currentPings.RemoveAt(i);
                }
            }

            PingIndicator activePingIndicator = null;
            if (_currentPings.Count > 0)
            {
                activePingIndicator = _currentPings[_currentPings.Count - 1];
            }

            if (NetworkServer.active && activePingIndicator)
            {
                _isDiscovered = true;
            }

            if (_chargeIndicator)
            {
                if (activePingIndicator)
                {
                    _chargeIndicator.TriggerPing(activePingIndicator.GetOwnerName(), usePingedAnimation);
                }
                else
                {
                    _chargeIndicator.isPinged = false;
                }
            }
        }

        public string GetContextString(Interactor activator)
        {
            return Language.GetString(BeginContextString);
        }

        public Interactability GetInteractability(Interactor activator)
        {
            if (TeleporterInteraction.instance && TeleporterInteraction.instance.isIdle)
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

                teleporterInteraction.clearPings();
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
