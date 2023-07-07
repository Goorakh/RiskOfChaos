using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AttractToPlayers : MonoBehaviour
    {
        public float MinVelocityTreshold;
        public float MaxDistance;
        public float MaxSpeed;
        public float Acceleration;

        public float? DynamicFrictionOverride = 0.025f;
        public float? StaticFrictionOverride = 0f;

        Rigidbody _rigidbody;

        Collider _collider;
        PhysicMaterial _originalMaterial;

        Vector3 _currentVelocity;
        Vector3 _targetVelocity;

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            InstanceTracker.Add(this);

            _collider = GetComponent<Collider>();
            if (_collider)
            {
                if (DynamicFrictionOverride.HasValue || StaticFrictionOverride.HasValue)
                {
                    _originalMaterial = _collider.material;
                    if (_originalMaterial)
                    {
                        PhysicMaterial overrideMaterial = Instantiate(_originalMaterial);

                        if (DynamicFrictionOverride.HasValue)
                            overrideMaterial.dynamicFriction = DynamicFrictionOverride.Value;

                        if (StaticFrictionOverride.HasValue)
                            overrideMaterial.staticFriction = StaticFrictionOverride.Value;

                        _collider.material = overrideMaterial;
                    }
                }
            }
        }

        void OnDestroy()
        {
            InstanceTracker.Remove(this);

            if (_collider && _originalMaterial)
            {
                _collider.material = _originalMaterial;
            }
        }

        void FixedUpdate()
        {
            Vector3 currentPosition = transform.position;

            _targetVelocity = Vector3.zero;
            PlayerUtils.GetAllPlayerBodies(true).TryDo(body =>
            {
                Vector3 bodyPosition = body.corePosition;

                Vector3 positionDiff = bodyPosition - currentPosition;
                float sqrDistance = positionDiff.sqrMagnitude;
                if (sqrDistance >= MaxDistance * MaxDistance)
                    return;

                Vector3 forceDirectionNormalized = positionDiff / Mathf.Sqrt(sqrDistance);
                _targetVelocity += forceDirectionNormalized * MaxSpeed;
            });

            _currentVelocity = Vector3.MoveTowards(_currentVelocity, _targetVelocity, Acceleration * Time.fixedDeltaTime);

            if (_currentVelocity.sqrMagnitude > MinVelocityTreshold * MinVelocityTreshold)
            {
                _rigidbody.AddForce(_currentVelocity, ForceMode.VelocityChange);
            }
        }

        public static AttractToPlayers TryAddComponent(MonoBehaviour self)
        {
            if (!NetworkServer.active)
                return null;

            if (!self.TryGetComponent(out Rigidbody rb))
            {
#if DEBUG
                Log.Debug($"Cannot add component to {self}: missing Rigidbody component");
#endif
                return null;
            }

            if (rb.isKinematic)
            {
#if DEBUG
                Log.Debug($"Cannot add component to {self}: object is kinematic");
#endif
                return null;
            }

            if (!rb.GetComponent<Collider>())
            {
#if DEBUG
                Log.Debug($"Cannot add component to {self}: missing collider");
#endif
                return null;
            }

#if DEBUG
            Log.Debug($"Adding component to {self}");
#endif

            return self.gameObject.AddComponent<AttractToPlayers>();
        }
    }
}
