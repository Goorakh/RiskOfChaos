using RoR2;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class TimeUtils
    {
        public static float UnpausedTimeScale
        {
            get
            {
                if (PauseStopController.instance && PauseStopController.instance.isPaused)
                {
                    return PauseStopController.instance._oldTimeScale;
                }
                else
                {
                    return Time.timeScale;
                }
            }
            set
            {
                if (PauseStopController.instance && PauseStopController.instance.isPaused)
                {
                    PauseStopController.instance._oldTimeScale = value;
                }
                else
                {
                    Time.timeScale = value;
                }
            }
        }
    }
}
