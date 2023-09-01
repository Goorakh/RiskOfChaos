using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ChatVoting
{
    public class ChaosEffectVoteItemController : MonoBehaviour
    {
        public CanvasGroup CanvasGroup;
        public Image BackdropImage;
        public HGTextMeshProUGUI EffectText;

        EffectVoteInfo _voteOption;

        void Awake()
        {
            // These don't want to serialize for some reason
            EffectText.rectTransform.sizeDelta = Vector2.zero;
            EffectText.rectTransform.anchoredPosition = Vector2.zero;
        }

        void Start()
        {
            Configs.ChatVoting.VoteDisplayBackgroundColor.SettingChanged += VoteDisplayColor_SettingChanged;
            setBackdropColor(Configs.ChatVoting.VoteDisplayBackgroundColor.Value);

            Configs.ChatVoting.VoteDisplayTextColor.SettingChanged += VoteDisplayColor_SettingChanged;
            setTextColor(Configs.ChatVoting.VoteDisplayTextColor.Value);
        }

        void OnDestroy()
        {
            Configs.ChatVoting.VoteDisplayBackgroundColor.SettingChanged -= VoteDisplayColor_SettingChanged;
            Configs.ChatVoting.VoteDisplayTextColor.SettingChanged -= VoteDisplayColor_SettingChanged;
        }

        void VoteDisplayColor_SettingChanged(object sender, ConfigChangedArgs<Color> e)
        {
            if (e.Holder == Configs.ChatVoting.VoteDisplayBackgroundColor)
            {
                setBackdropColor(e.NewValue);
            }
            else if (e.Holder == Configs.ChatVoting.VoteDisplayTextColor)
            {
                setTextColor(e.NewValue);
            }
        }

        void setBackdropColor(Color color)
        {
            if (BackdropImage)
            {
                BackdropImage.color = color;
            }
        }

        void setTextColor(Color color)
        {
            if (EffectText)
            {
                EffectText.color = color;
            }
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
