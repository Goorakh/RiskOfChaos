using System;

namespace RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders
{
    public interface IEffectSubtitleProvider
    {
        event Action<IEffectSubtitleProvider> OnSubtitleChanged;

        string GetSubtitle();
    }
}
