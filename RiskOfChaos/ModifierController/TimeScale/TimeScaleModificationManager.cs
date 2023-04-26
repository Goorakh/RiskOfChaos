using RiskOfChaos.Utilities;

namespace RiskOfChaos.ModifierController.TimeScale
{
    public class TimeScaleModificationManager : ValueModificationManager<ITimeScaleModificationProvider, float>
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

            TimeUtils.CurrentTimeScale = 1f;
        }

        protected override void updateValueModifications()
        {
            TimeUtils.CurrentTimeScale = getModifiedValue(1f);
        }
    }
}
