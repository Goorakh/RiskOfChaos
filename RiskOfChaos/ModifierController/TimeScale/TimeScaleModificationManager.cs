using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.TimeScale
{
    [ValueModificationManager(typeof(SyncTimeScaleModification), typeof(SyncTimeScale))]
    public class TimeScaleModificationManager : ValueModificationManager<float>
    {
        static TimeScaleModificationManager _instance;
        public static TimeScaleModificationManager Instance => _instance;

        SyncTimeScaleModification _clientSync;

        protected override void Awake()
        {
            base.Awake();
            _clientSync = GetComponent<SyncTimeScaleModification>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);

            TimeUtils.UnpausedTimeScale = 1f;
        }

        public override float InterpolateValue(in float a, in float b, float t)
        {
            return ValueInterpolationFunctionType.Linear.Interpolate(a, b, t);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            TimeUtils.UnpausedTimeScale = GetModifiedValue(1f);
        }
    }
}
