using RiskOfChaos.Utilities.Interpolation;
using System;

namespace RiskOfChaos.Components
{
    public interface IInterpolationProvider
    {
        InterpolationParameters InterpolationIn { get; set; }

        InterpolationParameters InterpolationOut { get; set; }

        bool IsInterpolating { get; }

        float CurrentInterpolationFraction { get; }

        event Action OnInterpolationChanged;

        event Action OnInterpolationOutComplete;

        void SetInterpolationParameters(InterpolationParameters parameters);

        void InterpolateOutOrDestroy();
    }
}
