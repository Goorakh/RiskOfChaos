using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("RandomPortalOrb")]
    public class SpawnRandomPortalOrb : BaseEffect
    {
        const int NUM_SHOULD_ATTEMPT_SPAWN_PROPERTIES = 3;

        readonly struct PortalInfo
        {
            public static readonly PortalInfo Invalid = new PortalInfo(() => false, _ => { });

            readonly Func<bool> _getWillSpawn;
            readonly Action<bool> _setWillSpawn;

            public bool WillSpawn
            {
                get => _getWillSpawn();
                set => _setWillSpawn(value);
            }

            public PortalInfo(Func<bool> getWillSpawn, Action<bool> setWillSpawn)
            {
                _getWillSpawn = getWillSpawn;
                _setWillSpawn = setWillSpawn;
            }
        }

        static PortalInfo GetPortalInfo(int index)
        {
            const string LOG_PREFIX = $"{nameof(SpawnRandomPortalOrb)}.{nameof(GetPortalInfo)} ";

            if (index < 0)
                return PortalInfo.Invalid;

            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;

            if (index < NUM_SHOULD_ATTEMPT_SPAWN_PROPERTIES)
            {
                switch (index)
                {
                    case 0:
                        return new PortalInfo(() => tpInteraction.shouldAttemptToSpawnShopPortal, v => tpInteraction.shouldAttemptToSpawnShopPortal = v);
                    case 1:
                        return new PortalInfo(() => tpInteraction.shouldAttemptToSpawnGoldshoresPortal, v => tpInteraction.shouldAttemptToSpawnGoldshoresPortal = v);
                    case 2:
                        return new PortalInfo(() => tpInteraction.shouldAttemptToSpawnMSPortal, v => tpInteraction.shouldAttemptToSpawnMSPortal = v);
                    default:
                        Log.Error(LOG_PREFIX + $"Unimplemented spawn property index {index}");
                        return PortalInfo.Invalid;
                }
            }
            else if (tpInteraction.portalSpawners != null && index - NUM_SHOULD_ATTEMPT_SPAWN_PROPERTIES < tpInteraction.portalSpawners.Length)
            {
                PortalSpawner portalSpawner = tpInteraction.portalSpawners[index - NUM_SHOULD_ATTEMPT_SPAWN_PROPERTIES];
                return new PortalInfo(() => portalSpawner.NetworkwillSpawn, v => portalSpawner.NetworkwillSpawn = v);
            }
            else
            {
                Log.Error(LOG_PREFIX + $"Unimplemented index {index}");
                return PortalInfo.Invalid;
            }
        }

        static IEnumerable<PortalInfo> getAllPortalInfos()
        {
            return Enumerable.Range(0, NUM_SHOULD_ATTEMPT_SPAWN_PROPERTIES + TeleporterInteraction.instance.portalSpawners.Length).Select(i => GetPortalInfo(i));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            TeleporterInteraction tpInteraction = TeleporterInteraction.instance;
            return tpInteraction && tpInteraction.activationState < TeleporterInteraction.ActivationState.Charged && getAllPortalInfos().Any(p => !p.WillSpawn);
        }

        [EffectWeightMultiplierSelector]
        static float GetWeightMult()
        {
            return RoCMath.CalcReductionWeight(getAllPortalInfos().Count(p => !p.WillSpawn), 3.5f);
        }

        public override void OnStart()
        {
            PortalInfo portalInfo;
            do
            {
                portalInfo = GetPortalInfo(RNG.RangeInt(0, NUM_SHOULD_ATTEMPT_SPAWN_PROPERTIES + TeleporterInteraction.instance.portalSpawners.Length));
            } while (portalInfo.WillSpawn);

            portalInfo.WillSpawn = true;
        }
    }
}
