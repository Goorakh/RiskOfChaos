using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class DotControllerExtensions
    {
        public static bool HasDOT(this GameObject victimObject, DotController.DotIndex dotIndex)
        {
            DotController dotController = DotController.FindDotController(victimObject);
            return dotController && dotController.HasDotActive(dotIndex);
        }

        public static void RemoveDOTStacks(this DotController controller, DotController.DotIndex dotIndex, int stacks)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (stacks <= 0)
                return;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            List<DotController.DotStack> dotStackList = controller.dotStackList;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            for (int i = dotStackList.Count - 1; i >= 0; i--)
            {
                if (dotStackList[i].dotIndex == dotIndex)
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    controller.RemoveDotStackAtServer(i);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                    if (--stacks <= 0)
                    {
                        return;
                    }
                }
            }
        }
    }
}
