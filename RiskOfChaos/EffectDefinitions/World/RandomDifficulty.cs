using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.Effects;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect(EFFECT_IDENTIFIER, TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.6f, HideFromEffectsListWhenPermanent = true)]
    public sealed class RandomDifficulty : TimedEffect
    {
        public const string EFFECT_IDENTIFIER = "random_difficulty";

        static Stack<DifficultyIndex> _previousDifficulties = new Stack<DifficultyIndex>();

        static void clearPreviousDifficulties()
        {
            _previousDifficulties.Clear();
        }

        [SystemInitializer]
        static void Init()
        {
            Run.onRunStartGlobal += _ =>
            {
                clearPreviousDifficulties();
            };

            Run.onRunDestroyGlobal += _ =>
            {
                clearPreviousDifficulties();
            };

            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += CollectSaveData;
                SaveManager.LoadSaveData += SaveManager_LoadSaveData;
            }
        }

        static void CollectSaveData(ref SaveContainer container)
        {
            container.Effects.RandomDifficulty_Data = new RandomDifficulty_Data
            {
                PreviousDifficulties = _previousDifficulties.ToArray()
            };
        }

        static void SaveManager_LoadSaveData(in SaveContainer container)
        {
            RandomDifficulty_Data data = container.Effects?.RandomDifficulty_Data;
            if (data is null)
            {
                _previousDifficulties.Clear();
            }
            else
            {
                _previousDifficulties = new Stack<DifficultyIndex>(data.PreviousDifficulties);
            }
        }

        DifficultyIndex _newDifficultyIndex;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            DifficultyIndex currentDifficulty = Run.instance.selectedDifficulty;

            const int TOTAL_DIFFICULTIES_COUNT = (int)DifficultyIndex.Count;

            // -1 since the current difficulty will be excluded
            WeightedSelection<DifficultyIndex> newDifficultySelection = new WeightedSelection<DifficultyIndex>(TOTAL_DIFFICULTIES_COUNT - 1);
            for (DifficultyIndex i = 0; i < (DifficultyIndex)TOTAL_DIFFICULTIES_COUNT; i++)
            {
                if (i == currentDifficulty)
                    continue;

                newDifficultySelection.AddChoice(i, 1f / Mathf.Abs(i - currentDifficulty));
            }

            _newDifficultyIndex = newDifficultySelection.GetRandom(RNG);

#if DEBUG
            DifficultyDef selectedDifficultyDef = DifficultyCatalog.GetDifficultyDef(_newDifficultyIndex);
            Log.Debug($"Selected difficulty: {(selectedDifficultyDef != null ? Language.GetString(selectedDifficultyDef.nameToken) : "NULL")}");
#endif

            _previousDifficulties.Push(currentDifficulty);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)_newDifficultyIndex);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _newDifficultyIndex = (DifficultyIndex)reader.ReadInt32();
        }

        public override void OnStart()
        {
            Run.instance.selectedDifficulty = _newDifficultyIndex;
        }

        public override void OnEnd()
        {
            if (_previousDifficulties.Count > 0)
            {
                DifficultyIndex restoredDifficultyIndex = _previousDifficulties.Pop();

                if (Run.instance)
                {
#if DEBUG
                    DifficultyDef restoredDifficultyDef = DifficultyCatalog.GetDifficultyDef(restoredDifficultyIndex);
                    Log.Debug($"Restoring difficulty: {(restoredDifficultyDef != null ? Language.GetString(restoredDifficultyDef.nameToken) : "NULL")}");
#endif

                    Run.instance.selectedDifficulty = restoredDifficultyIndex;
                }
            }
            else if (Run.instance)
            {
                Log.Error("Ending effect, but no difficulty to restore! This should never happen.");
            }
        }
    }
}
