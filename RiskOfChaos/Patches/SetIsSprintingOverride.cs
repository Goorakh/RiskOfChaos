using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches
{
    static class SetIsSprintingOverride
    {
        public static bool PatchSuccessful { get; private set; }

        public delegate void OverrideCharacterSprintingDelegate(CharacterBody body, ref bool isSprinting);
        public static event OverrideCharacterSprintingDelegate OverrideCharacterSprinting;

        [SystemInitializer]
        static void Init()
        {
            MethodInfo set_isSprinting_MI = AccessTools.DeclaredPropertySetter(typeof(CharacterBody), nameof(CharacterBody.isSprinting));
            if (set_isSprinting_MI != null)
            {
                new Hook(set_isSprinting_MI, CharacterBody_set_isSprinting);
                PatchSuccessful = true;
            }
            else
            {
                Log.Error($"Failed to find {nameof(CharacterBody)}.set_{nameof(CharacterBody.isSprinting)}");
                PatchSuccessful = false;
            }
        }

        delegate void orig_set_isSprinting(CharacterBody self, bool value);
        static void CharacterBody_set_isSprinting(orig_set_isSprinting orig, CharacterBody self, bool value)
        {
            OverrideCharacterSprinting?.Invoke(self, ref value);
            orig(self, value);
        }
    }
}
