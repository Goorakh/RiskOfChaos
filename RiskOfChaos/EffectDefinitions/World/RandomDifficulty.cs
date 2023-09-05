using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("random_difficulty", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.6f, AllowDuplicates = false, HideFromEffectsListWhenPermanent = true)]
    public sealed class RandomDifficulty : TimedEffect
    {
        static int difficultiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                return DifficultyCatalog.difficultyDefs.Length;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }

        DifficultyIndex _previousDifficulty;

        public override void OnStart()
        {
            _previousDifficulty = Run.instance.selectedDifficulty;

            int totalDifficultiesCount = difficultiesCount;

            // -1 since the current difficulty will be excluded
            WeightedSelection<DifficultyIndex> newDifficultySelection = new WeightedSelection<DifficultyIndex>(totalDifficultiesCount - 1);
            for (int i = 0; i < totalDifficultiesCount; i++)
            {
                if (i == (int)_previousDifficulty)
                    continue;

                newDifficultySelection.AddChoice((DifficultyIndex)i, 1f / Mathf.Abs(i - (int)_previousDifficulty));
            }

            DifficultyIndex newDifficultyIndex = newDifficultySelection.GetRandom(RNG);
            Run.instance.selectedDifficulty = newDifficultyIndex;

#if DEBUG
            DifficultyDef selectedDifficultyDef = DifficultyCatalog.GetDifficultyDef(newDifficultyIndex);
            Log.Debug($"Selected difficulty: {(selectedDifficultyDef != null ? Language.GetString(selectedDifficultyDef.nameToken) : "NULL")}");
#endif
        }

        public override void OnEnd()
        {
            Run.instance.selectedDifficulty = _previousDifficulty;
        }
    }
}
