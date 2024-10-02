using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
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
        [EffectConfig]
        static readonly ConfigHolder<int> _potCount =
            ConfigFactory<int>.CreateConfig("Pot Spawn Count", 50)
                              .Description("How many pots should be spawned per player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        static GameObject _potPrefab;

        [SystemInitializer]
        static void Init()
        {
            _potPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ExplosivePotDestructible/ExplosivePotDestructibleBody.prefab").WaitForCompletion();
        }

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

            Vector3 spawnPositionOffset = new Vector3(0f, 10f, 0f);
            for (int i = 0; i < _potCount.Value; i++)
            {
                foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
                {
                    Vector3 randomOffset = RNG.PointOnUnitSphere() * RNG.RangeFloat(0f, 4f);
                    GameObject pot = GameObject.Instantiate(_potPrefab, playerBody.corePosition + spawnPositionOffset + randomOffset, RNG.RandomRotation());
                    NetworkServer.Spawn(pot);

                    if (pot.TryGetComponent(out CharacterBody body))
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
