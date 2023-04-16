using R2API;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RoR2;
using RoR2.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ChatVoting
{
    public class ChaosEffectVoteDisplayController : MonoBehaviour
    {
        static GameObject _voteItemPrefab;

        [SystemInitializer]
        static void Init()
        {
            GameObject voteItemPrefab = Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/NotificationPanel2.prefab").WaitForCompletion());

            // Prevent added components from running while we're setting up the prefab
            voteItemPrefab.SetActive(false);

            Transform voteItemCanvasGroupTransform = voteItemPrefab.transform.Find("CanvasGroup");
            if (!voteItemCanvasGroupTransform)
            {
                Log.Error("Unable to find child CanvasGroup");
                return;
            }

            GameObject voteItemCanvasGroup = voteItemCanvasGroupTransform.gameObject;

            DestroyImmediate(voteItemCanvasGroup.GetComponent<HorizontalLayoutGroup>());
            DestroyImmediate(voteItemCanvasGroup.GetComponent<ContentSizeFitter>());

            Transform iconArea = voteItemCanvasGroup.transform.Find("IconArea");
            if (iconArea)
            {
                DestroyImmediate(iconArea.gameObject);
            }

            Transform textArea = voteItemCanvasGroup.transform.Find("TextArea");
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
            effectTextLabel.alignment = TextAlignmentOptions.Left;
            effectTextLabel.enableWordWrapping = false;
            effectTextLabel.fontSize = 30;
            effectTextLabel.text = "1: Effect Name blabalblablablablalbalblalblab (25%)";

            effectText.transform.SetParent(voteItemCanvasGroup.transform);

            LayoutElement layoutElement = voteItemCanvasGroup.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 50f;
            layoutElement.preferredWidth = 500f;

            ChaosEffectVoteItemController chaosEffectVoteItem = voteItemPrefab.AddComponent<ChaosEffectVoteItemController>();
            chaosEffectVoteItem.EffectText = effectTextLabel;
            chaosEffectVoteItem.CanvasGroup = voteItemCanvasGroup.GetComponent<CanvasGroup>();

            _voteItemPrefab = voteItemPrefab.InstantiateClone("ChaosEffectVoteItem", false);
            Destroy(voteItemPrefab);

            // Make sure it's actually active when instantiating :|
            _voteItemPrefab.SetActive(true);
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

        ChaosEffectVoteItemController[] _effectVoteItemControllers = Array.Empty<ChaosEffectVoteItemController>();

        Vector3 _defaultScale;

        void Awake()
        {
            _defaultScale = transform.localScale;
        }

        void OnEnable()
        {
            OnDisplayControllerCreated?.Invoke(this);

            Configs.ChatVoting.OnVoteDisplayScaleMultiplierChanged += refreshScale;
            refreshScale();
        }

        void OnDisable()
        {
            Configs.ChatVoting.OnVoteDisplayScaleMultiplierChanged -= refreshScale;
        }

        void refreshScale()
        {
            transform.localScale = _defaultScale * Configs.ChatVoting.VoteDisplayScaleMultiplier;
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

            _effectVoteItemControllers = Array.Empty<ChaosEffectVoteItemController>();
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
            ChaosEffectVoteItemController voteItemController = Instantiate(_voteItemPrefab, transform).GetComponent<ChaosEffectVoteItemController>();
            voteItemController.SetVote(voteOption);
            return voteItemController;
        }
    }
}
