using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_random_elite_aspect", DefaultSelectionWeight = 0.6f)]
    public sealed class GiveRandomEliteAspect : BaseEffect
    {
        static EquipmentDef[] _eliteAspects;

        [SystemInitializer(typeof(EliteCatalog))]
        static void Init()
        {
            HashSet<EquipmentDef> eliteAspects = new HashSet<EquipmentDef>();

            foreach (EliteIndex eliteIndex in EliteCatalog.eliteList)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (!eliteDef)
                    continue;

                EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
                if (!eliteEquipmentDef)
                    continue;

                if (!eliteEquipmentDef.pickupModelPrefab || eliteEquipmentDef.pickupModelPrefab.name == "NullModel")
                    continue;

                if (eliteEquipmentDef.dropOnDeathChance <= 0f)
                    continue;

                eliteAspects.Add(eliteEquipmentDef);
            }

            _eliteAspects = eliteAspects.ToArray();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _eliteAspects != null && _eliteAspects.Length > 0;
        }

        public override void OnStart()
        {
            PickupDef aspectPickupDef = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(RNG.NextElementUniform(_eliteAspects).equipmentIndex));

            PlayerUtils.GetAllPlayerMasters(true).TryDo(playerMaster =>
            {
                PickupUtils.GrantOrDropPickupAt(aspectPickupDef, playerMaster);
            }, Util.GetBestMasterName);
        }
    }
}
