using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class PositionBetweenPlayers : MonoBehaviour
    {
        public float SmoothTime = 3f;

        Vector3 _smoothVelocity;

        void Start()
        {
            updatePosition(false);
        }

        void FixedUpdate()
        {
            updatePosition(true);
        }

        void updatePosition(bool allowSmoothing)
        {
            List<Vector3> playerPositions = new List<Vector3>(PlayerCharacterMasterController.instances.Count);

            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master)
                    continue;

                if (master.TryGetBodyPosition(out Vector3 position))
                {
                    playerPositions.Add(position);
                }
            }

            if (playerPositions.Count > 0)
            {
                Vector3 averagePlayerPosition = Vector3.zero;
                foreach (Vector3 position in playerPositions)
                {
                    averagePlayerPosition += position;
                }

                averagePlayerPosition /= playerPositions.Count;

                Vector3 newPosition = averagePlayerPosition;
                if (allowSmoothing && SmoothTime > 0f)
                {
                    newPosition = Vector3.SmoothDamp(transform.position, newPosition, ref _smoothVelocity, SmoothTime, float.PositiveInfinity, Time.fixedDeltaTime);
                }

                transform.position = newPosition;
            }
        }
    }
}
