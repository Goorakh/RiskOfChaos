using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling;
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
        [ContentInitializer]
        static void LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            GameObject activeEffectItemObject = Prefabs.CreatePrefab(nameof(RoCContent.LocalPrefabs.ActiveEffectListUIItem), []);

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

            localPrefabs.Add(activeEffectItemObject);
        }

        [SerializeField]
        HGTextMeshProUGUI _effectNameLabel;

        [SerializeField]
        LanguageTextMeshController _effectNameText;

        ChaosEffectInfo _displayingEffectInfo;
        ChaosEffectComponent _displayingEffect;
        ChaosEffectNameComponent _displayingEffectNameComponent;
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

                if (_displayingEffectNameComponent)
                {
                    _displayingEffectNameComponent.NameFormatterProvider.OnNameFormatterChanged -= onEffectNameFormatterChanged;
                }

                _displayingEffect = value;

                ChaosEffectInfo displayingEffectInfo = null;
                ChaosEffectNameComponent displayingEffectNameComponent = null;
                ChaosEffectDurationComponent durationComponent = null;
                if (_displayingEffect)
                {
                    displayingEffectInfo = _displayingEffect.ChaosEffectInfo;
                    displayingEffectNameComponent = _displayingEffect.GetComponent<ChaosEffectNameComponent>();
                    durationComponent = _displayingEffect.GetComponent<ChaosEffectDurationComponent>();
                }

                _displayingEffectInfo = displayingEffectInfo;
                _displayingEffectNameComponent = displayingEffectNameComponent;
                _displayingEffectDurationComponent = durationComponent;

                if (_displayingEffectNameComponent)
                {
                    _displayingEffectNameComponent.NameFormatterProvider.OnNameFormatterChanged += onEffectNameFormatterChanged;
                }

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
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
            Configs.UI.ActiveEffectsTextColor.SettingChanged -= onActiveEffectsTextColorChanged;
        }

        void OnDestroy()
        {
            DisplayingEffect = null;
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

        void onEffectNameFormatterChanged()
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

            string displayName = "???";
            if (_displayingEffectInfo != null)
            {
                EffectNameFormatterProvider nameFormatterProvider = _displayingEffectInfo.StaticDisplayNameFormatterProvider;
                if (_displayingEffectNameComponent)
                {
                    nameFormatterProvider = _displayingEffectNameComponent.NameFormatterProvider;
                }

                displayName = nameFormatterProvider.NameFormatter.GetEffectDisplayName(_displayingEffectInfo, EffectNameFormatFlags.RuntimeFormatArgs);
            }

            string token;
            object[] formatArgs;
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
                default:
                    token = "CHAOS_ACTIVE_EFFECT_FALLBACK_FORMAT";
                    formatArgs = [displayName];

                    break;
            }

            _effectNameText.SetTokenAndFormatArgs(token, formatArgs);

            _displayingEffectTimeRemaining = timeRemaining;
        }
    }
}
