using R2API;
using RiskOfChaos.Networking.Components;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public class ChaosActiveEffectsDisplayController : MonoBehaviour
    {
        static GameObject _activeEffectsPanelPrefab;

        [SystemInitializer]
        static void Init()
        {
            GameObject hudInfoPanelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClassicRun/ClassicRunInfoHudPanel.prefab").WaitForCompletion();
            if (!hudInfoPanelPrefab)
            {
                Log.Warning("Unable to find HUD info panel prefab");
                return;
            }

            ChildLocator childLocator = hudInfoPanelPrefab.GetComponent<ChildLocator>();
            if (!childLocator)
            {
                Log.Warning("Info panel is missing ChildLocator component");
                return;
            }

            Transform rightInfoBar = childLocator.FindChild("RightInfoBar");
            if (!rightInfoBar)
            {
                Log.Warning("Could not find RightInfoBar");
                return;
            }

            Transform objectivePanel = rightInfoBar.Find("ObjectivePanel");
            if (!objectivePanel)
            {
                Log.Warning("Could not find ObjectivePanel");
                return;
            }

            _activeEffectsPanelPrefab = objectivePanel.gameObject.InstantiateClone("ActiveEffectsPanel", false);

            if (_activeEffectsPanelPrefab.TryGetComponent(out LayoutGroup layoutGroup))
            {
                layoutGroup.padding.bottom = 10;
            }

            ChaosActiveEffectsDisplayController activeEffectsDisplayController = _activeEffectsPanelPrefab.AddComponent<ChaosActiveEffectsDisplayController>();

            ObjectivePanelController objectivePanelController = _activeEffectsPanelPrefab.GetComponent<ObjectivePanelController>();

            ChaosActiveEffectItemController.InitializePrefab(objectivePanelController.objectiveTrackerPrefab);
            activeEffectsDisplayController._activeEffectsContainer = objectivePanelController.objectiveTrackerContainer;

            Destroy(objectivePanelController);
            Destroy(_activeEffectsPanelPrefab.GetComponent<HudObjectiveTargetSetter>());

            Transform titleLabelTransform = _activeEffectsPanelPrefab.transform.Find("Label");
            if (titleLabelTransform)
            {
                if (titleLabelTransform.TryGetComponent(out LayoutElement layoutElement))
                {
                    layoutElement.minHeight = 10f;
                    layoutElement.preferredHeight = 10f;
                }

                if (titleLabelTransform.TryGetComponent(out LanguageTextMeshController languageTextController))
                {
                    languageTextController.token = "CHAOS_ACTIVE_EFFECTS_BAR_TITLE";
                }
                else
                {
                    Log.Warning("Title is missing LanguageTextMeshController component");
                }

                if (titleLabelTransform.TryGetComponent(out activeEffectsDisplayController._activeEffectsTitleLabel))
                {
                    activeEffectsDisplayController._activeEffectsTitleLabel.fontSize = 14f;
                }
                else
                {
                    Log.Warning("Title is missing HGTextMeshProUGUI component");
                }

                if (titleLabelTransform.TryGetComponent(out LabelSkinController labelSkinController))
                {
                    Destroy(labelSkinController);
                }
            }
            else
            {
                Log.Warning("Unable to find title label");
            }
        }

        public static ChaosActiveEffectsDisplayController Create(ChaosUIController chaosUIController)
        {
            HUD hud = chaosUIController.GetComponent<HUD>();
            if (!hud)
            {
                Log.Warning($"Unable to find HUD component on {chaosUIController}");
                return null;
            }

            GameObject gameModeUI = hud.gameModeUiInstance;
            if (!gameModeUI)
            {
                Log.Warning("No gameModeUiInstance");
                return null;
            }

            ChildLocator childLocator = gameModeUI.GetComponent<ChildLocator>();
            Transform rightInfoBar = childLocator.FindChild("RightInfoBar");
            if (!rightInfoBar)
            {
                Log.Warning("Could not find RightInfoBar");
                return null;
            }

            ChaosActiveEffectsDisplayController displayController = Instantiate(_activeEffectsPanelPrefab, rightInfoBar).GetComponent<ChaosActiveEffectsDisplayController>();

            return displayController;
        }

        [SerializeField]
        RectTransform _activeEffectsContainer;

        [SerializeField]
        HGTextMeshProUGUI _activeEffectsTitleLabel;

        readonly List<ChaosActiveEffectItemController> _activeEffectItems = new List<ChaosActiveEffectItemController>();

        void OnEnable()
        {
            for (int i = 0; i < _activeEffectsContainer.childCount; i++)
            {
                Transform child = _activeEffectsContainer.GetChild(i);
                if (child)
                {
                    Destroy(child.gameObject);
                }
            }

            ActiveTimedEffectsProvider.OnActiveEffectsChanged += onActiveEffectsChanged;

            if (ActiveTimedEffectsProvider.Instance)
            {
                onActiveEffectsChanged(ActiveTimedEffectsProvider.Instance.GetActiveEffects());
            }
        }

        void FixedUpdate()
        {
            if (ActiveTimedEffectsProvider.Instance && _activeEffectsContainer.childCount != ActiveTimedEffectsProvider.Instance.NumActiveDisplayedEffects)
            {
#if DEBUG
                Log.Debug("Displayed effects differ from active effects, updating display");
#endif

                onActiveEffectsChanged(ActiveTimedEffectsProvider.Instance.GetActiveEffects());
            }
        }

        void OnDisable()
        {
            ActiveTimedEffectsProvider.OnActiveEffectsChanged -= onActiveEffectsChanged;
        }

        bool isDisplayingEffect(ActiveEffectItemInfo effectItemInfo)
        {
            return _activeEffectItems.Exists(displayController => displayController.DisplayingEffect == effectItemInfo);
        }

        void onActiveEffectsChanged(ActiveEffectItemInfo[] activeEffects)
        {
            _activeEffectItems.RemoveAll(display => !display);

            // Remove displays for effects that should no longer show
            for (int i = _activeEffectItems.Count - 1; i >= 0; i--)
            {
                if (!_activeEffectItems[i].DisplayingEffect.ShouldDisplay || Array.IndexOf(activeEffects, _activeEffectItems[i].DisplayingEffect) < 0)
                {
                    Destroy(_activeEffectItems[i].gameObject);
                    _activeEffectItems.RemoveAt(i);
                }
            }

            // Add display for effects that should be shown, but aren't
            _activeEffectItems.AddRange(from effect in activeEffects
                                        where effect.ShouldDisplay && !isDisplayingEffect(effect)
                                        select ChaosActiveEffectItemController.CreateEffectDisplayItem(_activeEffectsContainer, effect));

            // Hacky way of "hiding" the panel if there aren't any effects.
            _activeEffectsTitleLabel.gameObject.SetActive(_activeEffectItems.Count > 0);
        }
    }
}
