using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
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
            UISkinData uiSkin = UISkins.ActiveEffectsPanel;

            GameObject activeEffectItemObject = Prefabs.CreatePrefab(nameof(RoCContent.LocalPrefabs.ActiveEffectListUIItem), []);

            RectTransform activeEffectItemTransform = activeEffectItemObject.AddComponent<RectTransform>();

            ChaosActiveEffectDisplayController chaosActiveEffectItemController = activeEffectItemObject.AddComponent<ChaosActiveEffectDisplayController>();

            LayoutElement layoutElement = activeEffectItemObject.AddComponent<LayoutElement>();

            VerticalLayoutGroup verticalLayoutGroup = activeEffectItemObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
            verticalLayoutGroup.childForceExpandWidth = true;

            // NameLabel
            {
                GameObject nameHolder = new GameObject("Name");
                RectTransform rectTransform = nameHolder.AddComponent<RectTransform>();
                rectTransform.SetParent(activeEffectItemObject.transform, false);

                HGTextMeshProUGUI effectNameLabel = nameHolder.AddComponent<HGTextMeshProUGUI>();
                effectNameLabel.alignment = TextAlignmentOptions.Center;
                effectNameLabel.fontSizeMin = 8f;
                effectNameLabel.fontSizeMax = 12f;
                effectNameLabel.fontSize = 12f;
                effectNameLabel.enableAutoSizing = true;
                effectNameLabel.enableWordWrapping = false;

                LabelSkinController labelSkinController = nameHolder.AddComponent<LabelSkinController>();
                labelSkinController.label = effectNameLabel;
                labelSkinController.labelType = LabelSkinController.LabelType.Default;
                labelSkinController.skinData = uiSkin;

                LanguageTextMeshController languageTextMeshController = nameHolder.AddComponent<LanguageTextMeshController>();

                LayoutElement nameLayoutElement = nameHolder.AddComponent<LayoutElement>();

                chaosActiveEffectItemController._effectNameLabel = effectNameLabel;
                chaosActiveEffectItemController._effectNameText = languageTextMeshController;
            }

            // SubtitleLabel
            {
                GameObject subtitleHolder = new GameObject("Subtitle");
                RectTransform rectTransform = subtitleHolder.AddComponent<RectTransform>();
                rectTransform.SetParent(activeEffectItemObject.transform, false);

                HGTextMeshProUGUI subtitleNameLabel = subtitleHolder.AddComponent<HGTextMeshProUGUI>();
                subtitleNameLabel.alignment = TextAlignmentOptions.Center;
                subtitleNameLabel.fontSizeMin = 7f;
                subtitleNameLabel.fontSizeMax = 10f;
                subtitleNameLabel.fontSize = 10f;
                subtitleNameLabel.enableAutoSizing = true;
                subtitleNameLabel.enableWordWrapping = true;

                LabelSkinController labelSkinController = subtitleHolder.AddComponent<LabelSkinController>();
                labelSkinController.label = subtitleNameLabel;
                labelSkinController.labelType = LabelSkinController.LabelType.Detail;
                labelSkinController.skinData = uiSkin;

                LayoutElement nameLayoutElement = subtitleHolder.AddComponent<LayoutElement>();

                chaosActiveEffectItemController._effectSubtitleLabel = subtitleNameLabel;

                subtitleHolder.SetActive(false);
            }

            activeEffectItemObject.layer = LayerIndex.ui.intVal;

            localPrefabs.Add(activeEffectItemObject);
        }

        [SerializeField]
        HGTextMeshProUGUI _effectNameLabel;

        [SerializeField]
        LanguageTextMeshController _effectNameText;

        [SerializeField]
        HGTextMeshProUGUI _effectSubtitleLabel;

        ChaosEffectInfo _displayingEffectInfo;
        ChaosEffectComponent _displayingEffect;
        ChaosEffectNameComponent _displayingEffectNameComponent;
        ChaosEffectSubtitleComponent _displayingEffectSubtitleComponent;
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

                if (_displayingEffectSubtitleComponent)
                {
                    _displayingEffectSubtitleComponent.OnSubtitleChanged -= onEffectSubtitleChanged;
                }

                _displayingEffect = value;

                ChaosEffectInfo displayingEffectInfo = null;
                ChaosEffectNameComponent displayingEffectNameComponent = null;
                ChaosEffectSubtitleComponent displayingEffectSubtitleComponent = null;
                ChaosEffectDurationComponent durationComponent = null;
                if (_displayingEffect)
                {
                    displayingEffectInfo = _displayingEffect.ChaosEffectInfo;
                    displayingEffectNameComponent = _displayingEffect.GetComponent<ChaosEffectNameComponent>();
                    displayingEffectSubtitleComponent = _displayingEffect.GetComponent<ChaosEffectSubtitleComponent>();
                    durationComponent = _displayingEffect.GetComponent<ChaosEffectDurationComponent>();
                }

                _displayingEffectInfo = displayingEffectInfo;
                _displayingEffectNameComponent = displayingEffectNameComponent;
                _displayingEffectSubtitleComponent = displayingEffectSubtitleComponent;
                _displayingEffectDurationComponent = durationComponent;

                if (_displayingEffectNameComponent)
                {
                    _displayingEffectNameComponent.NameFormatterProvider.OnNameFormatterChanged += onEffectNameFormatterChanged;
                }

                if (_displayingEffectSubtitleComponent)
                {
                    _displayingEffectSubtitleComponent.OnSubtitleChanged += onEffectSubtitleChanged;
                }

                updateEffectLabel();
            }
        }

        float _currentlyDisplayedTimeRemaining;

        bool _effectLabelDirty;

        void OnEnable()
        {
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
            Configs.UI.ActiveEffectsTextColor.SettingChanged += onActiveEffectsTextColorChanged;

            updateEffectLabel();
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
            if (_displayingEffectDurationComponent)
            {
                float timeRemaining = _displayingEffectDurationComponent.Remaining;
                if (timeRemaining != _currentlyDisplayedTimeRemaining)
                {
                    markEffectLabelDirty();
                }
            }

            if (_effectLabelDirty)
            {
                _effectLabelDirty = false;
                updateEffectLabel();
            }
        }

        void onActiveEffectsTextColorChanged(object sender, ConfigChangedArgs<Color> e)
        {
            updateTextColor();
        }

        void updateTextColor()
        {
            Color textColor = Configs.UI.ActiveEffectsTextColor.Value;

            _effectNameLabel.color = textColor;

            if (_effectSubtitleLabel)
            {
                _effectSubtitleLabel.color = textColor;
            }
        }

        void onCurrentLanguageChanged()
        {
            markEffectLabelDirty();
        }

        void onEffectNameFormatterChanged()
        {
            markEffectLabelDirty();
        }

        void onEffectSubtitleChanged()
        {
            markEffectLabelDirty();
        }

        void markEffectLabelDirty()
        {
            _effectLabelDirty = true;
        }

        void updateEffectLabel()
        {
            string displayName = "???";
            if (_displayingEffectInfo != null)
            {
                EffectNameFormatterProvider nameFormatterProvider = _displayingEffectInfo.StaticDisplayNameFormatterProvider;
                if (_displayingEffectNameComponent)
                {
                    nameFormatterProvider = _displayingEffectNameComponent.NameFormatterProvider;
                }

                EffectNameFormatter nameFormatter = nameFormatterProvider.NameFormatter;
                displayName = nameFormatter.GetEffectDisplayName(_displayingEffectInfo, EffectNameFormatFlags.RuntimeFormatArgs);
            }

            string subtitle = string.Empty;
            if (_displayingEffectSubtitleComponent)
            {
                subtitle = _displayingEffectSubtitleComponent.Subtitle;
            }

            float timeRemaining = -1f;
            string token = "CHAOS_ACTIVE_EFFECT_FALLBACK_FORMAT";
            object[] formatArgs = [displayName];

            if (_displayingEffectDurationComponent)
            {
                timeRemaining = _displayingEffectDurationComponent.Remaining;

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
            }

            _effectNameText.SetTokenAndFormatArgs(token, formatArgs);

            bool hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
            if (_effectSubtitleLabel)
            {
                _effectSubtitleLabel.gameObject.SetActive(hasSubtitle);
                if (hasSubtitle)
                {
                    _effectSubtitleLabel.text = subtitle;
                }
            }

            _currentlyDisplayedTimeRemaining = timeRemaining;

            updateTextColor();
        }
    }
}
