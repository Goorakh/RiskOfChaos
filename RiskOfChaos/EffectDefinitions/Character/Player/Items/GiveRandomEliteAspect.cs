using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("GiveRandomEliteAspect", DefaultSelectionWeight = 0.6f)]
    public class GiveRandomEliteAspect : BaseEffect
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

            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(true))
            {
                PickupUtils.GrantOrDropPickupAt(aspectPickupDef, playerMaster);
            }
        }
    }
}
