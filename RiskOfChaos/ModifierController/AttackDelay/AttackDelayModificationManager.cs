using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.AttackDelay
{
    [ValueModificationManager(typeof(SyncAttackDelayModification))]
    public class AttackDelayModificationManager : ValueModificationManager<AttackDelayModificationInfo>
    {
        static AttackDelayModificationManager _instance;
        public static AttackDelayModificationManager Instance => _instance;

        SyncAttackDelayModification _clientSync;

        public float TotalAttackDelay
        {
            get
            {
                return _clientSync.TotalAttackDelay;
            }
            private set
            {
                _clientSync.TotalAttackDelay = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _clientSync = GetComponent<SyncAttackDelayModification>();
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

        public override AttackDelayModificationInfo InterpolateValue(in AttackDelayModificationInfo a, in AttackDelayModificationInfo b, float t)
        {
            return AttackDelayModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            AttackDelayModificationInfo attackDelayModificationInfo = GetModifiedValue(new AttackDelayModificationInfo(0f));
            TotalAttackDelay = attackDelayModificationInfo.TotalDelay;
        }
    }
}
