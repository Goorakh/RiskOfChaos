using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Effect
{
    [ValueModificationManager(typeof(SyncEffectModification))]
    public sealed class EffectModificationManager : ValueModificationManager<EffectModificationInfo>
    {
        static EffectModificationManager _instance;
        public static EffectModificationManager Instance => _instance;

        SyncEffectModification _clientSync;

        public float DurationMultiplier
        {
            get
            {
                return _clientSync.DurationMultiplier;
            }
            private set
            {
                _clientSync.DurationMultiplier = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _clientSync = GetComponent<SyncEffectModification>();
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
        }

        public override EffectModificationInfo InterpolateValue(in EffectModificationInfo a, in EffectModificationInfo b, float t)
        {
            return EffectModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            EffectModificationInfo modificationInfo = GetModifiedValue(new EffectModificationInfo());

            DurationMultiplier = modificationInfo.DurationMultiplier;
        }

        public bool TryModifyDuration(TimedEffectInfo effectInfo, ref float duration)
        {
            if (effectInfo == null || effectInfo.IgnoreDurationModifiers || !AnyModificationActive || DurationMultiplier == 1f)
                return false;

            duration *= DurationMultiplier;
            return true;
        }
    }
}
