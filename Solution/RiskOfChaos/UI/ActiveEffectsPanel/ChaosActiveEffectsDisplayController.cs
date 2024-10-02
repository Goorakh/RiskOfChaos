using RiskOfChaos.Networking.Components.Effects;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public class ChaosActiveEffectsDisplayController : MonoBehaviour
    {
        static GameObject _activeEffectsPanelPrefab;

        [SystemInitializer(typeof(UISkins))]
        static void Init()
        {
            UISkinData uiSkin = UISkins.ActiveEffectsPanel;

            GameObject activeEffectsPanelPrefab = NetPrefabs.CreateEmptyPrefabObject("ActiveEffectsPanel", false);

            RectTransform activeEffectsTransform = activeEffectsPanelPrefab.AddComponent<RectTransform>();

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

            ChaosActiveEffectsDisplayController activeEffectsDisplayController = activeEffectsPanelPrefab.AddComponent<ChaosActiveEffectsDisplayController>();

            // ActiveEffectsHeader
            {
                GameObject headerObject = new GameObject("ActiveEffectsLabel");
                RectTransform headerTransform = headerObject.AddComponent<RectTransform>();
                headerTransform.SetParent(activeEffectsTransform, false);

                HGTextMeshProUGUI headerLabel = headerObject.AddComponent<HGTextMeshProUGUI>();

                LabelSkinController headerLabelSkinController = headerObject.AddComponent<LabelSkinController>();
                headerLabelSkinController.labelType = LabelSkinController.LabelType.Header;
                headerLabelSkinController.skinData = uiSkin;

                LanguageTextMeshController headerLanguageController = headerObject.AddComponent<LanguageTextMeshController>();
                headerLanguageController.token = "CHAOS_ACTIVE_EFFECTS_BAR_TITLE";

                LayoutElement layoutElement = headerObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = 15f;

                activeEffectsDisplayController._activeEffectsTitleLabel = headerLabel;
            }

            // ActiveEffectsContainer
            {
                GameObject containerObject = new GameObject("EffectsContainer");
                RectTransform containerTransform = containerObject.AddComponent<RectTransform>();
                containerTransform.SetParent(activeEffectsTransform, false);

                VerticalLayoutGroup containerLayoutGroup = containerObject.AddComponent<VerticalLayoutGroup>();
                containerLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                containerLayoutGroup.childForceExpandHeight = false;
                containerLayoutGroup.padding = new RectOffset(0, 0, 10, 0);
                containerLayoutGroup.spacing = 7f;

                activeEffectsDisplayController._activeEffectsContainer = containerTransform;
            }

            activeEffectsPanelPrefab.layer = LayerIndex.ui.intVal;

            _activeEffectsPanelPrefab = activeEffectsPanelPrefab;
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

        readonly List<ChaosActiveEffectItemController> _activeEffectItems = [];

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

            // Update already active effects to refresh the data in case something has changed
            for (int i = 0; i < _activeEffectItems.Count; i++)
            {
                ulong dispatchID = _activeEffectItems[i].DisplayingEffect.DispatchID;
                int activeEffectsIndex = Array.FindIndex(activeEffects, a => a.DispatchID == dispatchID);
                if (activeEffectsIndex != -1)
                {
                    _activeEffectItems[i].DisplayingEffect = activeEffects[activeEffectsIndex];
                }
            }

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
