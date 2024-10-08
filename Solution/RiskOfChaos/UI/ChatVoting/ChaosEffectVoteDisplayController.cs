using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.Utilities.Extensions;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ChatVoting
{
    public class ChaosEffectVoteDisplayController : MonoBehaviour
    {
        [ContentInitializer]
        static IEnumerator LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = [];

            AsyncOperationHandle<GameObject> notificationPanelLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/NotificationPanel2.prefab");
            notificationPanelLoad.Completed += handle =>
            {
                // TODO: Construct panel prefab instead of Frankensteining existing prefabs
                GameObject prefab = handle.Result.InstantiatePrefab(nameof(RoCContent.LocalPrefabs.ChaosEffectUIVoteItem));

                Transform voteItemCanvasGroupTransform = prefab.transform.Find("CanvasGroup");
                if (!voteItemCanvasGroupTransform)
                {
                    Log.Error("Unable to find child CanvasGroup");
                    return;
                }

                Destroy(prefab.GetComponent<GenericNotification>());

                GameObject voteItemCanvasGroup = voteItemCanvasGroupTransform.gameObject;

                Destroy(voteItemCanvasGroup.GetComponent<HorizontalLayoutGroup>());
                Destroy(voteItemCanvasGroup.GetComponent<ContentSizeFitter>());

                Transform iconArea = voteItemCanvasGroup.transform.Find("IconArea");
                if (iconArea)
                {
                    Destroy(iconArea.gameObject);
                }

                Transform textArea = voteItemCanvasGroup.transform.Find("TextArea");
                if (textArea)
                {
                    Destroy(textArea.gameObject);
                }

                GameObject effectText = new GameObject("EffectText");
                RectTransform effectTextTransform = effectText.AddComponent<RectTransform>();
                effectTextTransform.anchorMin = Vector2.zero;
                effectTextTransform.anchorMax = Vector2.one;
                effectTextTransform.sizeDelta = Vector2.zero;
                effectTextTransform.anchoredPosition = Vector2.zero;

                HGTextMeshProUGUI effectTextLabel = effectText.AddComponent<HGTextMeshProUGUI>();
                effectTextLabel.alignment = TextAlignmentOptions.Left;
                effectTextLabel.enableWordWrapping = false;
                effectTextLabel.fontSize = 30;
                effectTextLabel.text = string.Empty;

                effectText.transform.SetParent(voteItemCanvasGroup.transform);

                LayoutElement layoutElement = voteItemCanvasGroup.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 50f;
                layoutElement.preferredWidth = 500f;

                ChaosEffectVoteItemController chaosEffectVoteItem = prefab.AddComponent<ChaosEffectVoteItemController>();
                chaosEffectVoteItem.EffectTextLabel = effectTextLabel;
                chaosEffectVoteItem.EffectTextController = effectText.AddComponent<LanguageTextMeshController>();
                chaosEffectVoteItem.CanvasGroup = voteItemCanvasGroup.GetComponent<CanvasGroup>();

                Transform backdropTransform = voteItemCanvasGroup.transform.Find("Backdrop");
                if (backdropTransform)
                {
                    chaosEffectVoteItem.BackdropImage = backdropTransform.GetComponent<Image>();
                }

                localPrefabs.Add(prefab);
            };

            yield return asyncOperations.WaitForAllLoaded();
        }

        internal static ChaosEffectVoteDisplayController Create(ChaosUIController chaosUIController)
        {
            Transform leftCluster = chaosUIController.transform.Find("MainContainer/MainUIArea/SpringCanvas/LeftCluster");
            if (!leftCluster)
            {
                Log.Error("Unable to find SpringCanvas LeftCluster");
                return null;
            }

            GameObject chaosEffectVoteDisplay = new GameObject("ChaosEffectVoteDisplay");

            RectTransform rectTransform = chaosEffectVoteDisplay.AddComponent<RectTransform>();
            rectTransform.SetParent(leftCluster, false);
            rectTransform.anchoredPosition = new Vector2(100f, 50f);
            rectTransform.localScale = Vector3.one * 0.7f;

            VerticalLayoutGroup layoutGroup = chaosEffectVoteDisplay.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;

            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;

            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;

            layoutGroup.spacing = 55f;

            layoutGroup.CalculateLayoutInputHorizontal();
            layoutGroup.CalculateLayoutInputVertical();

            return chaosEffectVoteDisplay.AddComponent<ChaosEffectVoteDisplayController>();
        }

        public static event Action<ChaosEffectVoteDisplayController> OnDisplayControllerCreated;

        ChaosEffectVoteItemController[] _effectVoteItemControllers = [];

        Vector3 _defaultScale;

        void Awake()
        {
            _defaultScale = transform.localScale;
        }

        void OnEnable()
        {
            OnDisplayControllerCreated?.Invoke(this);

            Configs.ChatVoting.VoteDisplayScaleMultiplier.SettingChanged += refreshScale;
            setScale(Configs.ChatVoting.VoteDisplayScaleMultiplier.Value);
        }

        void OnDisable()
        {
            Configs.ChatVoting.VoteDisplayScaleMultiplier.SettingChanged -= refreshScale;
        }

        void refreshScale(object sender, ConfigChangedArgs<float> args)
        {
            setScale(args.NewValue);
        }

        void setScale(float scale)
        {
            transform.localScale = _defaultScale * scale;
        }

        public void RemoveAllVoteDisplays()
        {
            foreach (ChaosEffectVoteItemController voteItemController in _effectVoteItemControllers)
            {
                if (voteItemController)
                {
                    Destroy(voteItemController.gameObject);
                }
            }

            _effectVoteItemControllers = [];
        }

        public void SetVoteDisplayAlpha(float alpha)
        {
            foreach (ChaosEffectVoteItemController voteItemController in _effectVoteItemControllers)
            {
                if (voteItemController)
                {
                    voteItemController.SetAlpha(alpha);
                }
            }
        }

        public void DisplayVote(EffectVoteInfo[] voteOptions)
        {
            RemoveAllVoteDisplays();

            _effectVoteItemControllers = Array.ConvertAll(voteOptions, createVoteItemControllerForVote);
        }

        ChaosEffectVoteItemController createVoteItemControllerForVote(EffectVoteInfo voteOption)
        {
            ChaosEffectVoteItemController voteItemController = Instantiate(RoCContent.LocalPrefabs.ChaosEffectUIVoteItem, transform).GetComponent<ChaosEffectVoteItemController>();
            voteItemController.SetVote(voteOption);
            return voteItemController;
        }
    }
}
