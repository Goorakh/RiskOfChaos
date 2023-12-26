using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("scramble_text", 120f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class ScrambleText : TimedEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _excludeEffectNames =
            ConfigFactory<bool>.CreateConfig("Exclude Effect Names", true)
                               .Description("Excludes chaos effect names from being scrambled")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        // Match string formats ({0}, {0:F0}, etc.)
        static readonly Regex _stringFormatsRegex = new Regex(@"\{\d+(?::\w+)?\}", RegexOptions.Compiled);

        // Match rich text tags (<i>, <size=12>, </b>, etc.)
        static readonly Regex _richTextTagsRegex = new Regex(@"<[^<>]+>", RegexOptions.Compiled);

        static readonly StringBuilder _sharedResultBuilder = new StringBuilder();
        static readonly StringBuilder _sharedWordBuilder = new StringBuilder();

        readonly Dictionary<string, string> _tokenOverrideCache = new Dictionary<string, string>();

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
        }

        public override void OnEnd()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
            LocalizedStringOverridePatch.OverrideLanguageString -= overrideLanguageString;
        }

        void onCurrentLanguageChanged()
        {
            _tokenOverrideCache.Clear();
        }

        void overrideLanguageString(ref string str, string token, Language language)
        {
            switch (token)
            {
                case "DEFAULT_FONT":
                case "CHAOS_EFFECT_UNHANDLED_EXCEPTION_MESSAGE":
                    return;
            }

            if (ChaosEffectActivationSignaler_TwitchVote.IsConnectionMessageToken(token))
                return;

            if (_excludeEffectNames.Value)
            {
                if (ChaosEffectCatalog.FindEffectIndexByNameToken(token) != ChaosEffectIndex.Invalid)
                    return;

                switch (token)
                {
                    case "CHAOS_ACTIVE_EFFECTS_BAR_TITLE":
                    case "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_SINGLE_FORMAT":
                    case "CHAOS_ACTIVE_EFFECT_UNTIL_STAGE_END_MULTI_FORMAT":
                    case "CHAOS_ACTIVE_EFFECT_FIXED_DURATION_FORMAT":
                    case "CHAOS_ACTIVE_EFFECT_FIXED_DURATION_LONG_FORMAT":
                    case "CHAOS_NEXT_EFFECT_DISPLAY_FORMAT":
                    case "CHAOS_EFFECT_ACTIVATE":
                    case "CHAOS_EFFECT_VOTING_RANDOM_OPTION_NAME":
                    case "TIMED_TYPE_UNTIL_STAGE_END_SINGLE_FORMAT":
                    case "TIMED_TYPE_UNTIL_STAGE_END_MULTI_FORMAT":
                    case "TIMED_TYPE_FIXED_DURATION_FORMAT":
                    case "TIMED_TYPE_PERMANENT_FORMAT":
                        return;
                }
            }

            if (_tokenOverrideCache.TryGetValue(token, out string cachedOverride))
            {
                str = cachedOverride;
                return;
            }

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_baseScrambleSeed ^ (ulong)token.GetHashCode());

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

            str = _sharedResultBuilder.ToString();
            _sharedResultBuilder.Clear();

            _tokenOverrideCache.Add(token, str);
        }
    }
}
