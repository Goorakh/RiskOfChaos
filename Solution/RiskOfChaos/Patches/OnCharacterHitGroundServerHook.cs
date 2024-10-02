using RoR2;
using System;

namespace RiskOfChaos.Patches
{
    static class OnCharacterHitGroundServerHook
    {
        public delegate void CharacterHitGroundDelegate(CharacterBody characterBody, in CharacterMotor.HitGroundInfo hitGroundInfo);
        public static event CharacterHitGroundDelegate OnCharacterHitGround;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GlobalEventManager.OnCharacterHitGroundServer += GlobalEventManager_OnCharacterHitGroundServer;
        }

        static void GlobalEventManager_OnCharacterHitGroundServer(On.RoR2.GlobalEventManager.orig_OnCharacterHitGroundServer orig, GlobalEventManager self, CharacterBody characterBody, CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            try
            {
                OnCharacterHitGround?.Invoke(characterBody, hitGroundInfo);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);
            }

            orig(self, characterBody, hitGroundInfo);
        }
    }
}
