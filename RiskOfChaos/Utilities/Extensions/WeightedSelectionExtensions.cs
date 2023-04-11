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
    }
}
