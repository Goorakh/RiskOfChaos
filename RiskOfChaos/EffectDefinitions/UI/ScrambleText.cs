using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.UI;
using System;
using System.Reflection;
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

        static readonly FieldInfo Language_onCurrentLanguageChanged = typeof(Language).GetField(nameof(Language.onCurrentLanguageChanged), BindingFlags.NonPublic | BindingFlags.Static);

        static ScrambleText()
        {
            if (Language_onCurrentLanguageChanged == null)
            {
                Log.Error($"Failed to find field {nameof(Language)}.{nameof(Language.onCurrentLanguageChanged)}");
            }
        }

        static void forceRefreshLanguageTokens()
        {
            if (Language_onCurrentLanguageChanged != null && Language_onCurrentLanguageChanged.GetValue(null) is Action onCurrentLanguageChanged)
            {
                onCurrentLanguageChanged?.Invoke();
            }

            static void OverrideObjectiveTrackerDirtyPatch_OverrideObjectiveTrackerDirty(ObjectivePanelController.ObjectiveTracker objective, ref bool isDirty)
            {
                isDirty = true;
            }

            OverrideObjectiveTrackerDirtyPatch.OverrideObjectiveTrackerDirty += OverrideObjectiveTrackerDirtyPatch_OverrideObjectiveTrackerDirty;

            RoR2Application.onNextUpdate += () =>
            {
                OverrideObjectiveTrackerDirtyPatch.OverrideObjectiveTrackerDirty -= OverrideObjectiveTrackerDirtyPatch_OverrideObjectiveTrackerDirty;
            };
        }

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
            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
            forceRefreshLanguageTokens();
        }

        public override void OnEnd()
        {
            On.RoR2.Language.GetLocalizedStringByToken -= Language_GetLocalizedStringByToken;
            forceRefreshLanguageTokens();
        }

        // Match string formats ({0}, {0:F0}, etc.)
        static readonly Regex _stringFormatsRegex = new Regex(@"\{\d+(?::\w+)?\}", RegexOptions.Compiled);

        // Match rich text tags (<i>, <size=12>, </b>, etc.)
        static readonly Regex _richTextTagsRegex = new Regex(@"<[^<>]+>", RegexOptions.Compiled);

        static readonly StringBuilder _sharedResultBuilder = new StringBuilder();
        static readonly StringBuilder _sharedWordBuilder = new StringBuilder();

        string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            string str = orig(self, token);

            if (_excludeEffectNames.Value && ChaosEffectCatalog.FindEffectIndexByNameToken(token) != ChaosEffectIndex.Invalid)
            {
                return str;
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
                else if (char.IsLetterOrDigit(str[i]))
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

            return _sharedResultBuilder.ToString();
        }
    }
}
