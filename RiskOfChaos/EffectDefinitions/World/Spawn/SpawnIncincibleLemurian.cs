using R2API;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.CharacterAI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("SpawnInvincibleLemurian", EffectRepetitionWeightExponent = 15f)]
    public class SpawnIncincibleLemurian : BaseEffect
    {
        static readonly SpawnCard _cscLemurian = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Lemurian/cscLemurian.asset").WaitForCompletion();

        static readonly HashSet<GameObject> _activeLemurianMasterObjects = new HashSet<GameObject>();

        static bool _hooksApplied;
        static void tryApplyHooks()
        {
            if (_hooksApplied)
                return;

            On.RoR2.HealthComponent.TakeDamage += static (orig, self, damageInfo) =>
            {
                if (NetworkServer.active &&
                    damageInfo != null &&
                    damageInfo.attacker &&
                    damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) &&
                    _activeLemurianMasterObjects.Contains(attackerBody.masterObject))
                {
                    // you die if the snail touches you
                    damageInfo.damage = float.PositiveInfinity;
                    damageInfo.damageType |= DamageType.BypassArmor | DamageType.BypassBlock | DamageType.BypassOneShotProtection;
                }

                orig(self, damageInfo);
            };

            On.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += static (orig, self, maxDistance, full360Vision, filterByLoS) =>
            {
                if (self && _activeLemurianMasterObjects.Contains(self.gameObject))
                {
                    filterByLoS = false;
                }

                return orig(self, maxDistance, full360Vision, filterByLoS);
            };

            RecalculateStatsAPI.GetStatCoefficients += static (body, args) =>
            {
                if (body && _activeLemurianMasterObjects.Contains(body.masterObject))
                {
                    args.moveSpeedReductionMultAdd += 1f;
                }
            };

            _hooksApplied = true;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _cscLemurian && DirectorCore.instance;
        }

        public override void OnStart()
        {
            DirectorPlacementRule placement = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                position = SpawnUtils.GetRandomSpawnPosition(RNG, false) ?? Vector3.zero,
                minDistance = 0f,
                maxDistance = float.PositiveInfinity
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_cscLemurian, placement, new Xoroshiro128Plus(RNG.nextUlong))
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

            spawnRequest.onSpawnedServer = static result =>
            {
                if (result.success && result.spawnedInstance)
                {
                    CharacterMaster characterMaster = result.spawnedInstance.GetComponent<CharacterMaster>();
                    if (characterMaster)
                    {
                        static void giveBuff(CharacterBody body)
                        {
                            body.AddBuff(RoR2Content.Buffs.Immune);
                        }

                        characterMaster.onBodyStart += giveBuff;

                        BaseAI baseAI = characterMaster.GetComponent<BaseAI>();
                        if (baseAI)
                        {
                            baseAI.fullVision = true;
                        }

                        CharacterBody body = characterMaster.GetBody();
                        if (body)
                        {
                            giveBuff(body);
                        }

                        if (_activeLemurianMasterObjects.Add(characterMaster.gameObject))
                        {
                            tryApplyHooks();
                            OnDestroyCallback.AddCallback(characterMaster.gameObject, static onDestroyCallback =>
                            {
                                _activeLemurianMasterObjects.Remove(onDestroyCallback.gameObject);
                            });
                        }
                    }
                }
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
