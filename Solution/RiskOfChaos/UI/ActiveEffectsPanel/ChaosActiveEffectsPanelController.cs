using HG;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public class ChaosActiveEffectsPanelController : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            UISkinData uiSkin = UISkins.ActiveEffectsPanel;

            GameObject activeEffectsPanelPrefab = Prefabs.CreatePrefab(nameof(RoCContent.LocalPrefabs.ActiveEffectsUIPanel), []);

            RectTransform activeEffectsPanelTransform = activeEffectsPanelPrefab.AddComponent<RectTransform>();

            CanvasRenderer panelCanvasRenderer = activeEffectsPanelPrefab.AddComponent<CanvasRenderer>();
            
            Image panelImage = activeEffectsPanelPrefab.AddComponent<Image>();
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;

            PanelSkinController panelSkinController = activeEffectsPanelPrefab.AddComponent<PanelSkinController>();
            panelSkinController.panelType = PanelSkinController.PanelType.Default;
            panelSkinController.skinData = uiSkin;

            VerticalLayoutGroup panelLayoutGroup = activeEffectsPanelPrefab.AddComponent<VerticalLayoutGroup>();
            panelLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            panelLayoutGroup.padding = new RectOffset(4, 4, 4, 8);

            Canvas panelCanvas = activeEffectsPanelPrefab.AddComponent<Canvas>();

            ChaosActiveEffectsPanelController activeEffectsDisplayController = activeEffectsPanelPrefab.AddComponent<ChaosActiveEffectsPanelController>();

            // ActiveEffectsHeader
            {
                GameObject headerRootObject = new GameObject("Header");
                RectTransform headerRootTransform = headerRootObject.AddComponent<RectTransform>();
                headerRootTransform.SetParent(activeEffectsPanelTransform, false);

                LayoutElement rootLayoutElement = headerRootObject.AddComponent<LayoutElement>();
                rootLayoutElement.minHeight = 15f;

                // ActiveEffectsLabel
                {
                    GameObject labelObject = new GameObject("Label");
                    RectTransform labelTransform = labelObject.AddComponent<RectTransform>();
                    labelTransform.SetParent(headerRootTransform, false);

                    HGTextMeshProUGUI label = labelObject.AddComponent<HGTextMeshProUGUI>();

                    LabelSkinController labelSkinController = labelObject.AddComponent<LabelSkinController>();
                    labelSkinController.labelType = LabelSkinController.LabelType.Header;
                    labelSkinController.skinData = uiSkin;

                    LanguageTextMeshController languageTextController = labelObject.AddComponent<LanguageTextMeshController>();
                    languageTextController.token = "CHAOS_ACTIVE_EFFECTS_BAR_TITLE";
                }

                activeEffectsDisplayController._activeEffectsHeaderRoot = headerRootTransform;
            }

            // ActiveEffectsContainer
            {
                GameObject containerObject = new GameObject("EffectsContainer");
                RectTransform containerTransform = containerObject.AddComponent<RectTransform>();
                containerTransform.SetParent(activeEffectsPanelTransform, false);

                VerticalLayoutGroup containerLayoutGroup = containerObject.AddComponent<VerticalLayoutGroup>();
                containerLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                containerLayoutGroup.childForceExpandHeight = false;
                containerLayoutGroup.padding = new RectOffset(0, 0, 10, 0);
                containerLayoutGroup.spacing = 7f;

                activeEffectsDisplayController._activeEffectsContainer = containerTransform;
            }

            activeEffectsPanelPrefab.layer = LayerIndex.ui.intVal;

            localPrefabs.Add(activeEffectsPanelPrefab);
        }

        public static ChaosActiveEffectsPanelController Create(ChaosUIController chaosUIController)
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

            ChaosActiveEffectsPanelController displayController = Instantiate(RoCContent.LocalPrefabs.ActiveEffectsUIPanel, rightInfoBar).GetComponent<ChaosActiveEffectsPanelController>();

            return displayController;
        }

        [SerializeField]
        RectTransform _activeEffectsContainer;

        [SerializeField]
        RectTransform _activeEffectsHeaderRoot;

        ChaosActiveEffectDisplayController[] _activeEffectDisplays = [];
        int _numDisplayingEffects;

        bool _activeEffectsDirty;

        void OnEnable()
        {
            ChaosEffectTracker.OnTimedEffectStartGlobal += onTimedEffectStartGlobal;
            ChaosEffectTracker.OnTimedEffectEndGlobal += onTimedEffectEndGlobal;

            markActiveEffectsDirty();
        }

        void OnDisable()
        {
            ChaosEffectTracker.OnTimedEffectStartGlobal -= onTimedEffectStartGlobal;
            ChaosEffectTracker.OnTimedEffectEndGlobal -= onTimedEffectEndGlobal;

            setDisplayedEffects([]);
        }

        void OnDestroy()
        {
            foreach (ChaosActiveEffectDisplayController activeEffectDisplay in _activeEffectDisplays)
            {
                if (activeEffectDisplay)
                {
                    Destroy(activeEffectDisplay.gameObject);
                }
            }
        }

        void onTimedEffectStartGlobal(ChaosEffectComponent effectComponent)
        {
            markActiveEffectsDirty();
        }

        void onTimedEffectEndGlobal(ChaosEffectComponent effectComponent)
        {
            markActiveEffectsDirty();
        }

        void FixedUpdate()
        {
            if (_activeEffectsDirty)
            {
                _activeEffectsDirty = false;
                updateActiveEffects();
            }
        }

        void markActiveEffectsDirty()
        {
            _activeEffectsDirty = true;
        }

        void updateActiveEffects()
        {
            setDisplayedEffects(ChaosEffectTracker.Instance.AllActiveTimedEffects);
        }

        void setDisplayedEffects(IReadOnlyCollection<ChaosEffectComponent> effects)
        {
            int displayingEffectCount = 0;

            ArrayUtils.EnsureCapacity(ref _activeEffectDisplays, effects.Count);
            foreach (ChaosEffectComponent effectComponent in effects)
            {
                if (effectComponent.ShouldDisplayOnHUD)
                {
                    ref ChaosActiveEffectDisplayController activeEffectDisplay = ref _activeEffectDisplays[displayingEffectCount];
                    if (!activeEffectDisplay)
                    {
                        activeEffectDisplay = Instantiate(RoCContent.LocalPrefabs.ActiveEffectListUIItem, _activeEffectsContainer).GetComponent<ChaosActiveEffectDisplayController>();
                    }

                    activeEffectDisplay.gameObject.SetActive(true);
                    activeEffectDisplay.DisplayingEffect = effectComponent;

                    displayingEffectCount++;
                }
            }

            if (displayingEffectCount < _numDisplayingEffects)
            {
                for (int i = displayingEffectCount; i < _numDisplayingEffects; i++)
                {
                    ChaosActiveEffectDisplayController activeEffectDisplay = _activeEffectDisplays[i];
                    if (activeEffectDisplay)
                    {
                        activeEffectDisplay.DisplayingEffect = null;
                        activeEffectDisplay.gameObject.SetActive(false);
                    }
                }
            }

            _numDisplayingEffects = displayingEffectCount;

            if (_activeEffectsHeaderRoot)
            {
                _activeEffectsHeaderRoot.gameObject.SetActive(_numDisplayingEffects > 0);
            }
        }
    }
}
