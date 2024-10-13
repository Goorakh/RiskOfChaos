using RiskOfChaos.EffectHandling.EffectClassAttributes;
using UnityEngine;

namespace RiskOfChaos.Components.MaterialInterpolation
{
    [RequiredComponents(typeof(GenericInterpolationComponent))]
    public sealed class MaterialInterpolator : MonoBehaviour
    {
        GenericInterpolationComponent _interpolation;

        IMaterialPropertyInterpolator[] _propertyInterpolators;

        Material _sharedMaterial;

        public Material MaterialInstance { get; private set; }

        void Awake()
        {
            _propertyInterpolators = GetComponents<IMaterialPropertyInterpolator>();

            _interpolation = GetComponent<GenericInterpolationComponent>();
            _interpolation.OnInterpolationChanged += updateMaterialProperties;
        }

        void OnDestroy()
        {
            SetMaterial(null);
        }

        public void SetMaterial(Material mat)
        {
            if (mat && mat == _sharedMaterial)
                return;

            if (MaterialInstance)
            {
                Destroy(MaterialInstance);
                MaterialInstance = null;
            }

            _sharedMaterial = mat;
            MaterialInstance = _sharedMaterial ? new Material(_sharedMaterial) : null;

            if (MaterialInstance)
            {
                updateMaterialProperties();
            }
        }

        void updateMaterialProperties()
        {
            if (!MaterialInstance)
                return;

            float interpolationFraction = _interpolation.CurrentInterpolationFraction;

            foreach (IMaterialPropertyInterpolator propertyInterpolator in _propertyInterpolators)
            {
                propertyInterpolator.SetValues(MaterialInstance, interpolationFraction);
            }
        }
    }
}
