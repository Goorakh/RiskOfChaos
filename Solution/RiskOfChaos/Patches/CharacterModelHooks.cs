using RoR2;

namespace RiskOfChaos.Patches
{
    static class CharacterModelHooks
    {
        public delegate void OnCharacterModelStartDelegate(CharacterModel model);
        public static event OnCharacterModelStartDelegate OnCharacterModelStartGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterModel.Start += CharacterModel_Start;
        }

        static void CharacterModel_Start(On.RoR2.CharacterModel.orig_Start orig, CharacterModel self)
        {
            orig(self);
            OnCharacterModelStartGlobal?.Invoke(self);
        }
    }
}
