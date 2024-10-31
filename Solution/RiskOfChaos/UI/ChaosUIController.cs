using RiskOfChaos.ConfigHandling;
using RiskOfChaos.UI.ActiveEffectsPanel;
using RiskOfChaos.UI.ChatVoting;
using RiskOfChaos.UI.NextEffectDisplay;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.UI
{
    public class ChaosUIController : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.HUD.Awake += (orig, self) =>
            {
                orig(self);

                if (NetworkClient.active)
                {
                    self.gameObject.AddComponent<ChaosUIController>();
                }
            };
        }

        static ChaosUIController _instance;
        public static ChaosUIController Instance => _instance;

        public HUD HUD { get; private set; }

        public ChaosEffectVoteDisplayController EffectVoteDisplayController { get; private set; }

        public ChaosActiveEffectsPanelController ActiveEffectsDisplayController { get; private set; }

        public NextEffectDisplayPanelController NextEffectDisplayController { get; private set; }

        void Awake()
        {
            HUD = GetComponent<HUD>();

            EffectVoteDisplayController = ChaosEffectVoteDisplayController.Create(this);

            ActiveEffectsDisplayController = ChaosActiveEffectsPanelController.Create(this);

            NextEffectDisplayController = NextEffectDisplayPanelController.Create(this);
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            refreshActiveEffectsDisplayActive();
            Configs.UI.HideActiveEffectsPanel.SettingChanged += HideActiveEffectsPanelConfigChanged;
        }

        void OnDisable()
        {
            Configs.UI.HideActiveEffectsPanel.SettingChanged -= HideActiveEffectsPanelConfigChanged;

            SingletonHelper.Unassign(ref _instance, this);
        }

        void HideActiveEffectsPanelConfigChanged(object sender, ConfigChangedArgs<bool> e)
        {
            refreshActiveEffectsDisplayActive();
        }

        void refreshActiveEffectsDisplayActive()
        {
            if (ActiveEffectsDisplayController)
            {
                ActiveEffectsDisplayController.gameObject.SetActive(!Configs.UI.HideActiveEffectsPanel.Value);
            }
        }
    }
}
