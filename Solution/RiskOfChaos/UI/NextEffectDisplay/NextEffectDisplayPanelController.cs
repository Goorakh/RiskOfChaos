﻿using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2.ContentManagement;
using RoR2.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace RiskOfChaos.UI.NextEffectDisplay
{
    public class NextEffectDisplayPanelController : MonoBehaviour
    {
        [ContentInitializer]
        static IEnumerator LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(1);

            AsyncOperationHandle<GameObject> notificationPanelLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_UI_NotificationPanel2_prefab, AsyncReferenceHandleUnloadType.Preload);
            notificationPanelLoad.OnSuccess(notificationPanelPrefab =>
            {
                // TODO: Construct panel prefab instead of Frankensteining existing prefabs
                GameObject prefab = notificationPanelPrefab.InstantiatePrefab(nameof(RoCContent.LocalPrefabs.ChaosNextEffectDisplay));

                Transform effectDisplayCanvasGroupTransform = prefab.transform.Find("CanvasGroup");
                if (!effectDisplayCanvasGroupTransform)
                {
                    Log.Error("Unable to find child CanvasGroup");
                    return;
                }

                Destroy(prefab.GetComponent<GenericNotification>());

                GameObject effectDisplayCanvasGroup = effectDisplayCanvasGroupTransform.gameObject;

                Destroy(effectDisplayCanvasGroup.GetComponent<HorizontalLayoutGroup>());
                Destroy(effectDisplayCanvasGroup.GetComponent<ContentSizeFitter>());

                Transform iconArea = effectDisplayCanvasGroup.transform.Find("IconArea");
                if (iconArea)
                {
                    Destroy(iconArea.gameObject);
                }

                Transform textArea = effectDisplayCanvasGroup.transform.Find("TextArea");
                if (textArea)
                {
                    Destroy(textArea.gameObject);
                }

                GameObject effectText = new GameObject("EffectText");
                effectText.transform.SetParent(effectDisplayCanvasGroup.transform);
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

                NextEffectDisplayController displayController = prefab.AddComponent<NextEffectDisplayController>();
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

                localPrefabs.Add(prefab);
            });

            asyncOperations.Add(notificationPanelLoad);

            yield return asyncOperations.WaitForAllLoaded();
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

            ChaosNextEffectProvider nextEffectProvider = ChaosNextEffectProvider.Instance;
            if (nextEffectProvider && !nextEffectProvider.NextEffectActivationTime.IsInfinity)
            {
                ChaosEffectIndex nextEffect = nextEffectProvider.NextEffectIndex;
                if (Configs.UI.DisplayNextEffect.Value && nextEffect != ChaosEffectIndex.Invalid)
                {
                    return new EffectDisplayData(nextEffect, nextEffectProvider.NextEffectActivationTime, nextEffectProvider.NextEffectNameFormatter);
                }
                else if (Configs.UI.ShouldShowNextEffectTimer(_ownerHud))
                {
                    return new EffectDisplayData(ChaosEffectIndex.Invalid, nextEffectProvider.NextEffectActivationTime, EffectNameFormatter_None.Instance);
                }
            }

            return null;
        }

        bool isNotificationShowing()
        {
            NotificationUIController notificationController = NotificationUIControllerTracker.GetNotificationUIControllerForHUD(_ownerHud);
            return notificationController && notificationController.currentNotification;
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
                    _currentDisplay = Instantiate(RoCContent.LocalPrefabs.ChaosNextEffectDisplay, transform).GetComponent<NextEffectDisplayController>();
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
