using System;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class WeightedSelectionExtensions
    {
        public static void AddOrModifyWeight<T>(this WeightedSelection<T> selection, T value, float weight) where T : IEquatable<T>
        {
            // If entry already exists, set the weight of that choice
            for (int i = 0; i < selection.Count; i++)
            {
                WeightedSelection<T>.ChoiceInfo choiceInfo = selection.choices[i];
                if (value.Equals(choiceInfo.value))
                {
                    selection.ModifyChoiceWeight(i, weight);
                    return;
                }
            }

            // Item doesn't exist in the selection, so add it
            selection.AddChoice(value, weight);
        }

        public static float GetSelectionChance<T>(this WeightedSelection<T> selection, float selectedWeight)
        {
            return selectedWeight / selection.totalWeight;
        }

        public static float GetSelectionChance<T>(this WeightedSelection<T> selection, WeightedSelection<T>.ChoiceInfo selectedChoice)
        {
            return selection.GetSelectionChance(selectedChoice.weight);
        }

        public static T GetRandom<T>(this WeightedSelection<T> selection, Xoroshiro128Plus rng)
        {
            if (selection.Count == 0)
                throw new IndexOutOfRangeException("Selection is empty, no element can be picked");

#if DEBUG
            int choiceIndex = selection.EvaluateToChoiceIndex(rng.nextNormalizedFloat);
            WeightedSelection<T>.ChoiceInfo effectChoice = selection.GetChoice(choiceIndex);

            T result = effectChoice.value;

            float effectWeight = effectChoice.weight;
            Log.Debug($"{effectChoice.value} selected, weight={effectWeight} ({selection.GetSelectionChance(effectWeight):P} chance)");

            return result;
#else
            return selection.Evaluate(rng.nextNormalizedFloat);
#endif
        }

        public static T GetAndRemoveRandom<T>(this WeightedSelection<T> selection, Xoroshiro128Plus rng)
        {
            if (selection.Count == 0)
                throw new IndexOutOfRangeException("Selection is empty, no element can be picked");

            int choiceIndex = selection.EvaluateToChoiceIndex(rng.nextNormalizedFloat);
            WeightedSelection<T>.ChoiceInfo effectChoice = selection.GetChoice(choiceIndex);

            T result = effectChoice.value;

#if DEBUG
            float effectWeight = effectChoice.weight;
            Log.Debug($"{effectChoice.value} selected, weight={effectWeight} ({selection.GetSelectionChance(effectWeight):P} chance)");
#endif

            selection.RemoveChoice(choiceIndex);

            return result;
        }

        public static void EnsureCapacity<T>(this WeightedSelection<T> selection, int capacity)
        {
            if (selection.Capacity < capacity)
            {
                selection.Capacity = capacity;
            }
        }
    }
}
