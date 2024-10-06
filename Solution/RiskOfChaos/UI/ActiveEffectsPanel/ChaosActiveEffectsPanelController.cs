using HG;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public class ChaosActiveEffectsPanelController : MonoBehaviour
    {
        static GameObject _activeEffectsPanelPrefab;

        [SystemInitializer(typeof(UISkins))]
        static void Init()
        {
            UISkinData uiSkin = UISkins.ActiveEffectsPanel;

            GameObject activeEffectsPanelObject = NetPrefabs.CreateEmptyPrefabObject("ActiveEffectsPanel", false);

            RectTransform activeEffectsPanelTransform = activeEffectsPanelObject.AddComponent<RectTransform>();

            CanvasRenderer panelCanvasRenderer = activeEffectsPanelObject.AddComponent<CanvasRenderer>();
            
            Image panelImage = activeEffectsPanelObject.AddComponent<Image>();
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;

            PanelSkinController panelSkinController = activeEffectsPanelObject.AddComponent<PanelSkinController>();
            panelSkinController.panelType = PanelSkinController.PanelType.Default;
            panelSkinController.skinData = uiSkin;

            VerticalLayoutGroup panelLayoutGroup = activeEffectsPanelObject.AddComponent<VerticalLayoutGroup>();
            panelLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            panelLayoutGroup.padding = new RectOffset(4, 4, 4, 8);

            Canvas panelCanvas = activeEffectsPanelObject.AddComponent<Canvas>();

            ChaosActiveEffectsPanelController activeEffectsDisplayController = activeEffectsPanelObject.AddComponent<ChaosActiveEffectsPanelController>();

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

            activeEffectsPanelObject.layer = LayerIndex.ui.intVal;

            _activeEffectsPanelPrefab = activeEffectsPanelObject;
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

            ChaosActiveEffectsPanelController displayController = Instantiate(_activeEffectsPanelPrefab, rightInfoBar).GetComponent<ChaosActiveEffectsPanelController>();

            return displayController;
        }

        [SerializeField]
        RectTransform _activeEffectsContainer;

        [SerializeField]
        RectTransform _activeEffectsHeaderRoot;

        ChaosActiveEffectDisplayController[] _activeEffectDisplays = [];
        int _numDisplayingEffects;

        float _lastDisplayRefreshTime;

        bool _activeEffectsDirty;

        void OnEnable()
        {
            updateActiveEffects();

            ChaosEffectTracker.OnTimedEffectStartGlobal += onTimedEffectStartGlobal;
            ChaosEffectTracker.OnTimedEffectEndGlobal += onTimedEffectEndGlobal;
        }

        void OnDisable()
        {
            ChaosEffectTracker.OnTimedEffectStartGlobal -= onTimedEffectStartGlobal;
            ChaosEffectTracker.OnTimedEffectEndGlobal -= onTimedEffectEndGlobal;
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
            if (_activeEffectsDirty/* || Time.unscaledTime - _lastDisplayRefreshTime > 15f*/)
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
            _lastDisplayRefreshTime = Time.unscaledTime;

            ReadOnlyCollection<ChaosEffectComponent> allActiveEffects = ChaosEffectTracker.Instance.AllActiveTimedEffects;

            int displayingEffectCount = 0;

            ArrayUtils.EnsureCapacity(ref _activeEffectDisplays, allActiveEffects.Count);
            foreach (ChaosEffectComponent effectComponent in allActiveEffects)
            {
                if (effectComponent.ShouldDisplayOnHUD)
                {
                    ref ChaosActiveEffectDisplayController activeEffectDisplay = ref _activeEffectDisplays[displayingEffectCount];
                    if (!activeEffectDisplay)
                    {
                        activeEffectDisplay = Instantiate(ChaosActiveEffectDisplayController.ItemPrefab, _activeEffectsContainer).GetComponent<ChaosActiveEffectDisplayController>();
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
