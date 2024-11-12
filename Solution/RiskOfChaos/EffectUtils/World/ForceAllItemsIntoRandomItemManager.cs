using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectDefinitions.World.Items;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.SaveHandling;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectUtils.World
{
    public sealed class ForceAllItemsIntoRandomItemManager : NetworkBehaviour
    {
        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            GameObject prefab = Prefabs.CreateNetworkedPrefab("ForceAllItemsIntoRandomItemManager", [
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                typeof(AutoCreateOnRunStart),
                typeof(ObjectSerializationComponent),
                typeof(ForceAllItemsIntoRandomItemManager)
            ]);

            ObjectSerializationComponent serializationComponent = prefab.GetComponent<ObjectSerializationComponent>();
            serializationComponent.IsSingleton = true;

            networkPrefabs.Add(prefab);
        }

        static ForceAllItemsIntoRandomItemManager _instance;
        public static ForceAllItemsIntoRandomItemManager Instance => _instance;

        public static event Action OnNextOverridePickupChanged;

        PickupIndex _nextOverridePickupIndex = PickupIndex.none;

        [SerializedMember("n")]
        public PickupIndex NextOverridePickupIndex
        {
            get
            {
                return _nextOverridePickupIndex;
            }
            private set
            {
                if (_nextOverridePickupIndex == value)
                    return;

#if DEBUG
                Log.Debug($"Next override pickup: {_nextOverridePickupIndex} -> {value}");
#endif

                _nextOverridePickupIndex = value;
                OnNextOverridePickupChanged?.Invoke();
            }
        }

        [SerializedMember("rng")]
        Xoroshiro128Plus _rng;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            ChaosEffectActivationSignaler_ChatVote.OnEffectVotingFinishedServer += onEffectVotingFinishedServer;

            Stage.onServerStageBegin += Stage_onServerStageBegin;
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            ChaosEffectActivationSignaler_ChatVote.OnEffectVotingFinishedServer -= onEffectVotingFinishedServer;

            Stage.onServerStageBegin -= Stage_onServerStageBegin;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _rng = new Xoroshiro128Plus(Run.instance.seed);
            RollNextOverridePickup();
        }

        [Server]
        public void RollNextOverridePickup()
        {
            NextOverridePickupIndex = ForceAllItemsIntoRandomItem.GenerateOverridePickup(_rng);
        }

        void Stage_onServerStageBegin(Stage stage)
        {
            if (Configs.EffectSelection.PerStageEffectListEnabled.Value &&
                Configs.ChatVoting.VotingMode.Value == Configs.ChatVoting.ChatVotingMode.Disabled)
            {
                _rng = new Xoroshiro128Plus(Run.instance.stageRng);

                if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(ForceAllItemsIntoRandomItem.EffectInfo))
                {
                    RollNextOverridePickup();
                }
            }
        }

        void onEffectVotingFinishedServer(in EffectVoteResult result)
        {
            TimedEffectInfo effectInfo = ForceAllItemsIntoRandomItem.EffectInfo;
            if (ChaosEffectTracker.Instance &&
                ChaosEffectTracker.Instance.IsTimedEffectActive(effectInfo))
            {
                return;
            }

            // If the effect was in this vote, but *didn't* win, reroll for next time
            EffectVoteInfo[] voteOptions = result.VoteSelection.GetVoteOptions();
            if (result.WinningOption.EffectInfo != effectInfo && Array.Exists(voteOptions, v => v.EffectInfo == effectInfo))
            {
                RollNextOverridePickup();
            }
        }
    }
}
