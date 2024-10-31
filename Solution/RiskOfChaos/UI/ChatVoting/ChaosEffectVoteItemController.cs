using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ChatVoting
{
    public class ChaosEffectVoteItemController : MonoBehaviour
    {
        public CanvasGroup CanvasGroup;
        public Image BackdropImage;

        public LanguageTextMeshController EffectTextController;
        public TMP_Text EffectTextLabel;

        EffectVoteInfo _voteOption;
        uint _displayedVersion;

        void Awake()
        {
            RectTransform rectTransform = (RectTransform)EffectTextController.transform;

            // These don't want to serialize for some reason
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        void OnEnable()
        {
            refreshTextDisplay();

            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;

            ChaosEffectInfo.OnEffectNameFormatterDirty += ChaosEffectInfo_OnEffectNameFormatterDirty;
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;

            ChaosEffectInfo.OnEffectNameFormatterDirty -= ChaosEffectInfo_OnEffectNameFormatterDirty;
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
            if (EffectTextLabel)
            {
                EffectTextLabel.color = color;
            }
        }

        public void SetAlpha(float alpha)
        {
            CanvasGroup.alpha = alpha;
        }

        public void SetVote(EffectVoteInfo voteOption)
        {
            _voteOption = voteOption;
            refreshTextDisplay();
        }

        void onCurrentLanguageChanged()
        {
            refreshTextDisplay();
        }

        void ChaosEffectInfo_OnEffectNameFormatterDirty(ChaosEffectInfo effectInfo)
        {
            if (_voteOption != null && !_voteOption.IsRandom && _voteOption.EffectInfo == effectInfo)
            {
                refreshTextDisplay();
            }
        }

        void refreshTextDisplay()
        {
            if (_voteOption == null)
            {
                EffectTextController.SetTokenAndFormatArgs(string.Empty, []);

                _displayedVersion = 0;
            }
            else
            {
                EffectTextController.SetTokenAndFormatArgs("CHAOS_EFFECT_VOTING_OPTION_FORMAT", _voteOption.GetArgs());

                _displayedVersion = _voteOption.Version;
            }
        }

        void Update()
        {
            if (_voteOption != null && _voteOption.Version > _displayedVersion)
            {
                refreshTextDisplay();
            }
        }
    }
}
