using RiskOfChaos.ConfigHandling;
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

        bool _voteDisplayDirty;

        EffectVoteInfo _voteOption;
        public EffectVoteInfo VoteOption
        {
            get
            {
                return _voteOption;
            }
            set
            {
                if (_voteOption == value)
                    return;

                unsubscribe(_voteOption);

                _voteOption = value;

                subscribe(_voteOption);

                refreshTextDisplay();
            }
        }

        void subscribe(EffectVoteInfo voteInfo)
        {
            if (voteInfo != null)
            {
                if (voteInfo.EffectInfo != null)
                {
                    voteInfo.EffectInfo.StaticDisplayNameFormatterProvider.OnNameFormatterChanged += markVoteDisplayDirty;
                }

                voteInfo.OnVotesChanged += markVoteDisplayDirty;
            }
        }

        void unsubscribe(EffectVoteInfo voteInfo)
        {
            if (voteInfo != null)
            {
                if (voteInfo.EffectInfo != null)
                {
                    voteInfo.EffectInfo.StaticDisplayNameFormatterProvider.OnNameFormatterChanged -= markVoteDisplayDirty;
                }

                voteInfo.OnVotesChanged -= markVoteDisplayDirty;
            }
        }

        void Awake()
        {
            RectTransform rectTransform = (RectTransform)EffectTextController.transform;

            // These don't want to serialize for some reason
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        void OnEnable()
        {
            Configs.ChatVoting.VoteDisplayBackgroundColor.SettingChanged += onVoteDisplayBackgroundColorChanged;
            updateBackdropColor();

            Configs.ChatVoting.VoteDisplayTextColor.SettingChanged += onVoteDisplayTextColorChanged;
            updateTextColor();

            Language.onCurrentLanguageChanged += markVoteDisplayDirty;

            refreshTextDisplay();
        }

        void OnDisable()
        {
            Configs.ChatVoting.VoteDisplayBackgroundColor.SettingChanged -= onVoteDisplayBackgroundColorChanged;
            Configs.ChatVoting.VoteDisplayTextColor.SettingChanged -= onVoteDisplayTextColorChanged;

            Language.onCurrentLanguageChanged -= markVoteDisplayDirty;
        }

        void OnDestroy()
        {
            unsubscribe(_voteOption);
        }

        void onVoteDisplayBackgroundColorChanged(object sender, ConfigChangedArgs<Color> e)
        {
            updateBackdropColor();
        }

        void onVoteDisplayTextColorChanged(object sender, ConfigChangedArgs<Color> e)
        {
            updateTextColor();
        }

        void updateBackdropColor()
        {
            if (BackdropImage)
            {
                BackdropImage.color = Configs.ChatVoting.VoteDisplayBackgroundColor.Value;
            }
        }

        void updateTextColor()
        {
            if (EffectTextLabel)
            {
                EffectTextLabel.color = Configs.ChatVoting.VoteDisplayTextColor.Value;
            }
        }

        public void SetAlpha(float alpha)
        {
            CanvasGroup.alpha = alpha;
        }

        void markVoteDisplayDirty()
        {
            _voteDisplayDirty = true;
        }

        void FixedUpdate()
        {
            if (_voteDisplayDirty)
            {
                _voteDisplayDirty = false;
                refreshTextDisplay();
            }
        }

        void refreshTextDisplay()
        {
            string token = string.Empty;
            object[] formatArgs = [];

            if (_voteOption != null)
            {
                token = "CHAOS_EFFECT_VOTING_OPTION_FORMAT";
                formatArgs = [
                    _voteOption.VoteNumber,
                    _voteOption.IsRandom ? Language.GetString("CHAOS_EFFECT_VOTING_RANDOM_OPTION_NAME") : _voteOption.EffectInfo.GetStaticDisplayName(),
                    _voteOption.VotePercentage * 100f
                ];
            }

            EffectTextController.SetTokenAndFormatArgs(token, formatArgs);
        }
    }
}
