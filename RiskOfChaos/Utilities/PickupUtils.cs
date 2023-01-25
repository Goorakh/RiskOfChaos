using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class PickupUtils
    {
        public static void GrantOrDropPickupAt(PickupDef pickup, CharacterMaster master, bool replaceExisting = true)
        {
            Inventory inventory = master.inventory;

            GenericPickupController createPickup(PickupIndex pickupIndex)
            {
                CharacterBody body = master.GetBody();
                if (body)
                {
                    return GenericPickupController.CreatePickup(new GenericPickupController.CreatePickupInfo
                    {
                        pickupIndex = pickupIndex,
                        position = body.footPosition
                    });
                }
                else
                {
                    Log.Warning($"unable to spawn pickup {pickupIndex} at {master}: Null body");
                    return null;
                }
            }

            PickupTryGrantResult tryGrantResult = inventory.TryGrant(pickup, replaceExisting);
            if (tryGrantResult.State != PickupTryGrantResult.ResultState.Failed)
            {
                GenericPickupController.SendPickupMessage(master, pickup.pickupIndex);
            }
            else
            {
                createPickup(pickup.pickupIndex);
            }

            PickupIndex pickupToSpawn = tryGrantResult.PickupToSpawn;
            if (pickupToSpawn.isValid)
            {
                createPickup(pickupToSpawn);
            }
        }
    }
}
