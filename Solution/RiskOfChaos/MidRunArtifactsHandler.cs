using RiskOfChaos.Content;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Artifacts;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos
{
    static class MidRunArtifactsHandler
    {
        [AddressableReference("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset")]
        static readonly InteractableSpawnCard _lemurianEggSpawnCard;

        [SystemInitializer]
        static void Init()
        {
            RunArtifactManager.onArtifactEnabledGlobal += RunArtifactManager_onArtifactEnabledGlobal;
            RunArtifactManager.onArtifactDisabledGlobal += RunArtifactManager_onArtifactDisabledGlobal;
        }

        static void RunArtifactManager_onArtifactEnabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            onArtifactStateChanged(artifactDef, true);
        }

        static void RunArtifactManager_onArtifactDisabledGlobal(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            onArtifactStateChanged(artifactDef, false);
        }

        static void onArtifactStateChanged(ArtifactDef artifactDef, bool enabled)
        {
            if (!Stage.instance)
                return;
            
            if (enabled)
            {
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

            if (artifactDef == RoR2Content.Artifacts.Sacrifice)
            {
                if (enabled)
                {
                    onSacrificeEnabled();
                }
            }
            else if (artifactDef == RoR2Content.Artifacts.RandomSurvivorOnRespawn)
            {
                if (enabled)
                {
                    onMetamorphosisEnabled();
                }
            }
            else if (artifactDef == RoR2Content.Artifacts.SingleMonsterType)
            {
                if (!enabled)
                {
                    onKinDisabled();
                }
            }
            else if (artifactDef == RoR2Content.Artifacts.Enigma)
            {
                if (enabled)
                {
                    onEnigmaEnabled();
                }
            }
            else if (artifactDef == CU8Content.Artifacts.Devotion)
            {
                if (enabled)
                {
                    onDevotionEnabled();
                }
            }
            else if (artifactDef == CU8Content.Artifacts.Delusion)
            {
                onDelusionEnabledOrDisabled(enabled);
            }
            else if (artifactDef == DLC2Content.Artifacts.Rebirth)
            {
                if (enabled)
                {
                    onRebirthEnabled();
                }
            }

            CharacterBodyUtils.MarkAllBodyStatsDirty();
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

            List<ObjectSpawnCardTracker> spawnedObjects = InstanceTracker.GetInstancesList<ObjectSpawnCardTracker>();
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                ObjectSpawnCardTracker spawnedObject = spawnedObjects[i];
                if (!spawnedObject)
                    continue;

                SpawnCard spawnCard = spawnedObject.SpawnCard;
                if (spawnCard is InteractableSpawnCard interactableSpawnCard)
                {
                    if (interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled)
                    {
                        NetworkServer.Destroy(spawnedObject.gameObject);
                    }
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
                    spawnCard = _lemurianEggSpawnCard
                };
            }

            if (lemurianEggSpawnCard == null || !lemurianEggSpawnCard.spawnCard)
            {
                Log.Error("Failed to find valid egg spawn card");
                return;
            }

            Xoroshiro128Plus eggSpawnRng = new Xoroshiro128Plus(RoR2Application.rng.nextUlong);

            List<ObjectSpawnCardTracker> spawnedObjects = InstanceTracker.GetInstancesList<ObjectSpawnCardTracker>();
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                ObjectSpawnCardTracker spawnedObject = spawnedObjects[i];
                if (!spawnedObject)
                    continue;

                SpawnCard spawnCard = spawnedObject.SpawnCard;
                if (spawnCard is InteractableSpawnCard interactableSpawnCard)
                {
                    if (interactableSpawnCard.skipSpawnWhenDevotionArtifactEnabled)
                    {
                        DirectorPlacementRule eggPlacementRule = new DirectorPlacementRule
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Direct,
                            spawnOnTarget = spawnedObject.transform,
                        };

                        DirectorSpawnRequest eggSpawnRequest = new DirectorSpawnRequest(lemurianEggSpawnCard.spawnCard, eggPlacementRule, eggSpawnRng);

                        eggSpawnRequest.spawnCard.DoSpawn(spawnedObject.transform.position, spawnedObject.transform.rotation, eggSpawnRequest);

                        NetworkServer.Destroy(spawnedObject.gameObject);
                    }
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
                    if (networkUser.isLocalPlayer)
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
}
