using RiskOfChaos.EffectHandling;
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

        ChaosEffectIndex _currentDisplayingEffectIndex;

        void Awake()
        {
            // These don't want to serialize for some reason
            EffectText.rectTransform.sizeDelta = Vector2.zero;
            EffectText.rectTransform.anchoredPosition = Vector2.zero;
        }

        public void DisplayEffect(EffectDisplayData displayData)
        {
            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(displayData.EffectIndex);

            string effectName = effectInfo?.GetDisplayName(displayData.NameFormatter, EffectNameFormatFlags.RuntimeFormatArgs) ?? "NULL";

            string timeRemainingString = displayData.TimeRemaining.ToString(displayData.TimeRemaining >= 10f ? "F0" : "F1");

            EffectText.text = Language.GetStringFormatted("CHAOS_NEXT_EFFECT_DISPLAY_FORMAT", effectName, timeRemainingString);

            if (_currentDisplayingEffectIndex != displayData.EffectIndex)
            {
                if (FlashController)
                {
                    FlashController.time = 0f;
                }
            }

            _currentDisplayingEffectIndex = displayData.EffectIndex;
        }
    }
}
