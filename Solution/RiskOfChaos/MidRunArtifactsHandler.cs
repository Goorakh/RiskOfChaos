using RiskOfChaos.Patches;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Artifacts;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

            // Enabling/Disabling an artifact doesn't refresh the info panel, so Artifact of Kin display doesn't update if it's enabled or disabled mid-run
            SingleMonsterTypeChangedHook.OnSingleMonsterTypeChanged += EnemyInfoPanel.MarkDirty;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (Stage.instance)
            {
                if (artifactDef == RoR2Content.Artifacts.Sacrifice)
                {
                    onSacrificeEnabled();
                }
                else if (artifactDef == RoR2Content.Artifacts.RandomSurvivorOnRespawn)
                {
                    onMetamorphosisEnabled();
                }
                else if (artifactDef == RoR2Content.Artifacts.Enigma)
                {
                    onEnigmaEnabled();
                }
                if (artifactDef == CU8Content.Artifacts.Devotion)
                {
                    onDevotionEnabled();
                }
                else if (artifactDef == CU8Content.Artifacts.Delusion)
                {
                    onDelusionEnabledOrDisabled(true);
                }
                else if (artifactDef == DLC2Content.Artifacts.Rebirth)
                {
                    onRebirthEnabled();
                }

                foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                {
                    body.MarkAllStatsDirty();
                }

                foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
                {
                    if (user is null)
                        continue;

                    CharacterMaster localPlayerMaster = user.cachedMaster;
                    if (localPlayerMaster)
                    {
                        CharacterMasterNotificationQueue.PushArtifactNotification(localPlayerMaster, artifactDef);
                    }
                }
            }
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (Stage.instance)
            {
                if (artifactDef == RoR2Content.Artifacts.SingleMonsterType)
                {
                    onKinDisabled();
                }
                else if (artifactDef == CU8Content.Artifacts.Delusion)
                {
                    onDelusionEnabledOrDisabled(false);
                }
            }
        }

        static void onKinDisabled()
        {
            if (!NetworkServer.active)
                return;

            if (Stage.instance)
            {
                Stage.instance.singleMonsterTypeBodyIndex = BodyIndex.None;
            }
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
                playerBody.master.Respawn(CharacterRespawnFlags.KeepState);
            }
        }

        static void onEnigmaEnabled()
        {
            if (!NetworkServer.active)
                return;

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                // Give equipment if missing
                EnigmaArtifactManager.OnPlayerCharacterBodyStartServer(playerBody);
            }
        }

        static void onDevotionEnabled()
        {
            if (!NetworkServer.active)
                return;

            SceneDirector sceneDirector = null;
            if (DirectorCore.instance)
            {
                sceneDirector = DirectorCore.instance.GetComponent<SceneDirector>();
            }

            DirectorCard lemurianEggSpawnCard;
            if (sceneDirector)
            {
                lemurianEggSpawnCard = sceneDirector.lumerianEgg;
            }
            else
            {
                Log.Warning("Failed to find SceneDirector");

                lemurianEggSpawnCard = new DirectorCard
                {
                    spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset").WaitForCompletion()
                };
            }

            if (lemurianEggSpawnCard == null || !lemurianEggSpawnCard.spawnCard)
            {
                Log.Error("Failed to find valid egg spawn card");
                return;
            }

            DirectorSpawnRequest eggDummySpawnRequest = new DirectorSpawnRequest(lemurianEggSpawnCard.spawnCard, new DirectorPlacementRule(), RoR2Application.rng);

            List<InteractableTracker> interactables = InstanceTracker.GetInstancesList<InteractableTracker>();
            for (int i = interactables.Count - 1; i >= 0; i--)
            {
                InteractableTracker interactable = interactables[i];
                if (!interactable)
                    continue;

                InteractableSpawnCard spawnCard = interactable.SpawnCard;
                if (!spawnCard)
                    continue;

                if (spawnCard.skipSpawnWhenDevotionArtifactEnabled)
                {
                    lemurianEggSpawnCard.spawnCard.DoSpawn(interactable.transform.position, interactable.transform.rotation, eggDummySpawnRequest);

                    NetworkServer.Destroy(interactable.gameObject);
                }
            }
        }

        static void onDelusionEnabledOrDisabled(bool enabled)
        {
            TeleporterInteraction teleporterInteraction = TeleporterInteraction.instance;

            bool teleporterFinished;
            if (teleporterInteraction)
            {
                teleporterFinished = teleporterInteraction.isCharged;
            }
            else
            {
                teleporterFinished = false;
            }

            foreach (DelusionChestControllerTracker delusionChestTracker in InstanceTracker.GetInstancesList<DelusionChestControllerTracker>())
            {
                DelusionChestController delusionChestController = delusionChestTracker.DelusionChestController;
                if (!delusionChestController)
                    continue;

                if (enabled)
                {
                    if (!delusionChestController.hasBeenReset)
                    {
                        delusionChestController.enabled = true;

                        PickupIndex pendingPickupIndex = delusionChestTracker.TakePendingDelusionPickupIndex();
                        if (pendingPickupIndex.isValid)
                        {
                            delusionChestController.delusionChest.CallRpcSetDelusionPickupIndex(pendingPickupIndex);
                        }

                        if (teleporterFinished)
                        {
                            delusionChestController.delusionChest.CallRpcResetChests();
                        }
                    }
                }
                else
                {
                    if (delusionChestController.hasBeenReset)
                    {
                        delusionChestController.enabled = false;

                        NetworkUIPromptController uiPromptController = delusionChestController._netUIPromptController;
                        if (uiPromptController)
                        {
                            uiPromptController.enabled = false;
                        }

                        PickupPickerController pickupPickerController = delusionChestController._pickupPickerController;
                        if (pickupPickerController)
                        {
                            pickupPickerController.enabled = false;
                            pickupPickerController.SetAvailable(false);
                        }

                        if (delusionChestController.TryGetComponent(out EntityStateMachine stateMachine))
                        {
                            stateMachine.SetNextState(new EntityStates.Barrel.Opening());
                        }
                    }
                }
            }
        }

        static void onRebirthEnabled()
        {
            if (NetworkServer.active && Run.instance)
            {
                Run.instance.ServerGiveRebirthItems();
            }

            if (NetworkClient.active)
            {
                foreach (NetworkUser networkUser in NetworkUser.readOnlyInstancesList)
                {
                    UserProfile profile = networkUser.localUser?.userProfile;
                    if (profile != null && !string.IsNullOrEmpty(profile.RebirthItem))
                    {
                        profile.RebirthItem = null;
                        profile.RequestEventualSave();
                    }
                }
            }
        }
    }
}
