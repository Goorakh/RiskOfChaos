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

        [SystemInitializer(typeof(CustomSpawnCards), typeof(ExpansionUtils))]
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

            static SpawnPool<InteractableSpawnCard>.Entry loadSpawnCardEntry(string assetPath, SpawnPoolEntryParameters parameters)
            {
                return _spawnPool.LoadEntry(assetPath, parameters, ensureUnrestrictedSpawn);
            }

            static SpawnPool<InteractableSpawnCard>.Entry loadCauldronSpawnCardEntry(string assetPath, SpawnPoolEntryParameters parameters)
            {
                return _spawnPool.LoadEntry<GameObject>(assetPath, parameters, cauldronPrefab =>
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
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenDrone1.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenDrone2.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenEmergencyDrone.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenEquipmentDrone.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenFlameDrone.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenMegaDrone.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenMissileDrone.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Drones/iscBrokenTurret1.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/CU8/LemurianEgg/iscLemurianEgg.asset", new SpawnPoolEntryParameters(1f)),
            ], 1f);

            // Barrels
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/Barrel1/iscBarrel1.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/DLC1/VoidCoinBarrel/iscVoidCoinBarrel.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),
            ]);

            // Chests
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/Chest1/iscChest1.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/Chest2/iscChest2.asset", new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry("RoR2/Base/EquipmentBarrel/iscEquipmentBarrel.asset", new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry("RoR2/Base/LunarChest/iscLunarChest.asset", new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry("RoR2/Base/GoldChest/iscGoldChest.asset", new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry("RoR2/Base/CategoryChest/iscCategoryChestDamage.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/DLC1/CategoryChest2/iscCategoryChest2Damage.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry("RoR2/Base/CategoryChest/iscCategoryChestHealing.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/DLC1/CategoryChest2/iscCategoryChest2Healing.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry("RoR2/Base/CategoryChest/iscCategoryChestUtility.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/DLC1/CategoryChest2/iscCategoryChest2Utility.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry("RoR2/Base/CasinoChest/iscCasinoChest.asset", new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry("RoR2/DLC1/VoidChest/iscVoidChest.asset", new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry("RoR2/CommandChest/iscCommandChest.asset", new SpawnPoolEntryParameters(1f)),
            ], 2f);

            // Multishops
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/TripleShop/iscTripleShop.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/TripleShopEquipment/iscTripleShopEquipment.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/TripleShopLarge/iscTripleShopLarge.asset", new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry("RoR2/DLC1/FreeChest/iscFreeChest.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry("RoR2/DLC1/VoidTriple/iscVoidTriple.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),
            ], 1.5f);

            // Printers
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/Base/Duplicator/iscDuplicator.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/DuplicatorMilitary/iscDuplicatorMilitary.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/Base/DuplicatorWild/iscDuplicatorWild.asset", new SpawnPoolEntryParameters(1f)),
            ], 1.5f);

            // Lockboxes
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry("RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),
                loadSpawnCardEntry("RoR2/Junk/TreasureCache/iscLockbox.asset", new SpawnPoolEntryParameters(1f)),
            ], 1f);

            // Cauldrons
            _spawnPool.AddGroupedEntries([
                loadCauldronSpawnCardEntry("RoR2/Base/LunarCauldrons/LunarCauldron, GreenToRed Variant.prefab", new SpawnPoolEntryParameters(1f)),
                loadCauldronSpawnCardEntry("RoR2/Base/LunarCauldrons/LunarCauldron, RedToWhite Variant.prefab", new SpawnPoolEntryParameters(1f)),
                loadCauldronSpawnCardEntry("RoR2/Base/LunarCauldrons/LunarCauldron, WhiteToGreen.prefab", new SpawnPoolEntryParameters(1f)),
            ], 1.5f);

            // Shrines
            _spawnPool.AddGroupedEntries([
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineBlood/iscShrineBlood.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineBlood/iscShrineBloodSandy.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineBlood/iscShrineBloodSnowy.asset", new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineBoss/iscShrineBoss.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineBoss/iscShrineBossSandy.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineBoss/iscShrineBossSnowy.asset", new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineChance/iscShrineChance.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineChance/iscShrineChanceSandy.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineChance/iscShrineChanceSnowy.asset", new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineCleanse/iscShrineCleanse.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineCleanse/iscShrineCleanseSandy.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineCleanse/iscShrineCleanseSnowy.asset", new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineCombat/iscShrineCombat.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineCombat/iscShrineCombatSandy.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineCombat/iscShrineCombatSnowy.asset", new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry("RoR2/Base/ShrineRestack/iscShrineRestack.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineRestack/iscShrineRestackSandy.asset", new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry("RoR2/Base/ShrineRestack/iscShrineRestackSnowy.asset", new SpawnPoolEntryParameters(1f)),
                ]),
                loadSpawnCardEntry("RoR2/Base/ShrineHealing/iscShrineHealing.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/DLC2/iscShrineColossusAccess.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
                new SpawnPool<InteractableSpawnCard>.Entry(CustomSpawnCards.iscGeodeFixed, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
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
                loadSpawnCardEntry("RoR2/Base/ShrineGoldshoresAccess/iscShrineGoldshoresAccess.asset", new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry("RoR2/DLC2/iscShrineHalcyonite.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
                new SpawnPool<InteractableSpawnCard>.Entry(iscNewtStatue, new SpawnPoolEntryParameters(1f)),
            ]);

            // Misc
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/Base/RadarTower/iscRadarTower.asset", new SpawnPoolEntryParameters(0.7f)));
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/Base/goldshores/iscGoldshoresBeacon.asset", new SpawnPoolEntryParameters(0.8f)));
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/Base/Scrapper/iscScrapper.asset", new SpawnPoolEntryParameters(1f)));
            _spawnPool.AddEntry(loadSpawnCardEntry("RoR2/DLC1/VoidSuppressor/iscVoidSuppressor.asset", new SpawnPoolEntryParameters(0.7f, ExpansionUtils.DLC1)));

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
