﻿using EntityStates;
using HG;
using RiskOfChaos.Collections;
using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("explode_at_low_health", 90f, AllowDuplicates = false)]
    public sealed class ExplodeAtLowHealth : MonoBehaviour
    {
        static GameObject _countDownVFXPrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> countdownVfxLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/QuestVolatileBattery/VolatileBatteryPreDetonation.prefab");
            countdownVfxLoad.OnSuccess(vfx => _countDownVFXPrefab = vfx);
        }

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkedPrefabs)
        {
            // ExplodeAtLowHealthController
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.ExplodeAtLowHealthController), [
                    typeof(EntityStateMachine),
                    typeof(NetworkStateMachine),
                    typeof(GenericOwnership),
                    typeof(ExplodeOnLowHealthController)
                ]);

                EntityStateMachine stateMachine = prefab.GetComponent<EntityStateMachine>();
                stateMachine.initialStateType = new SerializableEntityStateType(typeof(MonitorState));
                stateMachine.mainStateType = new SerializableEntityStateType(typeof(MonitorState));

                NetworkStateMachine networkStateMachine = prefab.GetComponent<NetworkStateMachine>();
                networkStateMachine.stateMachines = [stateMachine];

                networkedPrefabs.Add(prefab);
            }
        }

        readonly ClearingObjectList<GameObject> _exploderControllers = [];

        void Start()
        {
            if (NetworkServer.active)
            {
                _exploderControllers.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);
                CharacterBody.readOnlyInstancesList.TryDo(handleBody, FormatUtils.GetBestBodyName);
                CharacterBody.onBodyStartGlobal += handleBody;

                GlobalEventManager.onCharacterDeathGlobal += onCharacterDeathGlobal;
            }
        }

        void OnDestroy()
        {
            CharacterBody.onBodyStartGlobal -= handleBody;

            GlobalEventManager.onCharacterDeathGlobal -= onCharacterDeathGlobal;

            _exploderControllers.ClearAndDispose(true);
        }

        void onCharacterDeathGlobal(DamageReport damageReport)
        {
            // Catch "fake" death events (eg. FMP), begin countdown immediately in that case
            if (damageReport.victim && damageReport.victim.alive && damageReport.victimBody)
            {
                GameObject explodeController = attachExploder(damageReport.victimBody);

                EntityStateMachine entityStateMachine = explodeController.GetComponent<EntityStateMachine>();
                entityStateMachine.initialStateType = new SerializableEntityStateType(typeof(CountDownState));
            }
        }

        void handleBody(CharacterBody body)
        {
            attachExploder(body);
        }

        GameObject attachExploder(CharacterBody body)
        {
            if (body.isPlayerControlled)
                return null;

            GameObject explodeController = Instantiate(RoCContent.NetworkedPrefabs.ExplodeAtLowHealthController);

            ExplodeOnLowHealthController explodeOnLowHealthController = explodeController.GetComponent<ExplodeOnLowHealthController>();
            explodeOnLowHealthController.AttachedBody = body;

            NetworkServer.Spawn(explodeController);

            _exploderControllers.Add(explodeController);

            return explodeController;
        }

        class BaseState : EntityState
        {
            protected ExplodeOnLowHealthController explodeOnLowHealthController { get; private set; }

            public override void OnEnter()
            {
                base.OnEnter();

                explodeOnLowHealthController = GetComponent<ExplodeOnLowHealthController>();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                CharacterBody attachedBody = explodeOnLowHealthController.AttachedBody;
                if (attachedBody)
                {
                    transform.SetPositionAndRotation(attachedBody.corePosition, Quaternion.identity);
                }
            }
        }

        [EntityStateType]
        class MonitorState : BaseState
        {
            const float EXPLODE_HEALTH_FRACTION_DEFAULT = 0.45f;
            const float EXPLODE_HEALTH_FRACTION_BOSS = 0.175f;
            const float EXPLODE_HEALTH_FRACTION_PLAYER = 0.15f;

            bool _hasEverBeenAttachedToBody;

            float _lastHealthFraction;

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                if (!_hasEverBeenAttachedToBody)
                {
                    CharacterBody attachedBody = explodeOnLowHealthController.AttachedBody;
                    if (attachedBody)
                    {
                        _hasEverBeenAttachedToBody = true;
                    }
                }

                if (isAuthority)
                {
                    if (updateShouldStartCountdown())
                    {
                        outer.SetNextState(new CountDownState());
                    }
                }
            }

            bool updateShouldStartCountdown()
            {
                if (!_hasEverBeenAttachedToBody)
                    return false;

                CharacterBody attachedBody = explodeOnLowHealthController.AttachedBody;

                HealthComponent attachedHealthComponent = null;
                if (attachedBody)
                {
                    attachedHealthComponent = attachedBody.healthComponent;
                }

                if (attachedHealthComponent && attachedHealthComponent.alive)
                {
                    float currentHealthFraction = attachedHealthComponent.combinedHealthFraction;

                    float explodeHealthFraction = EXPLODE_HEALTH_FRACTION_DEFAULT;
                    if (attachedBody)
                    {
                        if (attachedBody.isChampion)
                            explodeHealthFraction = EXPLODE_HEALTH_FRACTION_BOSS;

                        if (attachedBody.isPlayerControlled)
                            explodeHealthFraction = EXPLODE_HEALTH_FRACTION_PLAYER;
                    }

                    if (_lastHealthFraction <= explodeHealthFraction)
                    {
                        _lastHealthFraction = currentHealthFraction;
                        return false;
                    }

                    if (currentHealthFraction > explodeHealthFraction)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [EntityStateType]
        class CountDownState : BaseState
        {
            float _countDownDuration;

            bool _hasDetonated;

            GameObject _countDownVFXInstance;

            public override void OnEnter()
            {
                base.OnEnter();

                CharacterBody attachedBody = explodeOnLowHealthController.AttachedBody;

                if (_countDownVFXPrefab)
                {
                    Transform vfxParent = transform;
                    if (vfxParent)
                    {
                        _countDownVFXInstance = GameObject.Instantiate(_countDownVFXPrefab, vfxParent);

                        Transform vfxTransform = _countDownVFXInstance.transform;
                        vfxTransform.localPosition = Vector3.zero;
                        vfxTransform.localRotation = Quaternion.identity;

                        float radius = 1f;
                        if (attachedBody)
                        {
                            radius = Mathf.Max(0.5f, attachedBody.radius);
                        }

                        vfxTransform.localScale *= (radius / vfxParent.lossyScale.ComponentMax()) * 2f;
                    }
                }

                float countDownDuration = 2f;
                if (attachedBody && attachedBody.isChampion)
                {
                    countDownDuration = 3.5f;
                }
                
                _countDownDuration = countDownDuration;
            }

            public override void OnExit()
            {
                base.OnExit();

                if (_countDownVFXInstance)
                {
                    Destroy(_countDownVFXInstance);
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                if (!_hasDetonated && checkShouldDetonate())
                {
                    detonate();
                    _hasDetonated = true;
                }
            }

            bool checkShouldDetonate()
            {
                if (fixedAge >= _countDownDuration)
                    return true;

                return false;
            }

            void detonate()
            {
                if (isAuthority)
                {
                    explodeOnLowHealthController.Detonate();
                }

                if (NetworkServer.active)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
