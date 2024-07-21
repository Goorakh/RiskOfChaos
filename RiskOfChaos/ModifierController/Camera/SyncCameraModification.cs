using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Camera
{
    public sealed class SyncCameraModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [SyncVar]
        bool _anyModificationActive;
        public bool AnyModificationActive
        {
            get
            {
                return _anyModificationActive || _isInterpolatingValuesClient;
            }
            set
            {
                _anyModificationActive = value;
            }
        }

        bool _isInterpolatingValuesClient;

        [SyncVar]
        public Vector2 RecoilMultiplier = Vector2.one;

        [SyncVar]
        float _serverFovMultiplier = 1f;

        float _clientFovMultiplier;
        float _clientFovMultiplierVelocity;

        public float FovMultiplier
        {
            get
            {
                return NetworkServer.active ? _serverFovMultiplier : _clientFovMultiplier;
            }
            [Server]
            set
            {
                _serverFovMultiplier = value;
            }
        }

        [SyncVar]
        Quaternion _serverRotationOffset = Quaternion.identity;

        Quaternion _clientRotationOffset;
        Vector3 _clientRotationOffsetVelocity;

        public Quaternion RotationOffset
        {
            get
            {
                return NetworkServer.active ? _serverRotationOffset : _clientRotationOffset;
            }
            [Server]
            set
            {
                _serverRotationOffset = value;
            }
        }

        [SyncVar]
        float _serverDistanceMultiplier = 1f;

        float _clientDistanceMultiplier;
        float _clientDistanceMultiplierVelocity;

        public float DistanceMultiplier
        {
            get
            {
                return NetworkServer.active ? _serverDistanceMultiplier : _clientDistanceMultiplier;
            }
            [Server]
            set
            {
                _serverDistanceMultiplier = value;
            }
        }

        const float NETWORK_UPDATE_INTERVAL = 0.1f;

        public override void OnStartClient()
        {
            base.OnStartClient();

            _clientFovMultiplier = _serverFovMultiplier;
            _clientRotationOffset = _serverRotationOffset;
            _clientDistanceMultiplier = _serverDistanceMultiplier;
        }

        public override float GetNetworkSendInterval()
        {
            return NETWORK_UPDATE_INTERVAL;
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active)
            {
                fixedUpdateClient();
            }
        }

        [Client]
        void fixedUpdateClient()
        {
            float fovMultiplierDiff = _serverFovMultiplier - _clientFovMultiplier;
            float rotationOffsetAngleDiff = Quaternion.Angle(_serverRotationOffset, _clientRotationOffset);
            float distanceMultiplierDiff = _serverDistanceMultiplier - _clientDistanceMultiplier;

            const float MULTIPLIER_EPSILON = 0.01f;
            const float ANGLE_EPSILON = 0.1f;

            bool isInterpolating = Mathf.Abs(fovMultiplierDiff) >= MULTIPLIER_EPSILON ||
                                   rotationOffsetAngleDiff >= ANGLE_EPSILON ||
                                   Mathf.Abs(distanceMultiplierDiff) >= MULTIPLIER_EPSILON;

            _clientFovMultiplier = Mathf.SmoothDamp(_clientFovMultiplier, _serverFovMultiplier, ref _clientFovMultiplierVelocity, NETWORK_UPDATE_INTERVAL, float.PositiveInfinity, Time.fixedUnscaledDeltaTime);

            Vector3 clientEulerOffset = _clientRotationOffset.eulerAngles;
            Vector3 serverEulerOffset = _serverRotationOffset.eulerAngles;

            clientEulerOffset.x = Mathf.SmoothDampAngle(clientEulerOffset.x, serverEulerOffset.x, ref _clientRotationOffsetVelocity.x, NETWORK_UPDATE_INTERVAL, float.PositiveInfinity, Time.fixedUnscaledDeltaTime);

            clientEulerOffset.y = Mathf.SmoothDampAngle(clientEulerOffset.y, serverEulerOffset.y, ref _clientRotationOffsetVelocity.y, NETWORK_UPDATE_INTERVAL, float.PositiveInfinity, Time.fixedUnscaledDeltaTime);

            clientEulerOffset.z = Mathf.SmoothDampAngle(clientEulerOffset.z, serverEulerOffset.z, ref _clientRotationOffsetVelocity.z, NETWORK_UPDATE_INTERVAL, float.PositiveInfinity, Time.fixedUnscaledDeltaTime);

            _clientRotationOffset = Quaternion.Euler(clientEulerOffset);

            _clientDistanceMultiplier = Mathf.SmoothDamp(_clientDistanceMultiplier, _serverDistanceMultiplier, ref _clientDistanceMultiplierVelocity, NETWORK_UPDATE_INTERVAL, float.PositiveInfinity, Time.fixedUnscaledDeltaTime);

#if DEBUG
            if (_isInterpolatingValuesClient != isInterpolating)
            {
                if (isInterpolating)
                {
                    Log.Debug("[Client] Started interpolating camera modifications");
                }
                else
                {
                    Log.Debug("[Client] Finished interpolating camera modifications");
                }
            }
#endif

            _isInterpolatingValuesClient = isInterpolating;
        }
    }
}
