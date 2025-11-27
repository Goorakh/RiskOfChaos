using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("scramble_text", 120f, AllowDuplicates = false)]
    public sealed class ScrambleText : NetworkBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        [EffectConfig]
        static readonly ConfigHolder<bool> _excludeEffectNames =
            ConfigFactory<bool>.CreateConfig("Exclude Effect Names", false)
                               .Description("Excludes chaos effect names from being scrambled")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        // Match string formats ({0}, {0:F0}, etc.)
        static readonly Regex _stringFormatsRegex = new Regex(@"\{\d+(?::\w+)?\}", RegexOptions.Compiled);

        // Match rich text tags (<i>, <size=12>, </b>, etc.)
        static readonly Regex _richTextTagsRegex = new Regex(@"<[^<>]+>", RegexOptions.Compiled);

        static readonly StringBuilder _sharedResultBuilder = new StringBuilder();
        static readonly StringBuilder _sharedWordBuilder = new StringBuilder();

        readonly Dictionary<string, string> _tokenOverrideCache = [];

        readonly HashSet<TMP_Text> _processedTextLabels = [];

        ChaosEffectComponent _effectComponent;

        [SyncVar]
        [SerializedMember("s")]
        ulong _baseScrambleSeed;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _baseScrambleSeed = _effectComponent.Rng.nextUlong;
        }

        void Start()
        {
            _excludeEffectNames.SettingChanged += excludeEffectNamesSettingChanged;

            LocalizedStringOverridePatch.OverrideLanguageString += overrideLanguageString;
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;

            InstanceTracker.GetInstancesList<CreditsPanelController>().TryDo(scrambleCredits);
            CreditsPanelControllerHooks.OnCreditsPanelControllerEnableGlobal += scrambleCredits;
        }

        void OnDestroy()
        {
            _excludeEffectNames.SettingChanged -= excludeEffectNamesSettingChanged;

            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
            LocalizedStringOverridePatch.OverrideLanguageString -= overrideLanguageString;

            CreditsPanelControllerHooks.OnCreditsPanelControllerEnableGlobal -= scrambleCredits;

            foreach (TMP_Text label in _processedTextLabels)
            {
                if (label && label.textPreprocessor is ScramblePreprocessor)
                {
                    label.textPreprocessor = null;
                    label.ForceMeshUpdate();
                }
            }

            _processedTextLabels.Clear();
        }

        static void excludeEffectNamesSettingChanged(object sender, ConfigChangedArgs<bool> e)
        {
            LocalizedStringOverridePatch.RefreshLanguageTokens();
        }

        void onCurrentLanguageChanged()
        {
            _tokenOverrideCache.Clear();
        }

        void scrambleCredits(CreditsPanelController creditsController)
        {
            HGTextMeshProUGUI[] labels = creditsController.GetComponentsInChildren<HGTextMeshProUGUI>(true);
            _processedTextLabels.EnsureCapacity(_processedTextLabels.Count + labels.Length);

            for (int i = 0; i < labels.Length; i++)
            {
                HGTextMeshProUGUI label = labels[i];
                if (label && !label.GetComponent<LanguageTextMeshController>())
                {
                    if (label.textPreprocessor != null)
                    {
                        Log.Warning($"Overriding text preprocessor {label.textPreprocessor} for {label} (in {creditsController})");
                    }

                    label.textPreprocessor = new ScramblePreprocessor(_baseScrambleSeed ^ (ulong)i);
                    label.ForceMeshUpdate();
                    _processedTextLabels.Add(label);
                }
            }
        }

        void overrideLanguageString(ref string str, string token, Language language)
        {
            if (string.IsNullOrEmpty(str) || !language.TokenIsRegistered(token))
                return;

            if (ChaosEffectActivationSignaler_TwitchVote.IsConnectionMessageToken(token))
                return;

            if (_excludeEffectNames.Value && ChaosEffectCatalog.IsEffectRelatedToken(token))
                return;

            // Miscellaneous important tokens that should always be readable
            switch (token)
            {
                case "ROC_ATTEMPT_RECONNECT_NOT_LOGGED_IN_HEADER":
                case "ROC_ATTEMPT_RECONNECT_NOT_LOGGED_IN_DESCRIPTION_TWITCH":
                case "TWITCH_USER_TOKEN_AUTHENTICATING_HEADER":
                case "TWITCH_USER_TOKEN_AUTHENTICATING_DESCRIPTION":
                case "TWITCH_USER_TOKEN_AUTHENTICATED_HEADER":
                case "TWITCH_USER_TOKEN_AUTHENTICATED_DESCRIPTION":
                case "TWITCH_USER_TOKEN_AUTHENTICATION_ERROR_HEADER":
                case "TWITCH_USER_TOKEN_AUTHENTICATION_ERROR_DESCRIPTION":
                case "POPUP_CONFIG_UPDATE_HEADER":
                case "POPUP_CONFIG_UPDATE_DESCRIPTION":
                case "POPUP_CONFIG_UPDATE_RESET":
                case "POPUP_CONFIG_UPDATE_IGNORE":
                case "POPUP_TWITCH_USER_TOKEN_EXPIRED_HEADER":
                case "POPUP_TWITCH_USER_TOKEN_EXPIRED_DESCRIPTION":
                case "POPUP_TWITCH_USER_TOKEN_ABOUT_TO_EXPIRE_HEADER":
                case "POPUP_TWITCH_USER_TOKEN_ABOUT_TO_EXPIRE_DESCRIPTION":
                case "POPUP_TWITCH_USER_TOKEN_MISSING_SCOPES_HEADER":
                case "POPUP_TWITCH_USER_TOKEN_MISSING_SCOPES_DESCRIPTION":
                case "POPUP_TWITCH_LOGIN_OAUTH_NO_LONGER_USED_HEADER":
                case "POPUP_TWITCH_LOGIN_OAUTH_NO_LONGER_USED_DESCRIPTION":
                    return;
            }

            if (_tokenOverrideCache.TryGetValue(token, out string cachedOverride))
            {
                str = cachedOverride;
                return;
            }

            str = ScrambleString(str);

            _tokenOverrideCache.Add(token, str);
        }

        public string ScrambleString(string str)
        {
            return scrambleString(str, _baseScrambleSeed + (ulong)StringComparer.OrdinalIgnoreCase.GetHashCode(str));
        }

        static string scrambleString(string str, ulong scrambleSeed)
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(scrambleSeed);

            MatchCollection formatMatches = _stringFormatsRegex.Matches(str);
            int formatMatchIndex = 0;

            MatchCollection tagMatches = _richTextTagsRegex.Matches(str);
            int tagMatchIndex = 0;

            _sharedResultBuilder.Clear();
            _sharedWordBuilder.Clear();

            _sharedResultBuilder.EnsureCapacity(str.Length);
            _sharedWordBuilder.EnsureCapacity(str.Length);

            void endCurrentWord()
            {
                if (_sharedWordBuilder.Length == 0)
                    return;

                char[] wordLetters = _sharedWordBuilder.ToString().ToCharArray();
                Util.ShuffleArray(wordLetters, rng);

                _sharedResultBuilder.Append(wordLetters);
                _sharedWordBuilder.Clear();
            }

            for (int i = 0; i < str.Length; i++)
            {
                bool trySkipMatch(MatchCollection matchCollection, ref int currentIndex)
                {
                    if (currentIndex >= matchCollection.Count)
                        return false;

                    Match currentMatch = matchCollection[currentIndex];
                    if (currentMatch.Index == i)
                    {
                        endCurrentWord();

                        _sharedResultBuilder.Append(currentMatch.Value);
                        i += currentMatch.Length - 1;

                        currentIndex++;

                        return true;
                    }

                    return false;
                }

                if (trySkipMatch(tagMatches, ref tagMatchIndex) ||
                    trySkipMatch(formatMatches, ref formatMatchIndex))
                {
                    continue;
                }
                else if (char.IsLetterOrDigit(str[i]) || str[i] == '\'')
                {
                    _sharedWordBuilder.Append(str[i]);
                }
                else
                {
                    endCurrentWord();
                    _sharedResultBuilder.Append(str[i]);
                }
            }

            endCurrentWord();

            string result = _sharedResultBuilder.ToString();
            _sharedResultBuilder.Clear();

            return result;
        }

        sealed class ScramblePreprocessor : ITextPreprocessor
        {
            readonly ulong _scrambleSeed;

            string _lastText;
            string _cachedScramble;

            public ScramblePreprocessor(ulong scrambleSeed)
            {
                _scrambleSeed = scrambleSeed;
            }

            public string PreprocessText(string text)
            {
                if (_lastText != text)
                {
                    _cachedScramble = scrambleString(text, _scrambleSeed);
                    _lastText = text;
                }

                return _cachedScramble;
            }
        }
    }
}
