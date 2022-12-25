using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Artifacts;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos
{
    static class MidRunArtifactsHandler
    {
        [SystemInitializer]
        static void Init()
        {
            RunArtifactManager.onArtifactEnabledGlobal += static (runArtifactManager, artifactDef) =>
            {
                if (Stage.instance)
                {
                    if (artifactDef == RoR2Content.Artifacts.sacrificeArtifactDef)
                    {
                        onSacrificeEnabled();
                    }
                    else if (artifactDef == RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef)
                    {
                        onMetamorphosisEnabled();
                    }
                    else if (artifactDef == RoR2Content.Artifacts.enigmaArtifactDef)
                    {
                        onEnigmaEnabled();
                    }

                    CharacterMaster localPlayerMaster = PlayerUtils.GetLocalUserMaster();
                    if (localPlayerMaster)
                    {
                        CharacterMasterNotificationQueue.PushArtifactNotification(localPlayerMaster, artifactDef);
                    }
                }
            };
        }

        // FIXES: Enabling/Disabling an artifact doesn't refresh the info panel, so Artifact of Kin doesn't display properly if it's enabled mid-run
        internal static void PatchEnemyInfoPanel()
        {
            On.RoR2.UI.EnemyInfoPanel.Init += static orig =>
            {
                orig();

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                RunArtifactManager.onArtifactEnabledGlobal += static (runArtifactManager, artifactDef) => EnemyInfoPanel.MarkDirty();
                RunArtifactManager.onArtifactDisabledGlobal += static (runArtifactManager, artifactDef) => EnemyInfoPanel.MarkDirty();
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            };
        }

        static void onSacrificeEnabled()
        {
            if (!NetworkServer.active)
                return;

            List<InteractableTracker> interactables = InstanceTracker.GetInstancesList<InteractableTracker>();
            for (int i = interactables.Count - 1; i >= 0; i--)
            {
                InteractableTracker interactable = interactables[i];
                if (!interactable)
                    continue;

                InteractableSpawnCard spawnCard = interactable.SpawnCard;
                if (!spawnCard)
                    continue;

                if (spawnCard.skipSpawnWhenSacrificeArtifactEnabled)
                {
                    NetworkServer.Destroy(interactable.gameObject);
                }
            }
        }

        static void onMetamorphosisEnabled()
        {
            if (!NetworkServer.active)
                return;

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                playerBody.master.Respawn(playerBody.footPosition, playerBody.GetRotation());
            }
        }

        static void onEnigmaEnabled()
        {
            if (!NetworkServer.active)
                return;

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                // Give equipment if missing
                EnigmaArtifactManager.OnPlayerCharacterBodyStartServer(playerBody);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
