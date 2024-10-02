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
            return PlayerCharacterMasterController.instances.Where(p => p)
                                                            .Select(p => p.master)
                                                            .Where(m => m && (!requireAlive || m.IsAlive()));
        }

        public static IEnumerable<CharacterBody> GetAllPlayerBodies(bool requireAlive)
        {
            return GetAllPlayerMasters(requireAlive).Select(m => m.GetBody())
                                                    .Where(b => b);
        }
    }
}
