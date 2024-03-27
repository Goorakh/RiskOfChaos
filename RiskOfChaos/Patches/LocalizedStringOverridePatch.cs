using HarmonyLib;
using RiskOfChaos.Utilities;
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

        public static void RefreshLanguageTokens()
        {
            if (Language_onCurrentLanguageChanged != null && Language_onCurrentLanguageChanged.GetValue(null) is Action onCurrentLanguageChanged)
            {
                onCurrentLanguageChanged?.Invoke();
            }

            OverrideObjectiveTrackerDirtyPatch.ForceRefresh();

            foreach (BossGroup bossGroup in InstanceTracker.GetInstancesList<BossGroup>())
            {
                BossUtils.RefreshBossTitle(bossGroup);
            }
        }

        public delegate void OverrideLanguageStringDelegate(ref string str, string token, Language language);
        static event OverrideLanguageStringDelegate _overrideLanguageString;

        public static event OverrideLanguageStringDelegate OverrideLanguageString
        {
            add
            {
                _overrideLanguageString += value;
                tryApplyPatches();
                RefreshLanguageTokens();
            }
            remove
            {
                _overrideLanguageString -= value;
                RefreshLanguageTokens();
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

        static bool canModifyToken(string token)
        {
            switch (token)
            {
                case "DEFAULT_FONT":
                case "CHAOS_EFFECT_UNHANDLED_EXCEPTION_MESSAGE":
                    return false;
            }

            return true;
        }

        static string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            string result = orig(self, token);

            if (_overrideLanguageString != null && canModifyToken(token))
            {
                string tmpResult = result;

                try
                {
                    _overrideLanguageString(ref tmpResult, token, self);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to override language token {token}: {e}");
                }

                result = tmpResult;
            }

            return result;
        }
    }
}
