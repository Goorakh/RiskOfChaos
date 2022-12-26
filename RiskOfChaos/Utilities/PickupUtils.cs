using RiskOfChaos.EffectDefinitions.Items;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Utilities
{
    public static class PickupUtils
    {
        public static void GrantOrDropPickupAt(PickupDef pickup, CharacterMaster master)
        {
            Inventory inventory = master.inventory;
            if (inventory && inventory.TryGrant(pickup))
            {
                GenericPickupController.SendPickupMessage(master, pickup.pickupIndex);
            }
            else
            {
                CharacterBody playerBody = master.GetBody();
                if (playerBody)
                {
                    GenericPickupController.CreatePickup(new GenericPickupController.CreatePickupInfo
                    {
                        pickupIndex = pickup.pickupIndex,
                        position = playerBody.footPosition
                    });
                }
                else
                {
                    Log.Warning($"unable to spawn pickup {pickup.pickupIndex} at {master}: Null body");
                }
            }
        }
    }
}
