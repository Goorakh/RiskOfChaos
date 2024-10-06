using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public class ChaosActiveEffectDisplayController : MonoBehaviour
    {
        public static GameObject ItemPrefab { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            GameObject activeEffectItemObject = NetPrefabs.CreateEmptyPrefabObject("ActiveEffectItem", false);
            RectTransform activeEffectItemTransform = activeEffectItemObject.AddComponent<RectTransform>();

            LayoutElement layoutElement = activeEffectItemObject.AddComponent<LayoutElement>();

            HGTextMeshProUGUI effectNameLabel = activeEffectItemObject.AddComponent<HGTextMeshProUGUI>();
            effectNameLabel.alignment = TextAlignmentOptions.Center;
            effectNameLabel.fontSizeMin = 8f;
            effectNameLabel.fontSizeMax = 12f;
            effectNameLabel.fontSize = 12f;
            effectNameLabel.enableAutoSizing = true;
            effectNameLabel.enableWordWrapping = false;

            LanguageTextMeshController languageTextMeshController = activeEffectItemObject.AddComponent<LanguageTextMeshController>();

            ChaosActiveEffectDisplayController chaosActiveEffectItemController = activeEffectItemObject.AddComponent<ChaosActiveEffectDisplayController>();
            chaosActiveEffectItemController._effectNameLabel = effectNameLabel;
            chaosActiveEffectItemController._effectNameText = languageTextMeshController;

            activeEffectItemObject.layer = LayerIndex.ui.intVal;
            ItemPrefab = activeEffectItemObject;
        }

        [SerializeField]
        HGTextMeshProUGUI _effectNameLabel;

        [SerializeField]
        LanguageTextMeshController _effectNameText;

        ChaosEffectComponent _displayingEffect;
        ChaosEffectDurationComponent _displayingEffectDurationComponent;

        public ChaosEffectComponent DisplayingEffect
        {
            get
            {
                return _displayingEffect;
            }
            set
            {
                if (_displayingEffect == value)
                    return;

                _displayingEffect = value;

                ChaosEffectDurationComponent durationComponent = null;
                if (_displayingEffect)
                {
                    durationComponent = _displayingEffect.GetComponent<ChaosEffectDurationComponent>();
                }

                _displayingEffectDurationComponent = durationComponent;

                if (enabled)
                {
                    updateEffectLabel(true);
                }
            }
        }

        float _displayingEffectTimeRemaining;

        void OnEnable()
        {
            updateTextColor();
            updateEffectLabel(true);

            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
            Configs.UI.ActiveEffectsTextColor.SettingChanged += onActiveEffectsTextColorChanged;
            ChaosEffectInfo.OnEffectNameFormatterDirty += onEffectNameFormatterDirty;
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
            Configs.UI.ActiveEffectsTextColor.SettingChanged -= onActiveEffectsTextColorChanged;
            ChaosEffectInfo.OnEffectNameFormatterDirty -= onEffectNameFormatterDirty;
        }

        void FixedUpdate()
        {
            updateEffectLabel(false);
        }

        void onActiveEffectsTextColorChanged(object sender, ConfigChangedArgs<Color> e)
        {
            updateTextColor();
        }

        void updateTextColor()
        {
            _effectNameLabel.color = Configs.UI.ActiveEffectsTextColor.Value;
        }

        void onCurrentLanguageChanged()
        {
            updateEffectLabel(true);
        }

        void onEffectNameFormatterDirty(ChaosEffectInfo effectInfo)
        {
            updateEffectLabel(true);
        }

        void updateEffectLabel(bool forceUpdate)
        {
            if (!_displayingEffect || !_displayingEffectDurationComponent)
            {
                _effectNameText.SetTokenAndFormatArgs(string.Empty, []);
                return;
            }

            float timeRemaining = _displayingEffectDurationComponent.Remaining;
            if (!forceUpdate && timeRemaining == _displayingEffectTimeRemaining)
                return;

            ChaosEffectInfo effectInfo = _displayingEffect.ChaosEffectInfo;

            string displayName = "<color=red>INVALID EFFECT</color>";
            if (effectInfo != null)
            {
                EffectNameFormatter effectNameFormatter = _displayingEffect.EffectNameFormatter;
                if (effectNameFormatter == null)
                {
                    Log.Warning($"Unable to resolve name formatter for {_displayingEffect.name}, using local formatter");
                    effectNameFormatter = effectInfo.LocalDisplayNameFormatter;
                }

                displayName = effectInfo.GetDisplayName(effectNameFormatter, EffectNameFormatFlags.RuntimeFormatArgs);
            }

            string token = "CHAOS_ACTIVE_EFFECT_FALLBACK_FORMAT";
            object[] formatArgs = [displayName];
            switch (_displayingEffectDurationComponent.TimedType)
            {
                case TimedEffectType.UntilStageEnd:
                    int stagesRemaining = Mathf.CeilToInt(timeRemaining);

                    if (stagesRemaining == 1)
                    {
                        token = "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_SINGLE_FORMAT";
                    }
                    else
                    {
                        token = "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_MULTI_FORMAT";
                    }

                    formatArgs = [
                        displayName,
                        stagesRemaining
                    ];
                    break;
                case TimedEffectType.FixedDuration:
                    token = "CHAOS_ACTIVE_EFFECT_FIXED_DURATION_FORMAT";

                    formatArgs = [
                        displayName,
                        FormatUtils.FormatTimeSeconds(timeRemaining)
                    ];
                    break;
            }

            _effectNameText.SetTokenAndFormatArgs(token, formatArgs);

            _displayingEffectTimeRemaining = timeRemaining;
        }
    }
}
