using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class PersistentOverlayController : MonoBehaviour
    {
        public Material Overlay;

        CharacterModel _characterModel;
        TemporaryOverlayInstance _temporaryOverlay;

        void Awake()
        {
            _characterModel = GetComponent<CharacterModel>();
        }

        void OnEnable()
        {
            if (_characterModel)
            {
                _temporaryOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                _temporaryOverlay.duration = float.PositiveInfinity;
                _temporaryOverlay.originalMaterial = Overlay;
                _temporaryOverlay.AddToCharacterModel(_characterModel);
            }
        }

        void OnDisable()
        {
            if (_temporaryOverlay != null)
            {
                _temporaryOverlay.CleanupEffect();
                _temporaryOverlay = null;
            }
        }
    }
}
