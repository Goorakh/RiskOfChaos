using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public class NextEffectDisplayController : MonoBehaviour
    {
        public LanguageTextMeshController EffectText;
        public Image BackdropImage;
        public AnimateUIAlpha FlashController;

        float _lastDisplayedTimeRemaining;
        EffectDisplayData _currentDisplayData;

        void Awake()
        {
            RectTransform rectTransform = (RectTransform)EffectText.transform;

            // These don't want to serialize for some reason
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        void OnEnable()
        {
            beginFlash();

            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
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
            float timeRemaining = displayData.ActivationTime.TimeUntilClamped;
            string timeRemainingString = FormatUtils.FormatTimeSeconds(timeRemaining);

            if (displayData.EffectIndex != ChaosEffectIndex.Invalid)
            {
                ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(displayData.EffectIndex);
                string effectName = effectInfo?.GetDisplayName(displayData.NameFormatter, EffectNameFormatFlags.RuntimeFormatArgs) ?? "NULL";
                EffectText.SetTokenAndFormatArgs("CHAOS_NEXT_EFFECT_DISPLAY_FORMAT", [effectName, timeRemainingString]);
            }
            else
            {
                EffectText.SetTokenAndFormatArgs("CHAOS_NEXT_EFFECT_TIME_REMAINING_DISPLAY_FORMAT", [timeRemainingString]);
            }

            if (_currentDisplayData.EffectIndex != displayData.EffectIndex || Mathf.Abs(timeRemaining - _lastDisplayedTimeRemaining) > 2f)
            {
                beginFlash();
            }

            _currentDisplayData = displayData;
            _lastDisplayedTimeRemaining = timeRemaining;
        }

        void onCurrentLanguageChanged()
        {
            DisplayEffect(_currentDisplayData);
        }
    }
}
