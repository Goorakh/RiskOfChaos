using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.UI.ChatVoting
{
    public class ChaosEffectVoteItemController : MonoBehaviour
    {
        public CanvasGroup CanvasGroup;
        public HGTextMeshProUGUI EffectText;

        EffectVoteInfo _voteOption;

        void Awake()
        {
            // These don't want to serialize for some reason
            EffectText.rectTransform.sizeDelta = Vector2.zero;
            EffectText.rectTransform.anchoredPosition = Vector2.zero;
        }

        public void SetAlpha(float alpha)
        {
            CanvasGroup.alpha = alpha;
        }

        public void SetVote(EffectVoteInfo voteOption)
        {
            _voteOption = voteOption;
        }

        void Update()
        {
            if (_voteOption == null)
            {
                EffectText.text = string.Empty;
            }
            else
            {
                EffectText.text = _voteOption.ToString();
            }
        }
    }
}
