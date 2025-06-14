using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Components
{
    public class PersistentOverlayController : MonoBehaviour
    {
        public Material OverlayMaterial;
        public AssetReferenceT<Material> OverlayMaterialReference;

        AssetOrDirectReference<Material> _overlayReference;

        CharacterModel _characterModel;
        TemporaryOverlayInstance _temporaryOverlay;

        void Awake()
        {
            _characterModel = GetComponent<CharacterModel>();

            _overlayReference = new AssetOrDirectReference<Material>
            {
                unloadType = AsyncReferenceHandleUnloadType.AtWill,
                address = OverlayMaterialReference,
                directRef = OverlayMaterial
            };
        }

        void OnDestroy()
        {
            _overlayReference.Reset();
        }

        void OnEnable()
        {
            if (_overlayReference.IsLoaded())
            {
                setOverlayActive(true, _overlayReference.Result);
            }

            _overlayReference.onValidReferenceDiscovered += onOverlayMaterialDiscovered;
        }

        void OnDisable()
        {
            _overlayReference.onValidReferenceDiscovered -= onOverlayMaterialDiscovered;
            setOverlayActive(false, null);
        }

        void onOverlayMaterialDiscovered(Material overlayMaterial)
        {
            setOverlayActive(true, overlayMaterial);
        }

        void setOverlayActive(bool enable, Material overlayMaterial)
        {
            bool hasOverlay = _temporaryOverlay != null;
            if (enable == hasOverlay)
                return;

            if (enable)
            {
                if (_characterModel)
                {
                    _temporaryOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                    _temporaryOverlay.duration = float.PositiveInfinity;
                    _temporaryOverlay.originalMaterial = overlayMaterial;
                    _temporaryOverlay.AddToCharacterModel(_characterModel);
                }
            }
            else
            {
                if (_temporaryOverlay != null)
                {
                    _temporaryOverlay.CleanupEffect();
                    _temporaryOverlay = null;
                }
            }
        }
    }
}
