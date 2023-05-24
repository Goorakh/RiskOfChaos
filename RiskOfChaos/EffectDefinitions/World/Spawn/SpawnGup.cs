using HG;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_gup")]
    public sealed class SpawnGup : BaseEffect
    {
        static GameObject _gupPrefab;

        [SystemInitializer]
        static void Init()
        {
            _gupPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Gup/GupMaster.prefab").WaitForCompletion();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _gupPrefab && ExpansionUtils.IsCharacterMasterExpansionAvailable(_gupPrefab) && PlayerUtils.GetAllPlayerBodies(true).Any();
        }

        public override void OnStart()
        {
            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                spawnGupOn(playerBody);
            }
        }

        void spawnGupOn(CharacterBody playerBody)
        {
            if (!playerBody)
                return;

            MasterSummon gupSummon = new MasterSummon
            {
                masterPrefab = _gupPrefab,
                teamIndexOverride = TeamIndex.Monster,
                useAmbientLevel = true,
                position = playerBody.footPosition + new Vector3(0f, 20f, 0f) + (3f * RNG.PointInUnitSphere()),
                rotation = Quaternion.identity,
                ignoreTeamMemberLimit = true
            };

            CharacterMaster gupMaster = gupSummon.Perform();
            if (!gupMaster)
                return;

            CharacterBody gupBody = gupMaster.GetBody();
            if (gupBody && gupBody.TryGetComponent(out IPhysMotor physMotor))
            {
                physMotor.ApplyForceImpulse(new PhysForceInfo
                {
                    force = new Vector3(0f, -15f, 0f),
                    massIsOne = true
                });
            }
        }
    }
}
