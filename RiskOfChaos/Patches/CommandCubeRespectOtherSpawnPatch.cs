using RoR2;

namespace RiskOfChaos.Patches
{
    static class CommandCubeRespectOtherSpawnPatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer += CommandArtifactManager_OnDropletHitGroundServer;
        }

        static void CommandArtifactManager_OnDropletHitGroundServer(On.RoR2.Artifacts.CommandArtifactManager.orig_OnDropletHitGroundServer orig, ref GenericPickupController.CreatePickupInfo createPickupInfo, ref bool shouldSpawn)
        {
            if (!shouldSpawn)
                return;

            orig(ref createPickupInfo, ref shouldSpawn);
        }
    }
}
