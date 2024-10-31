using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using UnityEngine;

namespace RiskOfChaos.ModificationController.UI
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class UIModificationProvider : MonoBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> HudScaleMultiplierConfigBinding { get; private set; }

        float _hudScaleMultiplier = 1f;
        public float HudScaleMultiplier
        {
            get
            {
                float hudScaleMultiplier = _hudScaleMultiplier;

                if (_modificationController && _modificationController.IsInterpolating)
                {
                    hudScaleMultiplier = Mathf.Lerp(1f, hudScaleMultiplier, Ease.InOutQuad(_modificationController.CurrentInterpolationFraction));
                }

                return hudScaleMultiplier;
            }
            set
            {
                if (_hudScaleMultiplier == value)
                    return;

                _hudScaleMultiplier = value;
                onValueChanged();
            }
        }

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            HudScaleMultiplierConfigBinding = new ValueModificationConfigBinding<float>(v => HudScaleMultiplier = v);
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
            HudScaleMultiplierConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }
    }
}
