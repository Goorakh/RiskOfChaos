using HG;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
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
    public sealed class SpawnPots : BaseEffect, ICoroutineEffect
    {
        static readonly GameObject _potPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ExplosivePotDestructible/ExplosivePotDestructibleBody.prefab").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _potPrefab;
        }

        public override void OnStart()
        {
        }

        public IEnumerator OnStartCoroutine()
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

                    CharacterBody body = pot.GetComponent<CharacterBody>();
                    if (body)
                    {
                        body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1f);
                    }
                    else
                    {
                        Log.Warning("Pot has no body component");
                    }
                }

                yield return new WaitForSeconds(WAIT_BETWEEN_POT_SPAWNS);
            }
        }

        public void OnForceStopped()
        {
        }
    }
}
