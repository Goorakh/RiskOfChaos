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

            List<DotController.DotStack> dotStackList = controller.dotStackList;

            for (int i = dotStackList.Count - 1; i >= 0; i--)
            {
                if (dotStackList[i].dotIndex == dotIndex)
                {
                    controller.RemoveDotStackAtServer(i);

                    if (--stacks <= 0)
                    {
                        return;
                    }
                }
            }
        }
    }
}
