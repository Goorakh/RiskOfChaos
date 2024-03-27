using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("scramble_text", 120f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class ScrambleText : TimedEffect
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        [EffectConfig]
        static readonly ConfigHolder<bool> _excludeEffectNames =
            ConfigFactory<bool>.CreateConfig("Exclude Effect Names", false)
                               .Description("Excludes chaos effect names from being scrambled")
                               .OptionConfig(new CheckBoxConfig())
                               .OnValueChanged(() =>
                               {
                                   if (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                                   {
                                       LocalizedStringOverridePatch.RefreshLanguageTokens();
                                   }
                               })
                               .Build();

        // Match string formats ({0}, {0:F0}, etc.)
        static readonly Regex _stringFormatsRegex = new Regex(@"\{\d+(?::\w+)?\}", RegexOptions.Compiled);

        // Match rich text tags (<i>, <size=12>, </b>, etc.)
        static readonly Regex _richTextTagsRegex = new Regex(@"<[^<>]+>", RegexOptions.Compiled);

        static readonly StringBuilder _sharedResultBuilder = new StringBuilder();
        static readonly StringBuilder _sharedWordBuilder = new StringBuilder();

        readonly Dictionary<string, string> _tokenOverrideCache = [];

        readonly HashSet<TMP_Text> _processedTextLabels = [];

        ulong _baseScrambleSeed;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();
            _baseScrambleSeed = RNG.nextUlong;
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.WritePackedUInt64(_baseScrambleSeed);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _baseScrambleSeed = reader.ReadPackedUInt64();
        }

        public override void OnStart()
        {
            LocalizedStringOverridePatch.OverrideLanguageString += overrideLanguageString;
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;

            On.RoR2.UI.CreditsPanelController.OnEnable += CreditsPanelController_OnEnable;
            InstanceTracker.GetInstancesList<CreditsPanelController>().TryDo(scrambleCredits);
        }

        public override void OnEnd()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
            LocalizedStringOverridePatch.OverrideLanguageString -= overrideLanguageString;

            On.RoR2.UI.CreditsPanelController.OnEnable -= CreditsPanelController_OnEnable;

            foreach (TMP_Text label in _processedTextLabels)
            {
                label.textPreprocessor = null;
                label.ForceMeshUpdate();
            }

            _processedTextLabels.Clear();
        }

        void onCurrentLanguageChanged()
        {
            _tokenOverrideCache.Clear();
        }

        void CreditsPanelController_OnEnable(On.RoR2.UI.CreditsPanelController.orig_OnEnable orig, CreditsPanelController self)
        {
            orig(self);

            scrambleCredits(self);
        }

        void scrambleCredits(CreditsPanelController creditsController)
        {
            HGTextMeshProUGUI[] labels = creditsController.GetComponentsInChildren<HGTextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (!labels[i] || labels[i].GetComponent<LanguageTextMeshController>())
                    continue;

                labels[i].textPreprocessor = new ScramblePreprocessor(_baseScrambleSeed ^ (ulong)i);
                labels[i].ForceMeshUpdate();
                _processedTextLabels.Add(labels[i]);
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

            if (_tokenOverrideCache.TryGetValue(token, out string cachedOverride))
            {
                str = cachedOverride;
                return;
            }

            str = scrambleString(str, _baseScrambleSeed ^ (ulong)token.GetHashCode());

            _tokenOverrideCache.Add(token, str);
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

            void endCurrentWord()
            {
                if (_sharedWordBuilder.Length == 0)
                    return;

                char[] wordLetters = _sharedWordBuilder.ToString().ToCharArray();
                Util.ShuffleArray(wordLetters, rng);

                _sharedResultBuilder.Append(new string(wordLetters));
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

        class ScramblePreprocessor : ITextPreprocessor
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
