using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.ModifierController.TimeScale
{
    [ValueModificationManager(typeof(SyncTimeScale))]
    public class TimeScaleModificationManager : NetworkedValueModificationManager<float>
    {
        static TimeScaleModificationManager _instance;
        public static TimeScaleModificationManager Instance => _instance;

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
            TimeUtils.UnpausedTimeScale = GetModifiedValue(1f);
        }
    }
}
