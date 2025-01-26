using EntityStates;
using EntityStates.GolemMonster;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Content.EntityStates.PulseGolem
{
    [EntityStateType]
    public class ChargeHook : BaseState
    {
        static float baseDuration => ChargeLaser.baseDuration;

        static float laserMaxWidth => ChargeLaser.laserMaxWidth;

        static GameObject chargeEffectPrefab => ChargeLaser.effectPrefab;

        static GameObject laserPrefab => ChargeLaser.laserPrefab;

        static string attackSoundString => ChargeLaser.attackSoundString;

        float _duration;

        uint _chargeSoundPlayingID;

        EffectManagerHelper _chargeEffectHelper;
        GameObject _chargeEffect;

        GameObject _laserEffect;
        LineRenderer _laserEffectLineRenderer;

        float _laserFlashTimer;
        bool _laserFlashOn;

        Ray? _currentLaserAimRay;

        public override void OnEnter()
        {
            base.OnEnter();

            SetAIUpdateFrequency(true);

            _duration = baseDuration / attackSpeedStat;

            _chargeSoundPlayingID = Util.PlayAttackSpeedSound(attackSoundString, gameObject, attackSpeedStat);

            ChildLocator modelChildLocator = GetModelChildLocator();
            if (modelChildLocator)
            {
                Transform muzzleLaserTransform = modelChildLocator.FindChild("MuzzleLaser");
                if (muzzleLaserTransform)
                {
                    if (chargeEffectPrefab)
                    {
                        if (EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
                        {
                            _chargeEffectHelper = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, muzzleLaserTransform.position, muzzleLaserTransform.rotation);
                            _chargeEffect = _chargeEffectHelper.gameObject;
                        }
                        else
                        {
                            _chargeEffect = GameObject.Instantiate(chargeEffectPrefab, muzzleLaserTransform.position, muzzleLaserTransform.rotation);
                        }

                        if (_chargeEffect)
                        {
                            _chargeEffect.transform.SetParent(muzzleLaserTransform, true);

                            if (_chargeEffect.TryGetComponent(out ScaleParticleSystemDuration scaleParticleSystemDuration))
                            {
                                scaleParticleSystemDuration.newDuration = _duration;
                            }
                        }
                    }

                    if (laserPrefab)
                    {
                        _laserEffect = GameObject.Instantiate(laserPrefab, muzzleLaserTransform.position, muzzleLaserTransform.rotation);
                    }

                    if (_laserEffect)
                    {
                        _laserEffect.transform.SetParent(muzzleLaserTransform, true);
                        _laserEffect.SetActive(true);

                        _laserEffectLineRenderer = _laserEffect.GetComponent<LineRenderer>();
                    }
                }
            }

            if (_laserEffectLineRenderer)
            {
                _laserEffectLineRenderer.startColor = new Color(0f, 0.3f, 1f);
                _laserEffectLineRenderer.endColor = new Color(0f, 0.85f, 1f);
            }

            if (characterBody)
            {
                characterBody.SetAimTimer(_duration);
            }

            _laserFlashTimer = 0f;
            _laserFlashOn = true;
        }

        public override void OnExit()
        {
            base.OnExit();

            AkSoundEngine.StopPlayingID(_chargeSoundPlayingID);

            SetAIUpdateFrequency(false);

            if (_chargeEffectHelper)
            {
                _chargeEffectHelper.ReturnToPoolOrDestroyInstance(ref _chargeEffect);
            }
            else
            {
                Destroy(_chargeEffect);
            }

            Destroy(_laserEffect);
        }

        public override void Update()
        {
            base.Update();

            if (_laserEffect && _laserEffectLineRenderer)
            {
                float laserMaxDistance = 1000f;

                Ray aimRay = GetAimRay();

                Vector3 laserStartPosition = _laserEffect.transform.parent.position;
                Vector3 laserEndPosition = aimRay.GetPoint(laserMaxDistance);

                if (Util.CharacterRaycast(gameObject, aimRay, out RaycastHit hitInfo, laserMaxDistance, LayerIndex.CommonMasks.laser, QueryTriggerInteraction.Ignore))
                {
                    laserEndPosition = hitInfo.point;
                }

                _currentLaserAimRay = new Ray(laserStartPosition, laserEndPosition - laserStartPosition);

                _laserEffectLineRenderer.SetPosition(0, laserStartPosition);
                _laserEffectLineRenderer.SetPosition(1, laserEndPosition);

                float laserWidth = 0f;
                if (_duration - age > 0.5f)
                {
                    laserWidth = age / _duration;
                }
                else
                {
                    _laserFlashTimer -= Time.deltaTime;
                    if (_laserFlashTimer <= 0f)
                    {
                        _laserFlashTimer += 1f / 30f;
                        _laserFlashOn = !_laserFlashOn;
                    }

                    laserWidth = _laserFlashOn ? 1f : 0f;
                }

                laserWidth *= laserMaxWidth;

                _laserEffectLineRenderer.startWidth = laserWidth;
                _laserEffectLineRenderer.endWidth = laserWidth;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fixedAge > _duration)
            {
                if (isAuthority)
                {
                    outer.SetNextState(new FireHook
                    {
                        HookFireRay = _currentLaserAimRay
                    });
                }

                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
