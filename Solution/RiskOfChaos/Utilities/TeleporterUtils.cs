using RiskOfChaos.Components;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class TeleporterUtils
    {
        public static List<GameObject> GetActiveTeleporterObjects()
        {
            List<GameObject> teleporterObjects = [];

            if (TeleporterInteraction.instance)
            {
                teleporterObjects.Add(TeleporterInteraction.instance.gameObject);
            }

            foreach (FakeTeleporterInteraction fakeTeleporterInteraction in InstanceTracker.GetInstancesList<FakeTeleporterInteraction>())
            {
                if (fakeTeleporterInteraction)
                {
                    teleporterObjects.Add(fakeTeleporterInteraction.gameObject);
                }
            }

            return teleporterObjects;
        }
    }
}
