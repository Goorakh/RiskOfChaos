using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_pots")]
    public sealed class SpawnPots : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _potCount =
            ConfigFactory<int>.CreateConfig("Pot Spawn Count", 50)
                              .Description("How many pots should be spawned per player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        static GameObject _potPrefab;

        [SystemInitializer(typeof(BodyCatalog))]
        static void Init()
        {
            _potPrefab = BodyCatalog.FindBodyPrefab("ExplosivePotDestructibleBody");
            if (!_potPrefab)
            {
                Log.Error("Failed to find explosive pot body prefab");
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _potPrefab;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectComponent.EffectDestructionHandledByComponent = true;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        IEnumerator Start()
        {
            if (!NetworkServer.active)
                yield break;

            const float WAIT_BETWEEN_POT_SPAWNS = 0.1f;

            Vector3 spawnPositionOffset = new Vector3(0f, 10f, 0f);
            for (int i = 0; i < _potCount.Value; i++)
            {
                Xoroshiro128Plus potRng = new Xoroshiro128Plus(_rng.nextUlong);

                foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
                {
                    if (!playerMaster.isConnected)
                        continue;

                    CharacterMaster master = playerMaster.master;
                    if (!master || master.IsDeadAndOutOfLivesServer())
                        continue;

                    if (!master.TryGetBodyPosition(out Vector3 bodyPosition))
                        continue;

                    Vector3 randomOffset = potRng.PointOnUnitSphere() * potRng.RangeFloat(0f, 4f);
                    GameObject pot = Instantiate(_potPrefab, bodyPosition + spawnPositionOffset + randomOffset, potRng.RandomRotation());
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

            _effectComponent.RetireEffect();
        }
    }
}
