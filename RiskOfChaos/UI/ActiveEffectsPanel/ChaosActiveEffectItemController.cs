using R2API;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
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
            private set
            {
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
            string displayText = _displayingEffect.DisplayName;
            if (_displayingEffect.TimedType == TimedEffectType.FixedDuration && Run.instance)
            {
                float currentTime = Run.instance.GetRunTime(RunTimerType.Realtime);
                float endTime = _displayingEffect.TimeStarted + _displayingEffect.DurationSeconds;

                float timeRemaining = endTime - currentTime;
                displayText += $" ({timeRemaining.ToString(timeRemaining >= 10f ? "F0" : "F1")}s)";
            }

            _effectNameLabel.text = displayText;
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
