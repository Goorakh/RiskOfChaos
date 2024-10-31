using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World.Spawn;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_interactable")]
    public sealed class SpawnRandomInteractable : NetworkBehaviour
    {
        static readonly SpawnPool<InteractableSpawnCard> _spawnPool = new SpawnPool<InteractableSpawnCard>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.InteractableSpawnCardExpansionsProvider
        };

        [SystemInitializer(typeof(CustomSpawnCards))]
        static void Init()
        {
            static InteractableSpawnCard ensureUnrestrictedSpawn(InteractableSpawnCard spawnCard)
            {
                if (spawnCard.skipSpawnWhenSacrificeArtifactEnabled || spawnCard.skipSpawnWhenDevotionArtifactEnabled)
                {
                    string name = spawnCard.name;
                    spawnCard = Instantiate(spawnCard);
                    spawnCard.name = $"{name}_UnrestrictedSpawn";
                    spawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
                    spawnCard.skipSpawnWhenDevotionArtifactEnabled = false;
                }

                return spawnCard;
            }

            static SpawnPool<InteractableSpawnCard>.Entry loadSpawnCardEntry(string assetPath, float weight)
            {
                return _spawnPool.LoadEntry(assetPath, weight, ensureUnrestrictedSpawn);
            }

            static SpawnPool<InteractableSpawnCard>.Entry loadCauldronSpawnCardEntry(string assetPath, float weight)
            {
                return _spawnPool.LoadEntry<GameObject>(assetPath, weight, cauldronPrefab =>
                {
                    InteractableSpawnCard spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
                    spawnCard.name = $"isc{cauldronPrefab.name}";
                    spawnCard.prefab = cauldronPrefab;
                    spawnCard.orientToFloor = true;
                    spawnCard.hullSize = HullClassification.Golem;
                    spawnCard.requiredFlags = NodeFlags.None;
                    spawnCard.forbiddenFlags = NodeFlags.NoChestSpawn;
                    spawnCard.occupyPosition = true;
                    spawnCard.sendOverNetwork = true;

                    return spawnCard;
                });
            }

            _spawnPool.EnsureCapacity(75);

            // Drones
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenDrone1.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenDrone2.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenEmergencyDrone.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenEquipmentDrone.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenFlameDrone.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenMegaDrone.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenMissileDrone.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenTurret1.asset", 1f),
                loadSpawnCardEntry("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset", 1f),
            ], 1f);

            // Barrels
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/Barrel1/iscBarrel1.asset", 1f),
                loadSpawnCardEntry("RoR2/DLC1/VoidCoinBarrel/iscVoidCoinBarrel.asset", 1f),
            ]);

            // Chests
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/Chest1/iscChest1.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/Chest2/iscChest2.asset", 1f),

                loadSpawnCardEntry("RoR2/Base/EquipmentBarrel/iscEquipmentBarrel.asset", 1f),

                loadSpawnCardEntry("RoR2/Base/LunarChest/iscLunarChest.asset", 1f),

                loadSpawnCardEntry("RoR2/Base/GoldChest/iscGoldChest.asset", 1f),

                loadSpawnCardEntry("RoR2/Base/CategoryChest/iscCategoryChestDamage.asset", 1f),
                loadSpawnCardEntry("RoR2/DLC1/CategoryChest2/iscCategoryChest2Damage.asset", 1f),

                loadSpawnCardEntry("RoR2/Base/CategoryChest/iscCategoryChestHealing.asset", 1f),
                loadSpawnCardEntry("RoR2/DLC1/CategoryChest2/iscCategoryChest2Healing.asset", 1f),

                loadSpawnCardEntry("RoR2/Base/CategoryChest/iscCategoryChestUtility.asset", 1f),
                loadSpawnCardEntry("RoR2/DLC1/CategoryChest2/iscCategoryChest2Utility.asset", 1f),

                loadSpawnCardEntry("RoR2/Base/CasinoChest/iscCasinoChest.asset", 1f),

                loadSpawnCardEntry("RoR2/DLC1/VoidChest/iscVoidChest.asset", 1f),

                loadSpawnCardEntry("RoR2/CommandChest/iscCommandChest.asset", 1f),
            ], 2f);

            // Multishops
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/TripleShop/iscTripleShop.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/TripleShopEquipment/iscTripleShopEquipment.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/TripleShopLarge/iscTripleShopLarge.asset", 1f),

                loadSpawnCardEntry("RoR2/DLC1/FreeChest/iscFreeChest.asset", 1f),

                loadSpawnCardEntry("RoR2/DLC1/VoidTriple/iscVoidTriple.asset", 1f),
            ], 1.5f);

            // Printers
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/Duplicator/iscDuplicator.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/DuplicatorMilitary/iscDuplicatorMilitary.asset", 1f),
                loadSpawnCardEntry("RoR2/Base/DuplicatorWild/iscDuplicatorWild.asset", 1f),
            ], 1.5f);

            // Lockboxes
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset", 1f),
                loadSpawnCardEntry("RoR2/Junk/TreasureCache/iscLockbox.asset", 1f),
            ], 1f);

            // Cauldrons
            _spawnPool.AddGroupedEntries([
                loadCauldronSpawnCardEntry("RoR2/Base/LunarCauldrons/LunarCauldron, GreenToRed Variant.prefab", 1f),
                loadCauldronSpawnCardEntry("RoR2/Base/LunarCauldrons/LunarCauldron, RedToWhite Variant.prefab", 1f),
                loadCauldronSpawnCardEntry("RoR2/Base/LunarCauldrons/LunarCauldron, WhiteToGreen.prefab", 1f),
            ], 1.5f);

            // Shrines
            _spawnPool.AddGroupedEntries([
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineBlood/iscShrineBlood.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineBlood/iscShrineBloodSandy.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineBlood/iscShrineBloodSnowy.asset", 1f),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineBoss/iscShrineBoss.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineBoss/iscShrineBossSandy.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineBoss/iscShrineBossSnowy.asset", 1f),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineChance/iscShrineChance.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineChance/iscShrineChanceSandy.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineChance/iscShrineChanceSnowy.asset", 1f),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineCleanse/iscShrineCleanse.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineCleanse/iscShrineCleanseSandy.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineCleanse/iscShrineCleanseSnowy.asset", 1f),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineCombat/iscShrineCombat.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineCombat/iscShrineCombatSandy.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineCombat/iscShrineCombatSnowy.asset", 1f),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineRestack/iscShrineRestack.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineRestack/iscShrineRestackSandy.asset", 1f),
                    loadSpawnCardEntry("RoR2/Base/ShrineRestack/iscShrineRestackSnowy.asset", 1f),
                ]),
                loadSpawnCardEntry("RoR2/Base/ShrineHealing/iscShrineHealing.asset", 1f),
                loadSpawnCardEntry("RoR2/DLC2/iscShrineColossusAccess.asset", 1f),
            ]);

            InteractableSpawnCard iscNewtStatue = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            iscNewtStatue.name = "iscNewtStatue";
            iscNewtStatue.prefab = RoCContent.NetworkedPrefabs.NewtStatueFixedOrigin;
            iscNewtStatue.orientToFloor = false;
            iscNewtStatue.hullSize = HullClassification.Golem;
            iscNewtStatue.requiredFlags = NodeFlags.None;
            iscNewtStatue.forbiddenFlags = NodeFlags.NoChestSpawn;
            iscNewtStatue.occupyPosition = true;
            iscNewtStatue.sendOverNetwork = true;

            // Portal
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/ShrineGoldshoresAccess/iscShrineGoldshoresAccess.asset", 1f),
                loadSpawnCardEntry("RoR2/DLC2/iscShrineHalcyonite.asset", 1f),
                new SpawnPool<InteractableSpawnCard>.Entry(iscNewtStatue, 1f),
            ]);

            // Misc
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/Base/RadarTower/iscRadarTower.asset", 0.7f));
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/Base/goldshores/iscGoldshoresBeacon.asset", 0.8f));
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/Base/Scrapper/iscScrapper.asset", 1f));
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/DLC1/VoidSuppressor/iscVoidSuppressor.asset", 0.7f));
            _spawnPool.AddEntry(new SpawnPool<InteractableSpawnCard>.Entry(CustomSpawnCards.iscGeodeFixed, 1f));

            _spawnPool.TrimExcess();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        InteractableSpawnCard _selectedSpawnCard;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _selectedSpawnCard = _spawnPool.PickRandomEntry(_rng);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                if (!master.TryGetBodyPosition(out Vector3 bodyPosition))
                    continue;

                spawnInteractable(_selectedSpawnCard, bodyPosition, _rng);
            }
        }

        static void spawnInteractable(InteractableSpawnCard spawnCard, Vector3 approximatePosition, Xoroshiro128Plus rng)
        {
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                position = approximatePosition,
                placementMode = SpawnUtils.ExtraPlacementModes.NearestNodeWithConditions,
                minDistance = 0f,
                maxDistance = float.PositiveInfinity,
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

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
                    RNGOverridePatch.OverrideRNG(spawnedObject, new Xoroshiro128Plus(rng.nextUlong));
                }
            }
        }
    }
}
