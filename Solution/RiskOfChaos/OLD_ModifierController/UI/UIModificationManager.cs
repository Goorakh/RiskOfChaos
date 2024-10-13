using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.OLD_ModifierController.UI
{
    [ValueModificationManager]
    public class UIModificationManager : ValueModificationManager<UIModificationData>
    {
        static UIModificationManager _instance;
        public static UIModificationManager Instance => _instance;

        public delegate void OnHudScaleMultiplierChangedDelegate(float newScaleMultiplier);
        public static event OnHudScaleMultiplierChangedDelegate OnHudScaleMultiplierChanged;

        float _hudScaleMultiplier = 1f;
        public float HudScaleMultiplier
        {
            get
            {
                return _hudScaleMultiplier;
            }
            set
            {
                if (_hudScaleMultiplier == value)
                    return;

                _hudScaleMultiplier = value;

                OnHudScaleMultiplierChanged?.Invoke(_hudScaleMultiplier);
            }
        }

        public override UIModificationData InterpolateValue(in UIModificationData a, in UIModificationData b, float t)
        {
            return UIModificationData.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            UIModificationData modificationData = GetModifiedValue(new UIModificationData());

            HudScaleMultiplier = modificationData.ScaleMultiplier;
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
    }
}
