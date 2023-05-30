using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_interactable")]
    public sealed class SpawnRandomInteractable : GenericDirectorSpawnEffect<InteractableSpawnCard>
    {
        static SpawnCardEntry[] _spawnCards;

        [SystemInitializer]
        static void Init()
        {
            static InteractableSpawnCard loadSpawnCard(string path)
            {
                InteractableSpawnCard isc = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();

                // Make sure it can always be spawned
                if (isc.skipSpawnWhenSacrificeArtifactEnabled)
                {
                    isc = ScriptableObject.Instantiate(isc);
                    isc.skipSpawnWhenSacrificeArtifactEnabled = false;
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

            _spawnCards = new SpawnCardEntry[]
            {
                getEntryMany(new string[]
                {
                    "RoR2/Base/Drones/iscBrokenDrone1.asset",
                    "RoR2/Base/Drones/iscBrokenDrone2.asset",
                    "RoR2/Base/Drones/iscBrokenEmergencyDrone.asset",
                    "RoR2/Base/Drones/iscBrokenEquipmentDrone.asset",
                    "RoR2/Base/Drones/iscBrokenFlameDrone.asset",
                    "RoR2/Base/Drones/iscBrokenMegaDrone.asset",
                    "RoR2/Base/Drones/iscBrokenMissileDrone.asset",
                    "RoR2/Base/Drones/iscBrokenTurret1.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/Barrel1/iscBarrel1.asset",
                    "RoR2/DLC1/VoidCoinBarrel/iscVoidCoinBarrel.asset"
                }),
                getEntrySingle("RoR2/Base/CasinoChest/iscCasinoChest.asset"),
                getEntryMany(new string[]
                {
                    "RoR2/Base/CategoryChest/iscCategoryChestDamage.asset",
                    "RoR2/Base/CategoryChest/iscCategoryChestHealing.asset",
                    "RoR2/Base/CategoryChest/iscCategoryChestUtility.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/Chest1/iscChest1.asset",
                    "RoR2/Base/Chest2/iscChest2.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/Duplicator/iscDuplicator.asset",
                    "RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset",
                    "RoR2/Base/DuplicatorMilitary/iscDuplicatorMilitary.asset",
                    "RoR2/Base/DuplicatorWild/iscDuplicatorWild.asset"
                }),
                getEntrySingle("RoR2/Base/EquipmentBarrel/iscEquipmentBarrel.asset"),
                getEntrySingle("RoR2/Base/GoldChest/iscGoldChest.asset"),
                getEntrySingle("RoR2/Base/LunarChest/iscLunarChest.asset"),
                getEntrySingle("RoR2/Base/RadarTower/iscRadarTower.asset"),
                getEntrySingle("RoR2/Base/Scrapper/iscScrapper.asset"),
                getEntryMany(new string[]
                {
                    "RoR2/Base/ShrineBlood/iscShrineBlood.asset",
                    "RoR2/Base/ShrineBlood/iscShrineBloodSandy.asset",
                    "RoR2/Base/ShrineBlood/iscShrineBloodSnowy.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/ShrineBoss/iscShrineBoss.asset",
                    "RoR2/Base/ShrineBoss/iscShrineBossSandy.asset",
                    "RoR2/Base/ShrineBoss/iscShrineBossSnowy.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/ShrineChance/iscShrineChance.asset",
                    "RoR2/Base/ShrineChance/iscShrineChanceSandy.asset",
                    "RoR2/Base/ShrineChance/iscShrineChanceSnowy.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/ShrineCleanse/iscShrineCleanse.asset",
                    "RoR2/Base/ShrineCleanse/iscShrineCleanseSandy.asset",
                    "RoR2/Base/ShrineCleanse/iscShrineCleanseSnowy.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/ShrineCombat/iscShrineCombat.asset",
                    "RoR2/Base/ShrineCombat/iscShrineCombatSandy.asset",
                    "RoR2/Base/ShrineCombat/iscShrineCombatSnowy.asset"
                }),
                getEntrySingle("RoR2/Base/ShrineGoldshoresAccess/iscShrineGoldshoresAccess.asset"),
                getEntrySingle("RoR2/Base/ShrineHealing/iscShrineHealing.asset"),
                getEntryMany(new string[]
                {
                    "RoR2/Base/ShrineRestack/iscShrineRestack.asset",
                    "RoR2/Base/ShrineRestack/iscShrineRestackSandy.asset",
                    "RoR2/Base/ShrineRestack/iscShrineRestackSnowy.asset"
                }),
                getEntryMany(new string[]
                {
                    "RoR2/Base/TripleShop/iscTripleShop.asset",
                    "RoR2/Base/TripleShopEquipment/iscTripleShopEquipment.asset",
                    "RoR2/Base/TripleShopLarge/iscTripleShopLarge.asset"
                }),
                getEntrySingle("RoR2/Base/goldshores/iscGoldshoresBeacon.asset"),
                getEntryMany(new string[]
                {
                    "RoR2/DLC1/CategoryChest2/iscCategoryChest2Damage.asset",
                    "RoR2/DLC1/CategoryChest2/iscCategoryChest2Healing.asset",
                    "RoR2/DLC1/CategoryChest2/iscCategoryChest2Utility.asset"
                }),
                getEntrySingle("RoR2/DLC1/VoidChest/iscVoidChest.asset"),
                getEntrySingle("RoR2/DLC1/VoidSuppressor/iscVoidSuppressor.asset"),
                getEntrySingle("RoR2/DLC1/VoidTriple/iscVoidTriple.asset"),
                getEntrySingle("RoR2/DLC1/FreeChest/iscFreeChest.asset"),
                getEntryMany(new string[]
                {
                    "RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset",
                    "RoR2/Junk/TreasureCache/iscLockbox.asset"
                })
            };
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return areAnyAvailable(_spawnCards);
        }

        public override void OnStart()
        {
            InteractableSpawnCard spawnCard = getItemToSpawn(_spawnCards, RNG);

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    position = playerBody.footPosition,
                    placementMode = DirectorPlacementRule.PlacementMode.NearestNode
                };

                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, new Xoroshiro128Plus(RNG.nextUlong));

                if (!DirectorCore.instance.TrySpawnObject(spawnRequest))
                {
                    spawnRequest.placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                    DirectorCore.instance.TrySpawnObject(spawnRequest);
                }
            }
        }
    }
}
