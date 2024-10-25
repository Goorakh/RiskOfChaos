using EntityStates;
using HG;
using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("explode_at_low_health", 90f, AllowDuplicates = false)]
    public sealed class ExplodeAtLowHealth : MonoBehaviour
    {
        static GameObject _explosionVFXPrefab;

        static GameObject _countDownVFXPrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> explosionVfxLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/QuestVolatileBattery/VolatileBatteryExplosion.prefab");
            explosionVfxLoad.Completed += handle =>
            {
                _explosionVFXPrefab = handle.Result;
            };

            AsyncOperationHandle<GameObject> countdownVfxLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/QuestVolatileBattery/VolatileBatteryPreDetonation.prefab");
            countdownVfxLoad.Completed += handle =>
            {
                _countDownVFXPrefab = handle.Result;
            };
        }

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkedPrefabs)
        {
            // ExplodeAtLowHealthBodyAttachment
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.ExplodeAtLowHealthBodyAttachment), [
                    typeof(NetworkedBodyAttachment),
                    typeof(EntityStateMachine),
                    typeof(NetworkStateMachine),
                    typeof(GenericOwnership)
                ]);

                NetworkedBodyAttachment networkedBodyAttachment = prefab.GetComponent<NetworkedBodyAttachment>();
                networkedBodyAttachment.shouldParentToAttachedBody = true;
                networkedBodyAttachment.forceHostAuthority = true;

                EntityStateMachine stateMachine = prefab.GetComponent<EntityStateMachine>();
                stateMachine.initialStateType = new SerializableEntityStateType(typeof(MonitorState));
                stateMachine.mainStateType = new SerializableEntityStateType(typeof(MonitorState));

                NetworkStateMachine networkStateMachine = prefab.GetComponent<NetworkStateMachine>();
                networkStateMachine.stateMachines = [stateMachine];

                networkedPrefabs.Add(prefab);
            }
        }

        readonly List<GameObject> _exploderBodyAttachments = [];

        void Start()
        {
            if (NetworkServer.active)
            {
                _exploderBodyAttachments.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);
                CharacterBody.readOnlyInstancesList.TryDo(attachExploder, FormatUtils.GetBestBodyName);
                CharacterBody.onBodyStartGlobal += attachExploder;

                GlobalEventManager.onCharacterDeathGlobal += onCharacterDeathGlobal;
            }
        }

        void OnDestroy()
        {
            CharacterBody.onBodyStartGlobal -= attachExploder;

            GlobalEventManager.onCharacterDeathGlobal -= onCharacterDeathGlobal;

            foreach (GameObject bodyAttachment in _exploderBodyAttachments)
            {
                if (bodyAttachment)
                {
                    Destroy(bodyAttachment);
                }
            }
        }

        static void onCharacterDeathGlobal(DamageReport damageReport)
        {
            CharacterBody victimBody = damageReport.victimBody;
            if (!victimBody)
                return;

            List<NetworkedBodyAttachment> bodyAttachments = [];
            NetworkedBodyAttachment.FindBodyAttachments(victimBody, bodyAttachments);

            foreach (NetworkedBodyAttachment bodyAttachment in bodyAttachments)
            {
                if (bodyAttachment.TryGetComponent(out EntityStateMachine esm))
                {
                    EntityState state = esm.state;
                    if (state is BaseState)
                    {
                        if (state is CountDownState countDownState)
                        {
                            countDownState.TryForceDetonate();
                        }
                        else
                        {
                            explodeBody(victimBody, bodyAttachment.gameObject);
                        }
                    }
                }
            }
        }

        void attachExploder(CharacterBody body)
        {
            if (body.isPlayerControlled)
                return;

            GameObject attachment = Instantiate(RoCContent.NetworkedPrefabs.ExplodeAtLowHealthBodyAttachment);

            NetworkedBodyAttachment exploderAttachment = attachment.GetComponent<NetworkedBodyAttachment>();
            exploderAttachment.AttachToGameObjectAndSpawn(body.gameObject);

            _exploderBodyAttachments.Add(attachment);
        }

        static void explodeBody(CharacterBody body, GameObject inflictor)
        {
            HealthComponent healthComponent = body.healthComponent;

            Vector3 blastCenter = body.corePosition;

            float maxHealth;
            if (healthComponent)
            {
                maxHealth = healthComponent.fullCombinedHealth;
            }
            else
            {
                maxHealth = body.maxHealth + body.maxShield;
            }

            float damageMultiplier = body.isPlayerControlled ? 3f : 1.25f;

            float damage = Mathf.Max(15f, maxHealth) * damageMultiplier;

            float blastRadius = Mathf.Max(body.radius * 1.5f, 20f);

            EffectManager.SpawnEffect(_explosionVFXPrefab, new EffectData
            {
                origin = blastCenter,
                scale = blastRadius
            }, true);

            BlastAttack blastAttack = new BlastAttack
            {
                position = blastCenter,
                radius = blastRadius,
                falloffModel = BlastAttack.FalloffModel.Linear,
                // Credit whoever (probably) triggered the explosion, if applicable
                attacker = healthComponent && healthComponent.lastHitAttacker ? healthComponent.lastHitAttacker : body.gameObject, 
                inflictor = inflictor,
                damageColorIndex = DamageColorIndex.Item,
                baseDamage = damage,
                baseForce = 5000f,
                attackerFiltering = AttackerFiltering.AlwaysHit,
                crit = false,
                procCoefficient = 1f,
                teamIndex = body.teamComponent.teamIndex
            };

            blastAttack.AddModdedDamageType(DamageTypes.BypassArmorSelf);
            blastAttack.AddModdedDamageType(DamageTypes.BypassBlockSelf);

            if (!body.isPlayerControlled)
            {
                blastAttack.AddModdedDamageType(DamageTypes.BypassOSPSelf);
            }

            blastAttack.AddModdedDamageType(DamageTypes.NonLethalToNonAttackerPlayers);

            blastAttack.Fire();
        }

        class BaseState : EntityState
        {
            protected CharacterBody attachedBody { get; private set; }

            protected HealthComponent attachedHealthComponent { get; private set; }

            public override void OnEnter()
            {
                base.OnEnter();

                NetworkedBodyAttachment bodyAttachment = GetComponent<NetworkedBodyAttachment>();
                if (bodyAttachment)
                {
                    attachedBody = bodyAttachment.attachedBody;
                    attachedHealthComponent = attachedBody.healthComponent;
                }

                if (NetworkServer.active)
                {
                    GenericOwnership ownership = GetComponent<GenericOwnership>();
                    ownership.ownerObject = attachedBody ? attachedBody.gameObject : null;
                }
            }
        }

        [EntityStateType]
        class MonitorState : BaseState
        {
            const float EXPLODE_HEALTH_FRACTION_PLAYER = 0.15f;
            const float EXPLODE_HEALTH_FRACTION_ENEMY = 0.45f;
            const float EXPLODE_HEALTH_FRACTION_BOSS = 0.175f;

            float _explodeHealthFraction;

            float _lastHealthFraction;

            public override void OnEnter()
            {
                base.OnEnter();

                if (NetworkServer.active)
                {
                    if (attachedHealthComponent)
                    {
                        _lastHealthFraction = attachedHealthComponent.combinedHealthFraction;
                    }

                    if (attachedBody.isPlayerControlled)
                    {
                        _explodeHealthFraction = EXPLODE_HEALTH_FRACTION_PLAYER;
                    }
                    else if (attachedBody.isBoss)
                    {
                        _explodeHealthFraction = EXPLODE_HEALTH_FRACTION_BOSS;
                    }
                    else
                    {
                        _explodeHealthFraction = EXPLODE_HEALTH_FRACTION_ENEMY;
                    }
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                if (NetworkServer.active)
                {
                    if (updateShouldExplode())
                    {
                        outer.SetNextState(new CountDownState());
                    }
                }
            }

            bool updateShouldExplode()
            {
                if (!attachedHealthComponent || !attachedHealthComponent.alive)
                {
                    return false;
                }

                float currentHealthFraction = attachedHealthComponent.combinedHealthFraction;

                if (_lastHealthFraction > _explodeHealthFraction)
                {
                    if (currentHealthFraction <= _explodeHealthFraction)
                    {
                        return true;
                    }
                }
                else
                {
                    _lastHealthFraction = currentHealthFraction;
                }

                return false;
            }
        }

        [EntityStateType]
        class CountDownState : BaseState
        {
            float _countDownTime;

            bool _hasDetonated;

            GameObject _countDownVFXInstance;

            public override void OnEnter()
            {
                base.OnEnter();

                if (_countDownVFXPrefab && attachedBody)
                {
                    Transform vfxParent = attachedBody.coreTransform;
                    if (vfxParent)
                    {
                        _countDownVFXInstance = GameObject.Instantiate(_countDownVFXPrefab, vfxParent);

                        Transform vfxTransform = _countDownVFXInstance.transform;
                        vfxTransform.localPosition = Vector3.zero;
                        vfxTransform.localRotation = Quaternion.identity;
                        vfxTransform.localScale *= (Mathf.Max(0.5f, attachedBody.radius) / vfxParent.lossyScale.ComponentMax()) * 2f;
                    }
                }

                if (attachedBody && attachedBody.isBoss)
                {
                    _countDownTime = 3.5f;
                }
                else
                {
                    _countDownTime = 2f;
                }
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

                if (NetworkServer.active)
                {
                    if (!_hasDetonated && checkShouldDetonate())
                    {
                        detonate();
                        _hasDetonated = true;
                    }
                }
            }

            bool checkShouldDetonate()
            {
                if (!attachedHealthComponent || !attachedHealthComponent.alive)
                    return false;

                if (fixedAge >= _countDownTime)
                    return true;

                return false;
            }

            public void TryForceDetonate()
            {
                if (_hasDetonated)
                    return;

                detonate();
                _hasDetonated = true;
            }

            void detonate()
            {
                if (attachedBody)
                {
                    explodeBody(attachedBody, gameObject);
                }

                outer.SetState(new Idle());
            }
        }
    }
}
