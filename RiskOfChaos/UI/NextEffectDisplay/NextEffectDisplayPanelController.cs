using R2API;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Networking.Components.Effects;
using RiskOfChaos.Trackers;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public class NextEffectDisplayPanelController : MonoBehaviour
    {
        static GameObject _effectDisplayPrefab;

        [SystemInitializer]
        static void Init()
        {
            GameObject effectDisplayPrefab = Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/NotificationPanel2.prefab").WaitForCompletion());

            // Prevent added components from running while we're setting up the prefab
            effectDisplayPrefab.SetActive(false);

            Transform effectDisplayCanvasGroupTransform = effectDisplayPrefab.transform.Find("CanvasGroup");
            if (!effectDisplayCanvasGroupTransform)
            {
                Log.Error("Unable to find child CanvasGroup");
                return;
            }

            DestroyImmediate(effectDisplayPrefab.GetComponent<GenericNotification>());

            GameObject effectDisplayCanvasGroup = effectDisplayCanvasGroupTransform.gameObject;

            DestroyImmediate(effectDisplayCanvasGroup.GetComponent<HorizontalLayoutGroup>());
            DestroyImmediate(effectDisplayCanvasGroup.GetComponent<ContentSizeFitter>());

            Transform iconArea = effectDisplayCanvasGroup.transform.Find("IconArea");
            if (iconArea)
            {
                DestroyImmediate(iconArea.gameObject);
            }

            Transform textArea = effectDisplayCanvasGroup.transform.Find("TextArea");
            if (textArea)
            {
                DestroyImmediate(textArea.gameObject);
            }

            GameObject effectText = new GameObject("EffectText");
            RectTransform effectTextTransform = effectText.AddComponent<RectTransform>();
            effectTextTransform.anchorMin = Vector2.zero;
            effectTextTransform.anchorMax = Vector2.one;
            effectTextTransform.sizeDelta = Vector2.zero;
            effectTextTransform.anchoredPosition = Vector2.zero;

            HGTextMeshProUGUI effectTextLabel = effectText.AddComponent<HGTextMeshProUGUI>();
            effectTextLabel.alignment = TextAlignmentOptions.Center;
            effectTextLabel.enableWordWrapping = false;
            effectTextLabel.fontSize = 30;
            effectTextLabel.text = string.Empty;

            effectText.transform.SetParent(effectDisplayCanvasGroup.transform);

            NextEffectDisplayController displayController = effectDisplayPrefab.AddComponent<NextEffectDisplayController>();
            displayController.EffectText = effectText.AddComponent<LanguageTextMeshController>();

            Transform backdropTransform = effectDisplayCanvasGroup.transform.Find("Backdrop");
            if (backdropTransform)
            {
                displayController.BackdropImage = backdropTransform.GetComponent<Image>();
            }

            Transform flashTransform = effectDisplayCanvasGroup.transform.Find("Flash");
            if (flashTransform)
            {
                displayController.FlashController = flashTransform.GetComponent<AnimateUIAlpha>();
            }

            _effectDisplayPrefab = effectDisplayPrefab.InstantiateClone("ChaosNextEffectDisplay", false);
            Destroy(effectDisplayPrefab);

            // Make sure it's actually active when instantiating :|
            _effectDisplayPrefab.SetActive(true);
        }

        internal static NextEffectDisplayPanelController Create(ChaosUIController chaosUIController)
        {
            Transform leftCluster = chaosUIController.transform.Find("MainContainer/MainUIArea/SpringCanvas/BottomCenterCluster");
            if (!leftCluster)
            {
                Log.Error("Unable to find SpringCanvas BottomCenterCluster");
                return null;
            }

            GameObject nextEffectDisplay = new GameObject("NextEffectDisplay");

            RectTransform rectTransform = nextEffectDisplay.AddComponent<RectTransform>();
            rectTransform.SetParent(leftCluster, false);
            rectTransform.localPosition = new Vector2(0f, NO_NOTIFICATION_Y_POSITION);
            rectTransform.localScale = Vector3.one * 0.85f;

            NextEffectDisplayPanelController panelController = nextEffectDisplay.AddComponent<NextEffectDisplayPanelController>();
            panelController._ownerHud = chaosUIController.HUD;
            return panelController;
        }

        const float NO_NOTIFICATION_Y_POSITION = 100f;
        const float NOTIFICATION_Y_POSITION = 225f;

        HUD _ownerHud;

        NextEffectDisplayController _currentDisplay;

        Vector3 _velocity;

        EffectDisplayData? getEffectDisplayData()
        {
            if (Configs.General.DisableEffectDispatching.Value)
                return null;

            NextEffectProvider nextEffectProvider = NextEffectProvider.Instance;
            if (nextEffectProvider && nextEffectProvider.HasValidNextEffectState)
            {
                ChaosEffectIndex nextEffect = nextEffectProvider.NextEffectIndex;
                if (Configs.UI.DisplayNextEffect.Value && nextEffect != ChaosEffectIndex.Invalid)
                {
                    return new EffectDisplayData(nextEffect, nextEffectProvider.NextEffectActivationTime.timeUntilClamped, nextEffectProvider.NextEffectNameFormatter);
                }
                else if (Configs.UI.ShouldShowNextEffectTimer(_ownerHud))
                {
                    return new EffectDisplayData(ChaosEffectIndex.Invalid, nextEffectProvider.NextEffectActivationTime.timeUntilClamped, EffectNameFormatter_None.Instance);
                }
            }

            return null;
        }

        bool isNotificationShowing()
        {
            NotificationUIController notificationController = NotificationUIControllerTracker.GetNotificationUIControllerForHUD(_ownerHud);
            if (!notificationController)
                return false;

            return notificationController.currentNotification;
        }

        void FixedUpdate()
        {
            SetEffectDisplay(getEffectDisplayData());

            Vector3 targetPosition = transform.localPosition;
            targetPosition.y = isNotificationShowing() ? NOTIFICATION_Y_POSITION : NO_NOTIFICATION_Y_POSITION;

            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPosition, ref _velocity, 0.1f, float.PositiveInfinity, Time.fixedUnscaledDeltaTime);
        }

        public void SetEffectDisplay(EffectDisplayData? displayData)
        {
            if (displayData.HasValue)
            {
                if (!_currentDisplay)
                {
                    _currentDisplay = Instantiate(_effectDisplayPrefab, transform).GetComponent<NextEffectDisplayController>();
                }
                else if (!_currentDisplay.gameObject.activeSelf)
                {
                    _currentDisplay.gameObject.SetActive(true);
                }

                _currentDisplay.DisplayEffect(displayData.Value);
            }
            else
            {
                if (_currentDisplay && _currentDisplay.gameObject.activeSelf)
                {
                    _currentDisplay.gameObject.SetActive(false);
                }
            }
        }
    }
}
