using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectUtils.World.Spawn;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_interactable")]
    public sealed class SpawnRandomInteractable : GenericDirectorSpawnEffect<InteractableSpawnCard>
    {
        static SpawnCardEntry[] _spawnCards;

        [SystemInitializer(typeof(CustomSpawnCards))]
        static void Init()
        {
            static InteractableSpawnCard loadSpawnCard(string path)
            {
                InteractableSpawnCard isc = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();

                // Make sure it can always be spawned
                if (isc.skipSpawnWhenSacrificeArtifactEnabled || isc.skipSpawnWhenDevotionArtifactEnabled)
                {
                    string name = isc.name;
                    isc = ScriptableObject.Instantiate(isc);
                    isc.name = $"{name}_UnrestrictedSpawn";
                    isc.skipSpawnWhenSacrificeArtifactEnabled = false;
                    isc.skipSpawnWhenDevotionArtifactEnabled = false;
                }

                return isc;
            }

            static SpawnCardEntry getEntrySingle(string iscPath, float weight = 1f)
            {
                return new SpawnCardEntry(loadSpawnCard(iscPath), weight);
            }

            static SpawnCardEntry getEntryMany(string[] iscPaths, float weight = 1f)
            {
                return new SpawnCardEntry(Array.ConvertAll(iscPaths, loadSpawnCard), weight);
            }

            static InteractableSpawnCard createCauldronSpawnCard(string assetPath)
            {
                int lastSlashIndex = assetPath.LastIndexOf('/');
                string cardName = assetPath.Substring(lastSlashIndex + 1, assetPath.LastIndexOf('.') - lastSlashIndex - 1);

                InteractableSpawnCard spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
                spawnCard.name = cardName;
                spawnCard.prefab = Addressables.LoadAssetAsync<GameObject>(assetPath).WaitForCompletion();
                spawnCard.orientToFloor = true;
                spawnCard.hullSize = HullClassification.Golem;
                spawnCard.requiredFlags = NodeFlags.None;
                spawnCard.forbiddenFlags = NodeFlags.NoChestSpawn;
                spawnCard.occupyPosition = true;
                spawnCard.sendOverNetwork = true;

                return spawnCard;
            }

            InteractableSpawnCard iscNewtStatue = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            {
                iscNewtStatue.name = "iscNewtStatue";
                iscNewtStatue.prefab = RoCContent.NetworkedPrefabs.NewtStatueFixedOrigin;
                iscNewtStatue.orientToFloor = false;
                iscNewtStatue.hullSize = HullClassification.Golem;
                iscNewtStatue.requiredFlags = NodeFlags.None;
                iscNewtStatue.forbiddenFlags = NodeFlags.NoChestSpawn;
                iscNewtStatue.occupyPosition = true;
                iscNewtStatue.sendOverNetwork = true;
            }

            _spawnCards = [
                getEntryMany([
                    "RoR2/Base/Drones/iscBrokenDrone1.asset",
                    "RoR2/Base/Drones/iscBrokenDrone2.asset",
                    "RoR2/Base/Drones/iscBrokenEmergencyDrone.asset",
                    "RoR2/Base/Drones/iscBrokenEquipmentDrone.asset",
                    "RoR2/Base/Drones/iscBrokenFlameDrone.asset",
                    "RoR2/Base/Drones/iscBrokenMegaDrone.asset",
                    "RoR2/Base/Drones/iscBrokenMissileDrone.asset",
                    "RoR2/Base/Drones/iscBrokenTurret1.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/Barrel1/iscBarrel1.asset",
                    "RoR2/DLC1/VoidCoinBarrel/iscVoidCoinBarrel.asset"
                ]),
                getEntrySingle("RoR2/Base/CasinoChest/iscCasinoChest.asset"),
                getEntryMany([
                    "RoR2/Base/CategoryChest/iscCategoryChestDamage.asset",
                    "RoR2/DLC1/CategoryChest2/iscCategoryChest2Damage.asset",

                    "RoR2/Base/CategoryChest/iscCategoryChestHealing.asset",
                    "RoR2/DLC1/CategoryChest2/iscCategoryChest2Healing.asset",

                    "RoR2/Base/CategoryChest/iscCategoryChestUtility.asset",
                    "RoR2/DLC1/CategoryChest2/iscCategoryChest2Utility.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/Chest1/iscChest1.asset",
                    "RoR2/Base/Chest2/iscChest2.asset",
                    "RoR2/Base/GoldChest/iscGoldChest.asset",
                    "RoR2/CommandChest/iscCommandChest.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/Duplicator/iscDuplicator.asset",
                    "RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset",
                    "RoR2/Base/DuplicatorMilitary/iscDuplicatorMilitary.asset",
                    "RoR2/Base/DuplicatorWild/iscDuplicatorWild.asset"
                ]),
                getEntrySingle("RoR2/Base/EquipmentBarrel/iscEquipmentBarrel.asset"),
                getEntrySingle("RoR2/Base/LunarChest/iscLunarChest.asset"),
                getEntrySingle("RoR2/Base/RadarTower/iscRadarTower.asset", 0.7f),
                getEntrySingle("RoR2/Base/Scrapper/iscScrapper.asset"),
                getEntryMany([
                    "RoR2/Base/ShrineBlood/iscShrineBlood.asset",
                    "RoR2/Base/ShrineBlood/iscShrineBloodSandy.asset",
                    "RoR2/Base/ShrineBlood/iscShrineBloodSnowy.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/ShrineBoss/iscShrineBoss.asset",
                    "RoR2/Base/ShrineBoss/iscShrineBossSandy.asset",
                    "RoR2/Base/ShrineBoss/iscShrineBossSnowy.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/ShrineChance/iscShrineChance.asset",
                    "RoR2/Base/ShrineChance/iscShrineChanceSandy.asset",
                    "RoR2/Base/ShrineChance/iscShrineChanceSnowy.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/ShrineCleanse/iscShrineCleanse.asset",
                    "RoR2/Base/ShrineCleanse/iscShrineCleanseSandy.asset",
                    "RoR2/Base/ShrineCleanse/iscShrineCleanseSnowy.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/ShrineCombat/iscShrineCombat.asset",
                    "RoR2/Base/ShrineCombat/iscShrineCombatSandy.asset",
                    "RoR2/Base/ShrineCombat/iscShrineCombatSnowy.asset"
                ]),
                getEntrySingle("RoR2/Base/ShrineGoldshoresAccess/iscShrineGoldshoresAccess.asset"),
                getEntrySingle("RoR2/Base/ShrineHealing/iscShrineHealing.asset"),
                getEntryMany([
                    "RoR2/Base/ShrineRestack/iscShrineRestack.asset",
                    "RoR2/Base/ShrineRestack/iscShrineRestackSandy.asset",
                    "RoR2/Base/ShrineRestack/iscShrineRestackSnowy.asset"
                ]),
                getEntryMany([
                    "RoR2/Base/TripleShop/iscTripleShop.asset",
                    "RoR2/Base/TripleShopEquipment/iscTripleShopEquipment.asset",
                    "RoR2/Base/TripleShopLarge/iscTripleShopLarge.asset"
                ]),
                getEntrySingle("RoR2/Base/goldshores/iscGoldshoresBeacon.asset", 0.8f),
                getEntrySingle("RoR2/DLC1/VoidChest/iscVoidChest.asset"),
                getEntrySingle("RoR2/DLC1/VoidSuppressor/iscVoidSuppressor.asset", 0.7f),
                getEntrySingle("RoR2/DLC1/VoidTriple/iscVoidTriple.asset"),
                getEntrySingle("RoR2/DLC1/FreeChest/iscFreeChest.asset"),
                getEntryMany([
                    "RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset",
                    "RoR2/Junk/TreasureCache/iscLockbox.asset"
                ]),
                new SpawnCardEntry(new InteractableSpawnCard[]
                {
                    createCauldronSpawnCard("RoR2/Base/LunarCauldrons/LunarCauldron, GreenToRed Variant.prefab"),
                    createCauldronSpawnCard("RoR2/Base/LunarCauldrons/LunarCauldron, RedToWhite Variant.prefab"),
                    createCauldronSpawnCard("RoR2/Base/LunarCauldrons/LunarCauldron, WhiteToGreen.prefab")
                }, 1f),
                new SpawnCardEntry(iscNewtStatue, 1f),
                getEntrySingle("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset"),
                getEntrySingle("RoR2/DLC2/iscShrineHalcyonite.asset"),
                getEntrySingle("RoR2/DLC2/iscShrineColossusAccess.asset"),
                new SpawnCardEntry(CustomSpawnCards.iscGeodeFixed, 1f),
            ];
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnCards);
        }

        public override void OnStart()
        {
            InteractableSpawnCard spawnCard = getItemToSpawn(_spawnCards, RNG.Branch());

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                spawnInteractable(spawnCard, playerBody, RNG.Branch());
            }
        }

        static void spawnInteractable(InteractableSpawnCard spawnCard, CharacterBody playerBody, Xoroshiro128Plus rng)
        {
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                position = playerBody.footPosition,
                minDistance = 0f,
                maxDistance = float.PositiveInfinity,
                placementMode = SpawnUtils.ExtraPlacementModes.NearestNodeWithConditions
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng.Branch());

            GameObject spawnedObject = spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
            if (spawnedObject)
            {
                Run run = Run.instance;
                Stage stage = Stage.instance;
                if (run && stage)
                {
                    if (spawnedObject.TryGetComponent(out PurchaseInteraction purchaseInteraction) &&
                        purchaseInteraction.costType == CostTypeIndex.Money &&
                        !purchaseInteraction.automaticallyScaleCostWithDifficulty)
                    {
                        purchaseInteraction.Networkcost = run.GetDifficultyScaledCost(purchaseInteraction.cost, stage.entryDifficultyCoefficient);
                    }
                }

                if (Configs.EffectSelection.SeededEffectSelection.Value)
                {
                    RNGOverridePatch.OverrideRNG(spawnedObject, rng.Branch());
                }
            }
        }
    }
}
