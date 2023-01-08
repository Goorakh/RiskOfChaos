using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_pots")]
    public class SpawnPots : CoroutineEffect
    {
        static readonly GameObject _potPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ExplosivePotDestructible/ExplosivePotDestructibleBody.prefab").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _potPrefab;
        }

        protected override IEnumerator onStart()
        {
            const float WAIT_BETWEEN_POT_SPAWNS = 0.1f;
            const int POT_COUNT = 50;

            Vector3 spawnPositionOffset = new Vector3(0f, 10f, 0f);
            for (int i = 0; i < POT_COUNT; i++)
            {
                foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
                {
                    Vector3 randomOffset = RNG.PointOnUnitSphere() * RNG.RangeFloat(0f, 4f);
                    GameObject pot = GameObject.Instantiate(_potPrefab, playerBody.corePosition + spawnPositionOffset + randomOffset, RNG.RandomRotation());
                    NetworkServer.Spawn(pot);
                }

                yield return new WaitForSeconds(WAIT_BETWEEN_POT_SPAWNS);
            }
        }
    }
}
