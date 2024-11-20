using RiskOfChaos.EffectDefinitions.UI;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches.Effects.UI
{
    static class ScrambleTextHooks
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Chat.UserChatMessage.Deserialize += UserChatMessage_Deserialize;
        }

        static void UserChatMessage_Deserialize(On.RoR2.Chat.UserChatMessage.orig_Deserialize orig, Chat.UserChatMessage self, NetworkReader reader)
        {
            orig(self, reader);

            if (!string.IsNullOrWhiteSpace(self.text))
            {
                if (ChaosEffectTracker.Instance)
                {
                    ChaosEffectComponent scrambleTextEffectComponent = ChaosEffectTracker.Instance.GetFirstActiveTimedEffect(ScrambleText.EffectInfo);
                    if (scrambleTextEffectComponent && scrambleTextEffectComponent.TryGetComponent(out ScrambleText scrambleTextController))
                    {
                        self.text = scrambleTextController.ScrambleString(self.text);
                    }
                }
            }
        }
    }
}
