namespace RiskOfChaos.ModifierController.Pickups
{
    public class PickupModificationManager : ValueModificationManager<PickupModificationInfo>
    {
        static PickupModificationManager _instance;
        public static PickupModificationManager Instance => _instance;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        public uint BounceCount { get; private set; }

        protected override PickupModificationInfo interpolateValue(in PickupModificationInfo a, in PickupModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return PickupModificationInfo.Interpolate(a, b, t, interpolationType);
        }

        protected override void updateValueModifications()
        {
            PickupModificationInfo modificationInfo = getModifiedValue(new PickupModificationInfo());
            BounceCount = modificationInfo.BounceCount;
        }
    }
}
