using HarmonyLib;
using RoR2;
using System;
using System.Reflection;

namespace RiskOfChaos.Patches
{
    static class LocalizedStringOverridePatch
    {
        static readonly FieldInfo Language_onCurrentLanguageChanged = AccessTools.DeclaredField(typeof(Language), nameof(Language.onCurrentLanguageChanged));

        static LocalizedStringOverridePatch()
        {
            if (Language_onCurrentLanguageChanged == null)
            {
                Log.Error($"Failed to find field {nameof(Language)}.{nameof(Language.onCurrentLanguageChanged)}");
            }
        }

        static void refreshLanguageTokens()
        {
            if (Language_onCurrentLanguageChanged != null && Language_onCurrentLanguageChanged.GetValue(null) is Action onCurrentLanguageChanged)
            {
                onCurrentLanguageChanged?.Invoke();
            }

            OverrideObjectiveTrackerDirtyPatch.ForceRefresh();
        }

        public delegate void OverrideLanguageStringDelegate(ref string str, string token, Language language);
        static event OverrideLanguageStringDelegate _overrideLanguageString;

        public static event OverrideLanguageStringDelegate OverrideLanguageString
        {
            add
            {
                _overrideLanguageString += value;
                tryApplyPatches();
                refreshLanguageTokens();
            }
            remove
            {
                _overrideLanguageString -= value;
                refreshLanguageTokens();
            }
        }

        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;

            _hasAppliedPatches = true;
        }

        static string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            string result = orig(self, token);

            string tmpResult = result;
            try
            {
                _overrideLanguageString?.Invoke(ref tmpResult, token, self);
                result = tmpResult;
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Failed to override language token {token}: {e}");
            }

            return result;
        }
    }
}
