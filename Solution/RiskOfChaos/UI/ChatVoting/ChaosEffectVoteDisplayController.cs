using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.Utilities.Extensions;
using RoR2.UI;
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
            notificationPanelLoad.OnSuccess(notificationPanel =>
            {
                // TODO: Construct panel prefab instead of Frankensteining existing prefabs
                GameObject prefab = notificationPanel.InstantiatePrefab(nameof(RoCContent.LocalPrefabs.ChaosEffectUIVoteItem));

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
                effectText.transform.SetParent(voteItemCanvasGroup.transform);
                RectTransform effectTextTransform = effectText.AddComponent<RectTransform>();
                effectTextTransform.anchorMin = Vector2.zero;
                effectTextTransform.anchorMax = Vector2.one;
                effectTextTransform.sizeDelta = Vector2.zero;
                effectTextTransform.anchoredPosition = Vector2.zero;

                HGTextMeshProUGUI effectTextLabel = effectText.AddComponent<HGTextMeshProUGUI>();
                effectTextLabel.alignment = TextAlignmentOptions.Left;
                effectTextLabel.enableWordWrapping = false;
                effectTextLabel.fontSize = 32.5f;
                effectTextLabel.text = string.Empty;

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
            });

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

        bool _voteDisplaysDirty;

        UIElementAllocator<ChaosEffectVoteItemController> _effectVoteDisplaysAllocator;

        Vector3 _defaultScale;

        ChaosEffectActivationSignaler_ChatVote _chatVoteActivationSignaler;
        public ChaosEffectActivationSignaler_ChatVote ChatVoteActivationSignaler
        {
            get
            {
                return _chatVoteActivationSignaler;
            }
            private set
            {
                if (_chatVoteActivationSignaler == value)
                    return;

                if (_chatVoteActivationSignaler)
                {
                    _chatVoteActivationSignaler.OnVoteOptionsChanged -= markVoteDisplaysDirty;
                }

                _chatVoteActivationSignaler = value;

                if (_chatVoteActivationSignaler)
                {
                    _chatVoteActivationSignaler.OnVoteOptionsChanged += markVoteDisplaysDirty;
                }

                markVoteDisplaysDirty();
            }
        }

        void Awake()
        {
            _defaultScale = transform.localScale;

            _effectVoteDisplaysAllocator = new UIElementAllocator<ChaosEffectVoteItemController>(GetComponent<RectTransform>(), RoCContent.LocalPrefabs.ChaosEffectUIVoteItem)
            {
                onCreateElement = onCreateVoteDisplay
            };
        }

        void OnEnable()
        {
            Configs.ChatVotingUI.VoteDisplayScaleMultiplier.SettingChanged += onScaleMultiplierConfigChanged;
            updateScale();
        }

        void OnDisable()
        {
            Configs.ChatVotingUI.VoteDisplayScaleMultiplier.SettingChanged -= onScaleMultiplierConfigChanged;

            ChatVoteActivationSignaler = null;
            setVoteDisplays([]);
        }

        void FixedUpdate()
        {
            if (!ChatVoteActivationSignaler || !ChatVoteActivationSignaler.enabled)
            {
                ChaosEffectActivationSignaler_ChatVote chatVoteActivationSignaler = null;
                foreach (ChaosEffectActivationSignaler effectActivationSignaler in ChaosEffectActivationSignaler.InstancesList)
                {
                    if (effectActivationSignaler is ChaosEffectActivationSignaler_ChatVote chatVoteSignaler)
                    {
                        chatVoteActivationSignaler = chatVoteSignaler;
                        break;
                    }
                }

                ChatVoteActivationSignaler = chatVoteActivationSignaler;
            }

            if (_voteDisplaysDirty)
            {
                _voteDisplaysDirty = false;
                updateVoteDisplays();
            }

            if (_effectVoteDisplaysAllocator.elements.Count > 0)
            {
                float voteDisplayAlpha = 1f;
                if (ChatVoteActivationSignaler && ChatVoteActivationSignaler.CanDispatchEffects)
                {
                    float timeRemaining = ChatVoteActivationSignaler.GetNextEffectActivationTime().TimeUntilClamped;

                    const float START_FADE_TIME = 2.5f;
                    voteDisplayAlpha = Mathf.Clamp01(timeRemaining / START_FADE_TIME);
                }

                foreach (ChaosEffectVoteItemController voteItemController in _effectVoteDisplaysAllocator.elements)
                {
                    if (voteItemController)
                    {
                        voteItemController.SetAlpha(voteDisplayAlpha);
                    }
                }
            }
        }

        void markVoteDisplaysDirty()
        {
            _voteDisplaysDirty = true;
        }

        void onScaleMultiplierConfigChanged(object sender, ConfigChangedArgs<float> args)
        {
            updateScale();
        }

        void updateScale()
        {
            transform.localScale = _defaultScale * Configs.ChatVotingUI.VoteDisplayScaleMultiplier.Value;
        }

        void updateVoteDisplays()
        {
            EffectVoteInfo[] voteOptions = [];
            if (ChatVoteActivationSignaler)
            {
                voteOptions = ChatVoteActivationSignaler.GetCurrentVoteOptions();
            }

            setVoteDisplays(voteOptions);
        }

        void setVoteDisplays(EffectVoteInfo[] voteOptions)
        {
            _effectVoteDisplaysAllocator.AllocateElements(voteOptions.Length);

            for (int i = 0; i < voteOptions.Length; i++)
            {
                ChaosEffectVoteItemController voteDisplayController = _effectVoteDisplaysAllocator.elements[i];
                voteDisplayController.VoteOption = voteOptions[i];
            }
        }

        void onCreateVoteDisplay(int elementIndex, ChaosEffectVoteItemController voteDisplayController)
        {
            voteDisplayController.OwnerVoteDisplayController = this;
        }
    }
}
