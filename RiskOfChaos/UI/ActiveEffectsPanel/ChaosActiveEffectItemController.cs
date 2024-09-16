using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public class ChaosActiveEffectItemController : MonoBehaviour
    {
        static GameObject _itemPrefab;

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

            ChaosActiveEffectItemController chaosActiveEffectItemController = activeEffectItemObject.AddComponent<ChaosActiveEffectItemController>();
            chaosActiveEffectItemController._effectNameLabel = effectNameLabel;
            chaosActiveEffectItemController._effectNameText = languageTextMeshController;

            activeEffectItemObject.layer = LayerIndex.ui.intVal;
            _itemPrefab = activeEffectItemObject;
        }

        public static ChaosActiveEffectItemController CreateEffectDisplayItem(Transform parent, ActiveEffectItemInfo effectItemInfo)
        {
            ChaosActiveEffectItemController effectItemController = Instantiate(_itemPrefab, parent).GetComponent<ChaosActiveEffectItemController>();
            effectItemController.DisplayingEffect = effectItemInfo;
            return effectItemController;
        }

        ActiveEffectItemInfo _displayingEffect;

        public ActiveEffectItemInfo DisplayingEffect
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
                updateEffectLabel();
            }
        }

        [SerializeField]
        HGTextMeshProUGUI _effectNameLabel;

        [SerializeField]
        LanguageTextMeshController _effectNameText;

        void Start()
        {
            Configs.UI.ActiveEffectsTextColor.SettingChanged += ActiveEffectsTextColor_SettingChanged;
            setTextColor(Configs.UI.ActiveEffectsTextColor.Value);
        }

        void OnEnable()
        {
            updateEffectLabel();
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
        }

        void OnDestroy()
        {
            Configs.UI.ActiveEffectsTextColor.SettingChanged -= ActiveEffectsTextColor_SettingChanged;
        }

        void ActiveEffectsTextColor_SettingChanged(object sender, ConfigChangedArgs<Color> e)
        {
            setTextColor(e.NewValue);
        }

        void setTextColor(Color color)
        {
            _effectNameLabel.color = color;
        }

        void onCurrentLanguageChanged()
        {
            updateEffectLabel();
        }

        void updateEffectLabel()
        {
            if (_displayingEffect.EffectInfo == null)
            {
                _effectNameText.token = string.Empty;
                _effectNameText.formatArgs = [];
                return;
            }

            string token;
            object[] formatArgs;
            switch (_displayingEffect.TimedType)
            {
                case TimedEffectType.UntilStageEnd:
                    int stagesRemaining = Mathf.CeilToInt(_displayingEffect.RemainingStocks);

                    if (stagesRemaining == 1)
                    {
                        token = "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_SINGLE_FORMAT";
                    }
                    else
                    {
                        token = "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_MULTI_FORMAT";
                    }

                    formatArgs = [
                        stagesRemaining
                    ];
                    break;
                case TimedEffectType.FixedDuration when Run.instance:
                    token = "CHAOS_ACTIVE_EFFECT_FIXED_DURATION_FORMAT";

                    float currentTime = Run.instance.GetRunTime(RunTimerType.Realtime);
                    float endTime = _displayingEffect.EndTime;

                    float timeRemaining = endTime - currentTime;

                    formatArgs = [
                        FormatUtils.FormatTimeSeconds(timeRemaining)
                    ];
                    break;
                default:
                    token = "CHAOS_ACTIVE_EFFECT_FALLBACK_FORMAT";
                    formatArgs = [];
                    break;
            }

            _effectNameText.token = token;
            _effectNameText.formatArgs = [
                _displayingEffect.DisplayName,
                ..formatArgs
            ];
        }

        void FixedUpdate()
        {
            if (_displayingEffect.TimedType == TimedEffectType.FixedDuration)
            {
                updateEffectLabel();
            }
        }
    }
}
