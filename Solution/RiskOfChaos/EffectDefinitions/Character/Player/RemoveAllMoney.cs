using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("remove_all_money", DefaultSelectionWeight = 0.6f)]
    public sealed class RemoveAllMoney : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            // PlayerMoneyToExpConverter
            {
                GameObject prefab = Prefabs.CreatePrefab(nameof(RoCContent.LocalPrefabs.PlayerMoneyToExpConverter), [
                    typeof(ConvertPlayerMoneyToExperience),
                    typeof(DestroyOnConvertPlayerMoneyComplete),
                    typeof(PositionBetweenPlayers)
                ]);

                ConvertPlayerMoneyToExperience moneyConverter = prefab.GetComponent<ConvertPlayerMoneyToExperience>();
                moneyConverter.burstCount = 16;
                moneyConverter.burstInterval = 0.5f;

                PositionBetweenPlayers positionBetweenPlayers = prefab.GetComponent<PositionBetweenPlayers>();
                positionBetweenPlayers.SmoothTime = 3f;
            }
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            Instantiate(RoCContent.LocalPrefabs.PlayerMoneyToExpConverter);
        }
    }
}
