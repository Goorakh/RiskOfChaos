using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.DropTables;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_random_item")]
    public sealed class GiveRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _itemCount =
            ConfigFactory<int>.CreateConfig("Item Count", 1)
                              .Description("The amount of items to give to each player per effect activation")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1})
                              .Build();

        [EffectConfig]
        static readonly ConfigurableDropTable _dropTable;

        static GiveRandomItem()
        {
            _dropTable = ScriptableObject.CreateInstance<ConfigurableDropTable>();
            _dropTable.name = $"dt{nameof(GiveRandomItem)}";
            _dropTable.canDropBeReplaced = false;

            _dropTable.RegisterDrop(DropType.Tier1, 0.75f);
            _dropTable.RegisterDrop(DropType.Tier2, 0.6f);
            _dropTable.RegisterDrop(DropType.Tier3, 0.3f);
            _dropTable.RegisterDrop(DropType.Boss, 0.5f);
            _dropTable.RegisterDrop(DropType.LunarEquipment, 0.15f);
            _dropTable.RegisterDrop(DropType.LunarItem, 0.35f);
            _dropTable.RegisterDrop(DropType.Equipment, 0.25f);
            _dropTable.RegisterDrop(DropType.VoidTier1, 0.4f);
            _dropTable.RegisterDrop(DropType.VoidTier2, 0.25f);
            _dropTable.RegisterDrop(DropType.VoidTier3, 0.3f);
            _dropTable.RegisterDrop(DropType.VoidBoss, 0.3f);

            _dropTable.CreateItemBlacklistConfig("Item Blacklist", "A comma-separated list of items and equipment that should not be included for the effect. Both internal and English display names are accepted, with spaces and commas removed.");

            _dropTable.AddDrops += (List<ExplicitDrop> drops) =>
            {
                drops.Add(new ExplicitDrop(RoR2Content.Items.ArtifactKey.itemIndex, DropType.Boss, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.CaptainDefenseMatrix.itemIndex, DropType.Tier3, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.Pearl.itemIndex, DropType.Boss, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.ShinyPearl.itemIndex, DropType.Boss, null));

                drops.Add(new ExplicitDrop(RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex, DropType.Equipment, null));

                drops.Add(new ExplicitDrop(DLC1Content.Equipment.BossHunterConsumed.equipmentIndex, DropType.Equipment, ExpansionUtils.DLC1));
                drops.Add(new ExplicitDrop(DLC1Content.Equipment.LunarPortalOnUse.equipmentIndex, DropType.Equipment, ExpansionUtils.DLC1));

                drops.Add(new ExplicitDrop(DLC2Content.Equipment.HealAndReviveConsumed.equipmentIndex, DropType.Equipment, ExpansionUtils.DLC2));
            };
        }

        ChaosEffectComponent _effectComponent;

        PickupDef[] _pickupsToGive;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
            
            _dropTable.RegenerateIfNeeded();

            _pickupsToGive = new PickupDef[_itemCount.Value];
            for (int i = 0; i < _pickupsToGive.Length; i++)
            {
                _pickupsToGive[i] = PickupCatalog.GetPickupDef(_dropTable.GenerateDrop(rng));
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
                {
                    HashSet<PickupIndex> givenPickups = new HashSet<PickupIndex>(_pickupsToGive.Length);

                    foreach (PickupDef pickupDef in _pickupsToGive)
                    {
                        if (playerMaster.inventory.TryGrant(pickupDef, InventoryExtensions.EquipmentReplacementRule.DropExisting))
                        {
                            givenPickups.Add(pickupDef.pickupIndex);
                        }
                    }

                    if (givenPickups.Count > 0)
                    {
                        PickupUtils.QueuePickupsMessage(playerMaster, [.. givenPickups]);
                    }
                }, Util.GetBestMasterName);
            }
        }
    }
}
