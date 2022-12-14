using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos
{
    public class InteractableTracker : MonoBehaviour
    {
        public InteractableSpawnCard SpawnCard { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.InteractableSpawnCard.Spawn += InteractableSpawnCard_Spawn;
        }

        static void InteractableSpawnCard_Spawn(On.RoR2.InteractableSpawnCard.orig_Spawn orig, InteractableSpawnCard self, Vector3 position, Quaternion rotation, DirectorSpawnRequest directorSpawnRequest, ref SpawnCard.SpawnResult result)
        {
            orig(self, position, rotation, directorSpawnRequest, ref result);

            if (result.success && result.spawnedInstance)
            {
                InteractableTracker interactableTracker = result.spawnedInstance.AddComponent<InteractableTracker>();
                interactableTracker.SpawnCard = self;
            }
        }
    }
}
