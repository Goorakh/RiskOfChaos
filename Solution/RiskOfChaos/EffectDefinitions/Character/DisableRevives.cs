using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("disable_revives", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public class DisableRevives : MonoBehaviour
    {
        void Start()
        {
            ReviveHooks.OverrideAllowRevive += overrideAllowRevive;
        }

        void OnDestroy()
        {
            ReviveHooks.OverrideAllowRevive -= overrideAllowRevive;
        }

        static void overrideAllowRevive(CharacterMaster master, ref bool allowRevive)
        {
            allowRevive = false;
        }
    }
}
