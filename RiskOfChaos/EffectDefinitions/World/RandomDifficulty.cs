using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("random_difficulty", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.6f, HideFromEffectsListWhenPermanent = true)]
    public sealed class RandomDifficulty : TimedEffect
    {
        static readonly Stack<DifficultyIndex> _previousDifficulties = new Stack<DifficultyIndex>();

        static void clearPreviousDifficulties()
        {
            _previousDifficulties.Clear();
        }

        static RandomDifficulty()
        {
            Run.onRunStartGlobal += _ =>
            {
                clearPreviousDifficulties();
            };

            Run.onRunDestroyGlobal += _ =>
            {
                clearPreviousDifficulties();
            };
        }

        public override void OnStart()
        {
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

            DifficultyIndex newDifficultyIndex = newDifficultySelection.GetRandom(RNG);
            Run.instance.selectedDifficulty = newDifficultyIndex;

#if DEBUG
            DifficultyDef selectedDifficultyDef = DifficultyCatalog.GetDifficultyDef(newDifficultyIndex);
            Log.Debug($"Selected difficulty: {(selectedDifficultyDef != null ? Language.GetString(selectedDifficultyDef.nameToken) : "NULL")}");
#endif

            _previousDifficulties.Push(currentDifficulty);
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
