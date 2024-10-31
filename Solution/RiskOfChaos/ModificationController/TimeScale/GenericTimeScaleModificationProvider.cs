using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.TimeScale
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class GenericTimeScaleModificationProvider : NetworkBehaviour, ITimeScaleModificationProvider
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> TimeScaleMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setTimeScaleMultiplier))]
        float _timeScaleMultiplier = 1f;
        public float TimeScaleMultiplier
        {
            get
            {
                float timeScaleMultiplier = _timeScaleMultiplier;

                if (_modificationController && _modificationController.IsInterpolating)
                {
                    timeScaleMultiplier = Mathf.Lerp(1f, timeScaleMultiplier, Ease.InOutQuad(_modificationController.CurrentInterpolationFraction));
                }

                return timeScaleMultiplier;
            }
            set
            {
                _timeScaleMultiplier = Mathf.Max(0f, value);
            }
        }

        public ValueModificationConfigBinding<bool> CompensatePlayerSpeedConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setCompensatePlayerSpeed))]
        public bool CompensatePlayerSpeed;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            TimeScaleMultiplierConfigBinding = new ValueModificationConfigBinding<float>(v => TimeScaleMultiplier = v);
            CompensatePlayerSpeedConfigBinding = new ValueModificationConfigBinding<bool>(v => CompensatePlayerSpeed = v);
        }

        void OnDestroy()
        {
            disposeConfigBindings();
        }

        void onRetire()
        {
            disposeConfigBindings();
        }

        void disposeConfigBindings()
        {
            TimeScaleMultiplierConfigBinding?.Dispose();
            CompensatePlayerSpeedConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setTimeScaleMultiplier(float timeScaleMultiplier)
        {
            TimeScaleMultiplier = timeScaleMultiplier;
            onValueChanged();
        }

        void setCompensatePlayerSpeed(bool compensatePlayerSpeed)
        {
            CompensatePlayerSpeed = compensatePlayerSpeed;
            onValueChanged();
        }

        public bool TryGetTimeScaleModification(out TimeScaleModificationInfo modificationInfo)
        {
            if (TimeScaleMultiplier == 1f)
            {
                modificationInfo = default;
                return false;
            }

            modificationInfo = new TimeScaleModificationInfo
            {
                TimeScaleMultiplier = TimeScaleMultiplier,
                CompensatePlayerSpeed = CompensatePlayerSpeed
            };

            return true;
        }
    }
}
