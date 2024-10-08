using RoR2;

namespace RiskOfChaos.Patches
{
    static class CharacterTokenOverridePatch
    {
        public delegate void OverrideNameTokenDelegate(CharacterBody body, ref string nameToken);
        public static event OverrideNameTokenDelegate OverrideNameToken;

        public delegate void OverrideDisplayNameDelegate(CharacterBody body, ref string displayName);
        public static event OverrideDisplayNameDelegate OverrideDisplayName;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterBody.GetDisplayName += CharacterBody_GetDisplayName;
        }

        static string CharacterBody_GetDisplayName(On.RoR2.CharacterBody.orig_GetDisplayName orig, CharacterBody self)
        {
            string originalNameToken = self.baseNameToken;

            OverrideNameToken?.Invoke(self, ref self.baseNameToken);

            string displayName;
            try
            {
                displayName = orig(self);
            }
            finally
            {
                self.baseNameToken = originalNameToken;
            }

            OverrideDisplayName?.Invoke(self, ref displayName);
            return displayName;
        }
    }
}
