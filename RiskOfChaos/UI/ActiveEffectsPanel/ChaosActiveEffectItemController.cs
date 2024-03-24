using R2API;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public class ChaosActiveEffectItemController : MonoBehaviour
    {
        static GameObject _itemPrefab;

        static readonly StringBuilder _displayNameStringBuilder = new StringBuilder();

        internal static void InitializePrefab(GameObject objectiveTrackerPrefab)
        {
            if (!objectiveTrackerPrefab)
            {
                Log.Warning($"{nameof(objectiveTrackerPrefab)} is null");
                return;
            }

            _itemPrefab = objectiveTrackerPrefab.InstantiateClone("ActiveEffectItem", false);
            VerticalLayoutGroup verticalLayoutGroup = _itemPrefab.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup)
            {
                verticalLayoutGroup.enabled = false;
            }

            LayoutElement layoutElement = _itemPrefab.GetComponent<LayoutElement>();
            if (layoutElement)
            {
                layoutElement.enabled = true;
                layoutElement.minHeight = 20f;
            }

            Transform checkBoxTransform = _itemPrefab.transform.Find("Checkbox");
            if (checkBoxTransform)
            {
                Destroy(checkBoxTransform.gameObject);
            }

            ChaosActiveEffectItemController activeEffectItemController = _itemPrefab.AddComponent<ChaosActiveEffectItemController>();

            Transform labelTransform = _itemPrefab.transform.Find("Label");
            HGTextMeshProUGUI effectNameLabel = labelTransform.GetComponent<HGTextMeshProUGUI>();
            effectNameLabel.horizontalAlignment = HorizontalAlignmentOptions.Center;
            effectNameLabel.enableAutoSizing = true;
            effectNameLabel.enableWordWrapping = false;

            effectNameLabel.rectTransform.sizeDelta = new Vector2(140f, 25f);
            effectNameLabel.rectTransform.localPosition = new Vector3(0f, 0f, 0f);

            activeEffectItemController._effectNameLabel = effectNameLabel;

            _itemPrefab.SetActive(true);
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

        void Start()
        {
            Configs.UI.ActiveEffectsTextColor.SettingChanged += ActiveEffectsTextColor_SettingChanged;
            setTextColor(Configs.UI.ActiveEffectsTextColor.Value);
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

        void updateEffectLabel()
        {
            _displayNameStringBuilder.Clear();
            _displayNameStringBuilder.Append(_displayingEffect.DisplayName);

            switch (_displayingEffect.TimedType)
            {
                case TimedEffectType.UntilStageEnd:
                    int stagesRemaining = Mathf.CeilToInt(_displayingEffect.RemainingStocks);

                    string formatToken;
                    if (stagesRemaining == 1)
                    {
                        formatToken = "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_SINGLE_FORMAT";
                    }
                    else
                    {
                        formatToken = "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_MULTI_FORMAT";
                    }

                    _displayNameStringBuilder.Append(' ');
                    _displayNameStringBuilder.Append(Language.GetStringFormatted(formatToken, stagesRemaining));
                    break;
                case TimedEffectType.FixedDuration when Run.instance:
                    float currentTime = Run.instance.GetRunTime(RunTimerType.Realtime);
                    float endTime = _displayingEffect.EndTime;

                    float timeRemaining = endTime - currentTime;
                    _displayNameStringBuilder.Append(' ');

                    _displayNameStringBuilder.Append(Language.GetStringFormatted("CHAOS_ACTIVE_EFFECT_FIXED_DURATION_FORMAT", FormatUtils.FormatTimeSeconds(timeRemaining)));

                    break;
            }

            _effectNameLabel.text = _displayNameStringBuilder.Take();
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
