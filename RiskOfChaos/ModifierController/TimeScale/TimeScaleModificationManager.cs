using RiskOfChaos.Utilities;

namespace RiskOfChaos.ModifierController.TimeScale
{
    public class TimeScaleModificationManager : NetworkedValueModificationManager<float>
    {
        static TimeScaleModificationManager _instance;
        public static TimeScaleModificationManager Instance => _instance;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            TimeUtils.UnpausedTimeScale = 1f;
        }

        protected override float interpolateValue(in float a, in float b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return interpolationType.Interpolate(a, b, t);
        }

        protected override void updateValueModifications()
        {
            TimeUtils.UnpausedTimeScale = getModifiedValue(1f);
        }
    }
}
