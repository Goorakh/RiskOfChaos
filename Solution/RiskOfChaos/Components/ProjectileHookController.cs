using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(ProjectileController))]
    [RequireComponent(typeof(ProjectileStickOnImpact))]
    public sealed class ProjectileHookController : NetworkBehaviour
    {
        public float ReelDelay;

        public float ReelSpeed;

        public string PullOriginChildName;

        public float PullTargetDistance;

        public GameObject HookCharacterEffectPrefab;

        public string HookSoundString;

        ProjectileController _projectileController;
        ProjectileStickOnImpact _stickController;
        ProjectileDamage _projectileDamage;
        TeamFilter _teamFilter;

        Vector3 _startPosition;
        Transform _pullOriginTransform;

        float _reelTimer = 0f;

        GameObject _currentVictim;

        NetworkIdentity _victimNetIdentity;
        CharacterBody _victimCharacterBody;
        IPhysMotor _victimPhysMotor;
        Rigidbody _victimRigidbody;

        Vector3 victimVelocity
        {
            get
            {
                if (_victimPhysMotor != null)
                {
                    return _victimPhysMotor.velocityAuthority;
                }
                else if (_victimRigidbody)
                {
                    return _victimRigidbody.velocity;
                }

                return Vector3.zero;
            }
            set
            {
                if (_victimPhysMotor != null)
                {
                    _victimPhysMotor.velocityAuthority = value;
                }
                else if (_victimRigidbody)
                {
                    _victimRigidbody.velocity = value;
                }
            }
        }

        void Awake()
        {
            _projectileController = GetComponent<ProjectileController>();
            _stickController = GetComponent<ProjectileStickOnImpact>();
            _projectileDamage = GetComponent<ProjectileDamage>();
            _teamFilter = GetComponent<TeamFilter>();
        }

        void Start()
        {
            Transform pullOriginTransform = null;

            GameObject owner = _projectileController.owner;
            if (owner)
            {
                pullOriginTransform = owner.transform;

                if (owner.TryGetComponent(out ModelLocator ownerModelLocator) &&
                    ownerModelLocator.modelTransform &&
                    ownerModelLocator.modelTransform.TryGetComponent(out ChildLocator ownerModelChildLocator))
                {
                    Transform modelPullOriginTransform = ownerModelChildLocator.FindChild(PullOriginChildName);
                    if (modelPullOriginTransform)
                    {
                        pullOriginTransform = modelPullOriginTransform;
                    }
                }
            }

            _pullOriginTransform = pullOriginTransform;
            _startPosition = _pullOriginTransform ? _pullOriginTransform.position : transform.position;
        }

        void FixedUpdate()
        {
            updateReeling(Time.fixedDeltaTime);
        }

        void updateReeling(float deltaTime)
        {
            GameObject currentStickObject = null;

            if (_stickController && _stickController.stuck)
            {
                currentStickObject = _stickController.victim;
            }

            bool isNewVictim = false;

            if (currentStickObject != _currentVictim)
            {
                _currentVictim = currentStickObject;

                _victimNetIdentity = _currentVictim ? _currentVictim.GetComponent<NetworkIdentity>() : null;
                _victimCharacterBody = _currentVictim ? _currentVictim.GetComponent<CharacterBody>() : null;
                _victimPhysMotor = _currentVictim ? _currentVictim.GetComponent<IPhysMotor>() : null;
                _victimRigidbody = _currentVictim ? _currentVictim.GetComponent<Rigidbody>() : null;

                isNewVictim = true;
            }

            if (!currentStickObject)
            {
                _reelTimer = 0f;
                return;
            }

            _reelTimer += deltaTime;

            if (_victimNetIdentity)
            {
                if (!Util.HasEffectiveAuthority(_victimNetIdentity))
                    return;
            }
            else
            {
                if (!NetworkServer.active)
                    return;
            }

            if (_victimCharacterBody)
            {
                bool shouldDamage = FriendlyFireManager.ShouldDirectHitProceed(_victimCharacterBody.healthComponent, _teamFilter.teamIndex);

                if (isNewVictim)
                {
                    DamageInfo damageInfo = new DamageInfo
                    {
                        attacker = _projectileController.owner,
                        inflictor = gameObject,
                        position = transform.position,
                    };

                    if (_projectileDamage)
                    {
                        damageInfo.damage = _projectileDamage.damage;
                        damageInfo.crit = _projectileDamage.crit;
                        damageInfo.force = _projectileDamage.force * transform.forward;
                        damageInfo.procChainMask = _projectileController.procChainMask;
                        damageInfo.procCoefficient = _projectileController.procCoefficient;
                        damageInfo.damageColorIndex = _projectileDamage.damageColorIndex;
                        damageInfo.damageType = _projectileDamage.damageType;
                    }

                    if (shouldDamage)
                    {
                        _victimCharacterBody.healthComponent.TakeDamage(damageInfo);
                        GlobalEventManager.instance.OnHitEnemy(damageInfo, _victimCharacterBody.gameObject);
                    }

                    GlobalEventManager.instance.OnHitAll(damageInfo, _victimCharacterBody.gameObject);

                    if (HookCharacterEffectPrefab)
                    {
                        EffectManager.SimpleImpactEffect(HookCharacterEffectPrefab, transform.position, -transform.forward, false);
                    }

                    if (!string.IsNullOrWhiteSpace(HookSoundString))
                    {
                        Util.PlaySound(HookSoundString, gameObject);
                    }
                }

                if (!shouldDamage)
                {
                    if (NetworkServer.active)
                    {
                        _stickController.Detach();
                    }

                    return;
                }
            }

            if (_reelTimer < ReelDelay)
                return;

            Vector3 pullOrigin = _pullOriginTransform ? _pullOriginTransform.position : _startPosition;

            Vector3 pullVector = pullOrigin - transform.position;

            if (pullVector.sqrMagnitude <= PullTargetDistance * PullTargetDistance)
            {
                if (NetworkServer.active)
                {
                    Vector3 victimStopVelocity = victimVelocity;
                    
                    float maxStopSpeed = ReelSpeed * 0.2f;

                    if (victimStopVelocity.sqrMagnitude > maxStopSpeed * maxStopSpeed)
                    {
                        victimVelocity = victimStopVelocity.normalized * maxStopSpeed;
                    }

                    Destroy(gameObject);
                }

                return;
            }

            if ((NetworkServer.active && !_victimNetIdentity) || Util.HasEffectiveAuthority(_victimNetIdentity))
            {
                Vector3 targetVelocity = pullVector.normalized * ReelSpeed;

                victimVelocity = targetVelocity;

                if (targetVelocity.y > 0f && _victimPhysMotor is CharacterMotor victimCharacterMotor)
                {
                    victimCharacterMotor.Motor.ForceUnground();
                }
            }
        }
    }
}
