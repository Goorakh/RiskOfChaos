using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class TimeUtils
    {
        public static float UnpausedTimeScale
        {
            get
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                if (PauseScreenController.paused)
                {
                    return PauseScreenController.oldTimeScale;
                }
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                else
                {
                    return Time.timeScale;
                }
            }
            set
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                if (PauseScreenController.paused)
                {
                    PauseScreenController.oldTimeScale = value;
                }
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                else
                {
                    Time.timeScale = value;
                }
            }
        }
    }
}
