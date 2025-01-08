using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AttractToPlayers : MonoBehaviour
    {
        public float MaxDistance;
        public float Acceleration;
        public float OppositeDirectionFrictionMultiplier = 30f;

        float _dynamicFrictionOverride = 0.025f;
        public float DynamicFrictionOverride
        {
            get
            {
                return _dynamicFrictionOverride;
            }
            set
            {
                if (_dynamicFrictionOverride == value)
                    return;

                _dynamicFrictionOverride = value;

                if (_overrideMaterial)
                {
                    _overrideMaterial.dynamicFriction = _dynamicFrictionOverride;
                }
            }
        }

        float _staticFrictionOverride = 0f;
        public float StaticFrictionOverride
        {
            get
            {
                return _staticFrictionOverride;
            }
            set
            {
                if (_staticFrictionOverride == value)
                    return;

                _staticFrictionOverride = value;

                if (_overrideMaterial)
                {
                    _overrideMaterial.staticFriction = _staticFrictionOverride;
                }
            }
        }

        Rigidbody _rigidbody;

        PhysicMaterialOverride _materialOverrideController;
        PhysicMaterial _overrideMaterial;

        Vector3 _targetDirection;

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            _overrideMaterial = new PhysicMaterial("LowFriction")
            {
                dynamicFriction = _dynamicFrictionOverride,
                staticFriction = _staticFrictionOverride
            };

            _materialOverrideController = PhysicMaterialOverride.AddOverrideMaterial(gameObject, _overrideMaterial);
        }

        void OnDestroy()
        {
            if (_overrideMaterial)
            {
                _materialOverrideController.RemoveOverrideMaterial(_overrideMaterial);
                Destroy(_overrideMaterial);
            }
        }

        float getTargetFrictionMultiplier()
        {
            Vector3 velocity = _rigidbody.velocity;

            bool isMoving = velocity.sqrMagnitude > 0f;
            bool shouldBeMoving = _targetDirection.sqrMagnitude > 0f;

            if (!isMoving && shouldBeMoving)
                return 1f;

            if (!shouldBeMoving || (isMoving && Vector3.Angle(velocity, _targetDirection) >= 90f))
                return OppositeDirectionFrictionMultiplier;

            return 1f;
        }

        void setFrictionMultiplier(float multiplier)
        {
            if (_overrideMaterial)
            {
                _overrideMaterial.dynamicFriction = _dynamicFrictionOverride * multiplier;
            }
        }

        void FixedUpdate()
        {
            Vector3 currentPosition = transform.position;

            Vector3 targetDirection = Vector3.zero;

            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                if (!master.TryGetBodyPosition(out Vector3 bodyPosition))
                    continue;

                Vector3 positionDiff = bodyPosition - currentPosition;
                float sqrDistance = positionDiff.sqrMagnitude;
                if (sqrDistance > MaxDistance * MaxDistance)
                    continue;

                float normalizedStrength = 1f - (sqrDistance / (MaxDistance * MaxDistance));

                Vector3 force = positionDiff.normalized * (Acceleration * normalizedStrength);
                _rigidbody.AddForce(force, ForceMode.Acceleration);
                targetDirection += force.normalized;
            }

            _targetDirection = targetDirection;

            setFrictionMultiplier(getTargetFrictionMultiplier());
        }

        public static bool CanAddComponent(GameObject targetObject)
        {
            if (!targetObject.TryGetComponent(out Rigidbody rb))
            {
                Log.Debug($"Cannot add component to {targetObject}: missing Rigidbody component");
                return false;
            }

            if (rb.isKinematic)
            {
                Log.Debug($"Cannot add component to {targetObject}: object is kinematic");
                return false;
            }

            if (!rb.GetComponent<Collider>())
            {
                Log.Debug($"Cannot add component to {targetObject}: missing collider");
                return false;
            }

            return true;
        }

        public static AttractToPlayers TryAddComponent(GameObject targetObject)
        {
            if (!NetworkServer.active)
                return null;

            if (!CanAddComponent(targetObject))
                return null;

            Log.Debug($"Adding component to {targetObject}");

            if (!targetObject.GetComponent<NetworkTransform>() && !targetObject.GetComponent<ProjectileNetworkTransform>())
            {
                Log.Warning($"{targetObject} is missing network transform: Position will not be matched for clients!");
            }

            return targetObject.AddComponent<AttractToPlayers>();
        }
    }
}
