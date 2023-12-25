using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities
{
    public static class PlayerUtils
    {
        public static IEnumerable<CharacterMaster> GetAllPlayerMasters(bool requireAlive)
        {
            return from playerMasterController in PlayerCharacterMasterController.instances
                   where playerMasterController
                   let playerMaster = playerMasterController.master
                   where playerMaster && (!requireAlive || playerMaster.IsAlive())
                   select playerMaster;
        }

        public static IEnumerable<CharacterBody> GetAllPlayerBodies(bool requireAlive)
        {
            return from playerMaster in GetAllPlayerMasters(requireAlive)
                   let playerBody = playerMaster.GetBody()
                   where playerBody
                   select playerBody;
        }
    }
}
