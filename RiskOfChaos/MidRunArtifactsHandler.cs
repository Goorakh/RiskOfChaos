using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
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
        static void InitListeners()
        {
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
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
                else if (artifactDef == RoR2Content.Artifacts.glassArtifactDef)
                {
                    onGlassEnabledOrDisabled();
                }
                else if (artifactDef == RoR2Content.Artifacts.singleMonsterTypeArtifactDef)
                {
                    onKinEnabledOrDisabled();
                }

                CharacterMaster localPlayerMaster = PlayerUtils.GetLocalUserMaster();
                if (localPlayerMaster)
                {
                    CharacterMasterNotificationQueue.PushArtifactNotification(localPlayerMaster, artifactDef);
                }
            }
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (Stage.instance)
            {
                if (artifactDef == RoR2Content.Artifacts.glassArtifactDef)
                {
                    onGlassEnabledOrDisabled();
                }
                else if (artifactDef == RoR2Content.Artifacts.singleMonsterTypeArtifactDef)
                {
                    onKinEnabledOrDisabled();
                }
            }
        }

        static void onKinEnabledOrDisabled()
        {
            // Enabling/Disabling an artifact doesn't refresh the info panel, so Artifact of Kin doesn't display properly if it's enabled or disabled mid-run

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            EnemyInfoPanel.MarkDirty();
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
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
                playerBody.master.Respawn(CharacterMasterExtensions.CharacterRespawnFlags.KeepState);
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

        static void onGlassEnabledOrDisabled()
        {
            if (!NetworkServer.active)
                return;

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }
    }
}
