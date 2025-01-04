using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class PlayerUtils
    {
        public static IEnumerable<CharacterMaster> GetAllPlayerMasters(bool requireAlive)
        {
            return PlayerCharacterMasterController.instances.Where(p => p)
                                                            .Select(p => p.master)
                                                            .Where(m => m && (!requireAlive || m.IsAlive()));
        }

        public static IEnumerable<CharacterBody> GetAllPlayerBodies(bool requireAlive)
        {
            return GetAllPlayerMasters(requireAlive).Select(m => m.GetBody())
                                                    .Where(b => b);
        }

        public static bool AnyPlayerInRadius(Vector3 position, float radius)
        {
            float sqrRadius = radius * radius;
            
            foreach (PlayerCharacterMasterController playerMasterController in PlayerCharacterMasterController.instances)
            {
                if (playerMasterController.isConnected)
                {
                    CharacterMaster playerMaster = playerMasterController.master;
                    if (playerMaster)
                    {
                        CharacterBody body = playerMaster.GetBody();
                        if (body && body.healthComponent && body.healthComponent.alive)
                        {
                            if ((body.corePosition - position).sqrMagnitude <= sqrRadius)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
