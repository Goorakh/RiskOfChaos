using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Networking;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("random_difficulty", DefaultSelectionWeight = 0.2f, EffectWeightReductionPercentagePerActivation = 75f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class RandomDifficulty : BaseEffect
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

        public override void OnStart()
        {
            DifficultyIndex currentDifficulty = Run.instance.selectedDifficulty;

            int totalDifficultiesCount = difficultiesCount;

            // -1 since the current difficulty will be excluded
            WeightedSelection<DifficultyIndex> newDifficultySelection = new WeightedSelection<DifficultyIndex>(totalDifficultiesCount - 1);
            for (int i = 0; i < totalDifficultiesCount; i++)
            {
                if (i == (int)currentDifficulty)
                    continue;

                newDifficultySelection.AddChoice((DifficultyIndex)i, 1f / Mathf.Abs(i - (int)currentDifficulty));
            }

            int selectedChoiceIndex = newDifficultySelection.EvaluateToChoiceIndex(RNG.nextNormalizedFloat);

            WeightedSelection<DifficultyIndex>.ChoiceInfo choiceInfo = newDifficultySelection.GetChoice(selectedChoiceIndex);

            Run.instance.selectedDifficulty = choiceInfo.value;

#if DEBUG
            DifficultyDef selectedDifficultyDef = DifficultyCatalog.GetDifficultyDef(choiceInfo.value);

            Log.Debug($"Selected difficulty: {choiceInfo.value} (\"{(selectedDifficultyDef != null ? Language.GetString(selectedDifficultyDef.nameToken) : "NULL")}\"), weight={choiceInfo.weight} ({choiceInfo.weight / newDifficultySelection.totalWeight:P2} chance)");
#endif
        }
    }
}
