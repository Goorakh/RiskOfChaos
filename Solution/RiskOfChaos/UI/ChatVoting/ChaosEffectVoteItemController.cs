using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfChaos.UI.ChatVoting
{
    public sealed class ChaosEffectVoteItemController : MonoBehaviour
    {
        public CanvasGroup CanvasGroup;
        public Image BackdropImage;

        public LanguageTextMeshController EffectTextController;
        public TMP_Text EffectTextLabel;

        [NonSerialized]
        public ChaosEffectVoteDisplayController OwnerVoteDisplayController;

        float _currentVoteFraction;
        float _voteFractionSmoothVelocity;

        float _maxFontSize;

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

                markVoteDisplayDirty();
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

            _maxFontSize = EffectTextLabel.fontSize;
        }

        void OnEnable()
        {
            _currentVoteFraction = 1f;
            _voteFractionSmoothVelocity = 0f;
            updateScaling();

            Configs.ChatVotingUI.VoteDisplayBackgroundColor.SettingChanged += onVoteDisplayBackgroundColorChanged;
            updateBackdropColor();

            Configs.ChatVotingUI.VoteDisplayTextColor.SettingChanged += onVoteDisplayTextColorChanged;
            updateTextColor();

            Language.onCurrentLanguageChanged += markVoteDisplayDirty;

            refreshTextDisplay();
        }

        void OnDisable()
        {
            Configs.ChatVotingUI.VoteDisplayBackgroundColor.SettingChanged -= onVoteDisplayBackgroundColorChanged;
            Configs.ChatVotingUI.VoteDisplayTextColor.SettingChanged -= onVoteDisplayTextColorChanged;

            Language.onCurrentLanguageChanged -= markVoteDisplayDirty;
        }

        void OnDestroy()
        {
            VoteOption = null;
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
                BackdropImage.color = Configs.ChatVotingUI.VoteDisplayBackgroundColor.Value;
            }
        }

        void updateTextColor()
        {
            if (EffectTextLabel)
            {
                Color fullVotesColor = Configs.ChatVotingUI.VoteDisplayTextColor.Value;

                Color currentColor = fullVotesColor;
                if (Configs.ChatVotingUI.VoteDisplayScalingModeConfig.Value != Configs.ChatVotingUI.VoteDisplayScalingMode.Disabled)
                {
                    Color.RGBToHSV(currentColor, out float h, out float s, out float v);
                    Color noVotesColor = Color.HSVToRGB(h, s * 0.8f, v * 0.9f);

                    currentColor = Color.Lerp(noVotesColor, currentColor, _currentVoteFraction);
                }

                EffectTextLabel.color = currentColor;
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

        void Update()
        {
            if (_voteOption != null)
            {
                float targetVoteFraction = _voteOption.VotePercentage;

                if (OwnerVoteDisplayController)
                {
                    ChaosEffectActivationSignaler_ChatVote chatVoteActivationSignaler = OwnerVoteDisplayController.ChatVoteActivationSignaler;
                    if (chatVoteActivationSignaler && chatVoteActivationSignaler.TotalVotes == 0)
                    {
                        targetVoteFraction = 0.5f;
                    }
                }

                float currentVoteFraction = _currentVoteFraction;
                switch (Configs.ChatVotingUI.VoteDisplayScalingModeConfig.Value)
                {
                    case Configs.ChatVotingUI.VoteDisplayScalingMode.Disabled:
                        currentVoteFraction = 0.5f;
                        break;
                    case Configs.ChatVotingUI.VoteDisplayScalingMode.Smooth:
                        currentVoteFraction = Mathf.SmoothDamp(currentVoteFraction, targetVoteFraction, ref _voteFractionSmoothVelocity, 0.3f, float.PositiveInfinity, Time.unscaledDeltaTime);
                        break;
                    case Configs.ChatVotingUI.VoteDisplayScalingMode.Immediate:
                        currentVoteFraction = targetVoteFraction;
                        break;
                    default:
                        throw new NotImplementedException($"Scaling mode {Configs.ChatVotingUI.VoteDisplayScalingModeConfig.Value} is not implemented");
                }

                _currentVoteFraction = currentVoteFraction;

                updateScaling();
            }
        }

        void refreshTextDisplay()
        {
            string token = string.Empty;
            object[] formatArgs = [];

            if (_voteOption != null)
            {
                string optionName = "???";
                if (_voteOption.IsRandom)
                {
                    optionName = Language.GetString("CHAOS_EFFECT_VOTING_RANDOM_OPTION_NAME");
                }
                else if (_voteOption.EffectInfo != null)
                {
                    optionName = _voteOption.EffectInfo.GetStaticDisplayName();
                }

                token = "CHAOS_EFFECT_VOTING_OPTION_FORMAT";
                formatArgs = [
                    _voteOption.VoteNumber,
                    optionName,
                    _voteOption.VotePercentage
                ];
            }

            EffectTextController.SetTokenAndFormatArgs(token, formatArgs);
        }

        void updateScaling()
        {
            if (EffectTextLabel)
            {
                EffectTextLabel.fontSize = _maxFontSize * Mathf.Lerp(0.85f, 1f, _currentVoteFraction);
            }

            updateTextColor();
        }
    }
}
