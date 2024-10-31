using Newtonsoft.Json;
using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.SaveHandling;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectUtils.World
{
    public sealed class DifficultyModificationManager : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            GameObject prefab = Prefabs.CreateNetworkedPrefab("DifficultyModificationManager", [
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                typeof(AutoCreateOnRunStart),
                typeof(ObjectSerializationComponent),
                typeof(DifficultyModificationManager)
            ]);

            ObjectSerializationComponent serializationComponent = prefab.GetComponent<ObjectSerializationComponent>();
            serializationComponent.IsSingleton = true;

            networkPrefabs.Add(prefab);
        }

        static DifficultyModificationManager _instance;
        public static DifficultyModificationManager Instance => _instance;

        [Serializable]
        class DifficultyModification
        {
            [JsonProperty("id")]
            public int Id;

            [JsonProperty("d")]
            public DifficultyIndex OverrideDifficulty = DifficultyIndex.Invalid;

            public DifficultyModification(DifficultyIndex overrideDifficulty, int id)
            {
                OverrideDifficulty = overrideDifficulty;
                Id = id;
            }

            public DifficultyModification()
            {
            }
        }

        [SerializedMember("o")]
        DifficultyIndex _originalDifficulty = DifficultyIndex.Invalid;

        [SerializedMember("m")]
        List<DifficultyModification> _difficultyModificationsStack = [];

        [SerializedMember("n")]
        int _nextDifficultyModificationId = 1;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                refreshDifficulty();
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        public int PushDifficultyModification(DifficultyIndex overrideDifficulty)
        {
            int modificationId = _nextDifficultyModificationId;
            _nextDifficultyModificationId++;

            _difficultyModificationsStack.Add(new DifficultyModification(overrideDifficulty, modificationId));
            refreshDifficulty();

            return modificationId;
        }

        public void PopDifficultyModification(int modificationId)
        {
            for (int i = 0; i < _difficultyModificationsStack.Count; i++)
            {
                if (_difficultyModificationsStack[i].Id == modificationId)
                {
                    _difficultyModificationsStack.RemoveAt(i);
                    refreshDifficulty();
                    break;
                }
            }
        }

        void refreshDifficulty()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            DifficultyIndex previousDifficultyIndex = Run.instance.selectedDifficulty;
            if (_difficultyModificationsStack.Count > 0)
            {
                if (_originalDifficulty == DifficultyIndex.Invalid)
                {
                    _originalDifficulty = previousDifficultyIndex;
                }

                DifficultyIndex overrideDifficulty = _difficultyModificationsStack[^1].OverrideDifficulty;
                Run.instance.selectedDifficulty = overrideDifficulty;

#if DEBUG
                string previousDifficultyName = Language.GetString(DifficultyCatalog.GetDifficultyDef(previousDifficultyIndex)?.nameToken ?? "NULL");
                string newDifficultyName = Language.GetString(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty)?.nameToken ?? "NULL");
                Log.Debug($"Override difficulty changed: {previousDifficultyName} -> {newDifficultyName}");
#endif
            }
            else if (_originalDifficulty != DifficultyIndex.Invalid)
            {
                Run.instance.selectedDifficulty = _originalDifficulty;
                _originalDifficulty = DifficultyIndex.Invalid;

#if DEBUG
                string previousDifficultyName = Language.GetString(DifficultyCatalog.GetDifficultyDef(previousDifficultyIndex)?.nameToken ?? "NULL");
                string newDifficultyName = Language.GetString(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty)?.nameToken ?? "NULL");
                Log.Debug($"Run difficulty restored ({previousDifficultyName} -> {newDifficultyName})");
#endif
            }
        }
    }
}
