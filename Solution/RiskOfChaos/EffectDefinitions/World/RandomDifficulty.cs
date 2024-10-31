using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("random_difficulty", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.6f, HideFromEffectsListWhenPermanent = true)]
    public sealed class RandomDifficulty : NetworkBehaviour
    {
        ChaosEffectComponent _effectComponent;

        ObjectSerializationComponent _serializationComponent;

        [SerializedMember("i")]
        int _difficultyModificationId;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _serializationComponent = GetComponent<ObjectSerializationComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_serializationComponent && _serializationComponent.IsLoadedFromSave)
                return;

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            DifficultyIndex currentDifficulty = Run.instance.selectedDifficulty;

            const int TOTAL_DIFFICULTIES_COUNT = (int)DifficultyIndex.Count;

            WeightedSelection<DifficultyIndex> newDifficultySelection = new WeightedSelection<DifficultyIndex>();
            newDifficultySelection.EnsureCapacity(TOTAL_DIFFICULTIES_COUNT - 1); // -1 since the current difficulty will be excluded
            for (DifficultyIndex i = 0; i < (DifficultyIndex)TOTAL_DIFFICULTIES_COUNT; i++)
            {
                if (i == currentDifficulty)
                    continue;

                newDifficultySelection.AddChoice(i, 1f / Mathf.Abs(i - currentDifficulty));
            }

            DifficultyIndex newDifficultyIndex = newDifficultySelection.GetRandom(rng);

#if DEBUG
            DifficultyDef selectedDifficultyDef = DifficultyCatalog.GetDifficultyDef(newDifficultyIndex);
            Log.Debug($"Selected difficulty: {(selectedDifficultyDef != null ? Language.GetString(selectedDifficultyDef.nameToken) : "NULL")}");
#endif

            _difficultyModificationId = DifficultyModificationManager.Instance.PushDifficultyModification(newDifficultyIndex);
        }

        void OnDestroy()
        {
            if (NetworkServer.active && DifficultyModificationManager.Instance)
            {
                DifficultyModificationManager.Instance.PopDifficultyModification(_difficultyModificationId);
            }
        }
    }
}
