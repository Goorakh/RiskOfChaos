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

            static SpawnPool<InteractableSpawnCard>.Entry loadSpawnCardEntry(string assetGuid, SpawnPoolEntryParameters parameters)
            {
                return _spawnPool.LoadEntry(assetGuid, parameters, ensureUnrestrictedSpawn);
            }

            static SpawnPool<InteractableSpawnCard>.Entry loadCauldronSpawnCardEntry(string assetGuid, SpawnPoolEntryParameters parameters)
            {
                return _spawnPool.LoadEntry<GameObject>(assetGuid, parameters, cauldronPrefab =>
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
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenDrone1_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenDrone2_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenEmergencyDrone_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenEquipmentDrone_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenFlameDrone_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenMegaDrone_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenMissileDrone_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Drones_iscBrokenTurret1_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_CU8_LemurianEgg_iscLemurianEgg_asset, new SpawnPoolEntryParameters(1f)),
            ], 1f);

            // Barrels
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Barrel1_iscBarrel1_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_VoidCoinBarrel_iscVoidCoinBarrel_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),
            ]);

            // Chests
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Chest1_iscChest1_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Chest2_iscChest2_asset, new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry(AddressableGuids.RoR2_Base_EquipmentBarrel_iscEquipmentBarrel_asset, new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry(AddressableGuids.RoR2_Base_LunarChest_iscLunarChest_asset, new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry(AddressableGuids.RoR2_Base_GoldChest_iscGoldChest_asset, new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry(AddressableGuids.RoR2_Base_CategoryChest_iscCategoryChestDamage_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_CategoryChest2_iscCategoryChest2Damage_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry(AddressableGuids.RoR2_Base_CategoryChest_iscCategoryChestHealing_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_CategoryChest2_iscCategoryChest2Healing_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry(AddressableGuids.RoR2_Base_CategoryChest_iscCategoryChestUtility_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_CategoryChest2_iscCategoryChest2Utility_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry(AddressableGuids.RoR2_Base_CasinoChest_iscCasinoChest_asset, new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_VoidChest_iscVoidChest_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry(AddressableGuids.RoR2_CommandChest_iscCommandChest_asset, new SpawnPoolEntryParameters(1f)),

                new SpawnPool<InteractableSpawnCard>.Entry(CustomSpawnCards.iscTimedChest, new SpawnPoolEntryParameters(1f)),
            ], 2f);

            // Multishops
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_TripleShop_iscTripleShop_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_TripleShopEquipment_iscTripleShopEquipment_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_TripleShopLarge_iscTripleShopLarge_asset, new SpawnPoolEntryParameters(1f)),

                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_FreeChest_iscFreeChest_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),

                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_VoidTriple_iscVoidTriple_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),
            ], 1.5f);

            // Printers
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_Duplicator_iscDuplicator_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_DuplicatorLarge_iscDuplicatorLarge_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_DuplicatorMilitary_iscDuplicatorMilitary_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_DuplicatorWild_iscDuplicatorWild_asset, new SpawnPoolEntryParameters(1f)),
            ], 1.5f);

            // Lockboxes
            _spawnPool.AddGroupedEntries([
                loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_TreasureCacheVoid_iscLockboxVoid_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),
                loadSpawnCardEntry(AddressableGuids.RoR2_Junk_TreasureCache_iscLockbox_asset, new SpawnPoolEntryParameters(1f)),
            ], 1f);

            // Cauldrons
            _spawnPool.AddGroupedEntries([
                loadCauldronSpawnCardEntry(AddressableGuids.RoR2_Base_LunarCauldrons_LunarCauldron_GreenToRed_Variant_prefab, new SpawnPoolEntryParameters(1f)),
                loadCauldronSpawnCardEntry(AddressableGuids.RoR2_Base_LunarCauldrons_LunarCauldron_RedToWhite_Variant_prefab, new SpawnPoolEntryParameters(1f)),
                loadCauldronSpawnCardEntry(AddressableGuids.RoR2_Base_LunarCauldrons_LunarCauldron_WhiteToGreen_prefab, new SpawnPoolEntryParameters(1f)),
            ], 1.5f);

            // Shrines
            _spawnPool.AddGroupedEntries([
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineBlood_iscShrineBlood_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineBlood_iscShrineBloodSandy_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineBlood_iscShrineBloodSnowy_asset, new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineBoss_iscShrineBoss_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineBoss_iscShrineBossSandy_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineBoss_iscShrineBossSnowy_asset, new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineChance_iscShrineChance_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineChance_iscShrineChanceSandy_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineChance_iscShrineChanceSnowy_asset, new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineCleanse_iscShrineCleanse_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineCleanse_iscShrineCleanseSandy_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineCleanse_iscShrineCleanseSnowy_asset, new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineCombat_iscShrineCombat_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineCombat_iscShrineCombatSandy_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineCombat_iscShrineCombatSnowy_asset, new SpawnPoolEntryParameters(1f)),
                ]),
                .. _spawnPool.GroupEntries([
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineRestack_iscShrineRestack_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineRestack_iscShrineRestackSandy_asset, new SpawnPoolEntryParameters(1f)),
                    loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineRestack_iscShrineRestackSnowy_asset, new SpawnPoolEntryParameters(1f)),
                ]),
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineHealing_iscShrineHealing_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_DLC2_iscShrineColossusAccess_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
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
                loadSpawnCardEntry(AddressableGuids.RoR2_Base_ShrineGoldshoresAccess_iscShrineGoldshoresAccess_asset, new SpawnPoolEntryParameters(1f)),
                loadSpawnCardEntry(AddressableGuids.RoR2_DLC2_iscShrineHalcyonite_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
                new SpawnPool<InteractableSpawnCard>.Entry(iscNewtStatue, new SpawnPoolEntryParameters(1f)),
            ]);

            // Misc
            _spawnPool.AddEntry(loadSpawnCardEntry(AddressableGuids.RoR2_Base_RadarTower_iscRadarTower_asset, new SpawnPoolEntryParameters(0.7f)));
            _spawnPool.AddEntry(loadSpawnCardEntry(AddressableGuids.RoR2_Base_goldshores_iscGoldshoresBeacon_asset, new SpawnPoolEntryParameters(0.8f)));
            _spawnPool.AddEntry(loadSpawnCardEntry(AddressableGuids.RoR2_Base_Scrapper_iscScrapper_asset, new SpawnPoolEntryParameters(1f)));
            _spawnPool.AddEntry(loadSpawnCardEntry(AddressableGuids.RoR2_DLC1_VoidSuppressor_iscVoidSuppressor_asset, new SpawnPoolEntryParameters(0.7f, ExpansionUtils.DLC1)));

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
