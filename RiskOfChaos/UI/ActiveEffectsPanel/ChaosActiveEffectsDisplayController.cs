using HG;
using R2API;
using RiskOfChaos.Networking.Components;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

            ChaosActiveEffectsDisplayController activeEffectsDisplayController = _activeEffectsPanelPrefab.AddComponent<ChaosActiveEffectsDisplayController>();

            ObjectivePanelController objectivePanelController = _activeEffectsPanelPrefab.GetComponent<ObjectivePanelController>();

            ChaosActiveEffectItemController.InitializePrefab(objectivePanelController.objectiveTrackerPrefab);
            activeEffectsDisplayController._activeEffectsContainer = objectivePanelController.objectiveTrackerContainer;

            Destroy(objectivePanelController);

            Transform titleLabelTransform = _activeEffectsPanelPrefab.transform.Find("Label");
            if (titleLabelTransform)
            {
                LanguageTextMeshController languageTextController = titleLabelTransform.GetComponent<LanguageTextMeshController>();
                if (languageTextController)
                {
                    languageTextController.token = "CHAOS_ACTIVE_EFFECTS_BAR_TITLE";
                }
                else
                {
                    Log.Warning("Title is missing LanguageTextMeshController component");
                }

                activeEffectsDisplayController._activeEffectsTitleLabel = titleLabelTransform.GetComponent<HGTextMeshProUGUI>();
                if (activeEffectsDisplayController._activeEffectsTitleLabel)
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
            ActiveTimedEffectsProvider.OnActiveEffectsChanged += onActiveEffectsChanged;

            if (_activeEffectsContainer.childCount > 0)
            {
                for (int i = 0; i < _activeEffectsContainer.childCount; i++)
                {
                    Transform child = _activeEffectsContainer.GetChild(i);
                    if (child)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            if (ActiveTimedEffectsProvider.Instance)
            {
                onActiveEffectsChanged(ActiveTimedEffectsProvider.Instance.GetActiveEffects());
            }
        }

        void OnDisable()
        {
            ActiveTimedEffectsProvider.OnActiveEffectsChanged -= onActiveEffectsChanged;
        }

        bool isDisplayingEffect(ActiveEffectItemInfo effectItemInfo)
        {
            foreach (ChaosActiveEffectItemController effectItemDisplayController in _activeEffectItems)
            {
                if (effectItemDisplayController.DisplayingEffect == effectItemInfo)
                {
                    return true;
                }
            }

            return false;
        }

        void onActiveEffectsChanged(ActiveEffectItemInfo[] activeEffects)
        {
            for (int i = _activeEffectItems.Count - 1; i >= 0; i--)
            {
                if (!_activeEffectItems[i])
                {
                    _activeEffectItems.RemoveAt(i);
                }
                else if (Array.IndexOf(activeEffects, _activeEffectItems[i].DisplayingEffect) < 0)
                {
                    Destroy(_activeEffectItems[i].gameObject);
                    _activeEffectItems.RemoveAt(i);
                }
            }

            for (int i = 0; i < activeEffects.Length; i++)
            {
                if (!isDisplayingEffect(activeEffects[i]))
                {
                    _activeEffectItems.Add(ChaosActiveEffectItemController.CreateEffectDisplayItem(_activeEffectsContainer, activeEffects[i]));
                }
            }

            // Hancky way of "hiding" the panel if there aren't any effects.
            _activeEffectsTitleLabel.gameObject.SetActive(activeEffects.Length > 0);
        }
    }
}
