using RiskOfChaos.EffectHandling;
using System;

namespace RiskOfChaos.EffectDefinitions
{
    [Obsolete]
    public abstract class TimedEffect : BaseEffect
    {
        public readonly new TimedEffectInfo EffectInfo;

        public TimedEffect() : base()
        {
        }

        public bool IsNetDirty;

        public TimedEffectType TimedType { get; internal set; }

        public bool MatchesFlag(TimedEffectFlags flags)
        {
            return false;
        }

        public virtual bool ShouldDisplayOnHUD
        {
            get
            {
                return EffectInfo.ShouldDisplayOnHUD && (TimedType != TimedEffectType.AlwaysActive || Configs.UI.DisplayAlwaysActiveEffects.Value);
            }
        }

        public float MaxStocks
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public uint SpentStocks
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public float StocksRemaining => MaxStocks - SpentStocks;

        public float DurationSeconds { get; internal set; } = -1f;
        public float TimeStarted { get; private set; }

        public float TimeElapsed
        {
            get
            {
                return 0;
            }
        }

        public float TimeRemaining
        {
            get
            {
                return 0;
            }
        }

        public abstract void OnEnd();
    }
}
