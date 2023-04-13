using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_invincible_lemurian", EffectWeightReductionPercentagePerActivation = 15f)]
    public sealed class SpawnIncincibleLemurian : BaseEffect
    {
        static readonly SpawnCard _cscLemurian = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Lemurian/cscLemurian.asset").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _cscLemurian && DirectorCore.instance;
        }

        public override void OnStart()
        {
            DirectorPlacementRule placement = new DirectorPlacementRule
            {
                placementMode = SpawnUtils.GetBestValidRandomPlacementType()
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_cscLemurian, placement, new Xoroshiro128Plus(RNG.nextUlong))
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

            spawnRequest.onSpawnedServer = static result =>
            {
                if (!result.success || !result.spawnedInstance)
                    return;

                CharacterMaster characterMaster = result.spawnedInstance.GetComponent<CharacterMaster>();
                if (!characterMaster)
                    return;
                
                characterMaster.inventory.GiveItem(Items.InvincibleLemurianMarker);

                foreach (BaseAI baseAI in characterMaster.GetComponents<BaseAI>())
                {
                    if (baseAI)
                    {
                        baseAI.fullVision = true;
                        baseAI.neverRetaliateFriendlies = true;
                    }
                }
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
