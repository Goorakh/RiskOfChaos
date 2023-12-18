using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public class NextEffectDisplayController : MonoBehaviour
    {
        public HGTextMeshProUGUI EffectText;
        public Image BackdropImage;
        public AnimateUIAlpha FlashController;

        float _lastDisplayedTimeRemaining;
        ChaosEffectIndex _currentDisplayingEffectIndex;

        void Awake()
        {
            // These don't want to serialize for some reason
            EffectText.rectTransform.sizeDelta = Vector2.zero;
            EffectText.rectTransform.anchoredPosition = Vector2.zero;
        }

        void OnEnable()
        {
            beginFlash();
        }

        void beginFlash()
        {
            if (FlashController)
            {
                FlashController.time = 0f;
            }
        }

        public void DisplayEffect(EffectDisplayData displayData)
        {
            string timeRemainingString = displayData.TimeRemaining.ToString(displayData.TimeRemaining >= 10f ? "F0" : "F1");

            if (displayData.EffectIndex != ChaosEffectIndex.Invalid)
            {
                ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(displayData.EffectIndex);
                string effectName = effectInfo?.GetDisplayName(displayData.NameFormatter, EffectNameFormatFlags.RuntimeFormatArgs) ?? "NULL";
                EffectText.text = Language.GetStringFormatted("CHAOS_NEXT_EFFECT_DISPLAY_FORMAT", effectName, timeRemainingString);
            }
            else
            {
                EffectText.text = Language.GetStringFormatted("CHAOS_NEXT_EFFECT_TIME_REMAINING_DISPLAY_FORMAT", timeRemainingString);
            }

            if (_currentDisplayingEffectIndex != displayData.EffectIndex || displayData.TimeRemaining - _lastDisplayedTimeRemaining > 1f)
            {
                beginFlash();
            }

            _currentDisplayingEffectIndex = displayData.EffectIndex;
            _lastDisplayedTimeRemaining = displayData.TimeRemaining;
        }
    }
}
