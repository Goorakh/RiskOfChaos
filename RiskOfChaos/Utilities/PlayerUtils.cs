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
                   where playerMaster && (!requireAlive || !playerMaster.IsDeadAndOutOfLivesServer())
                   select playerMaster;
        }

        public static IEnumerable<CharacterBody> GetAllPlayerBodies(bool requireAlive)
        {
            return from playerMaster in GetAllPlayerMasters(requireAlive)
                   let playerBody = playerMaster.GetBody()
                   where playerBody
                   select playerBody;
        }

        public static CharacterMaster GetLocalUserMaster()
        {
            LocalUser localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser != null)
            {
                CharacterMaster localPlayerMaster = localUser.cachedMaster;
                if (localPlayerMaster)
                {
                    return localPlayerMaster;
                }
            }

            return null;
        }

        public static CharacterBody GetLocalUserBody()
        {
            CharacterMaster localPlayerMaster = GetLocalUserMaster();
            if (localPlayerMaster)
            {
                CharacterBody localUserBody = localPlayerMaster.GetBody();
                if (localUserBody)
                {
                    return localUserBody;
                }
            }

            return null;
        }

        public static Interactor GetLocalUserInteractor()
        {
            CharacterBody localUserBody = GetLocalUserBody();
            if (localUserBody)
            {
                return localUserBody.GetComponent<Interactor>();
            }
            else
            {
                return null;
            }
        }
    }
}
