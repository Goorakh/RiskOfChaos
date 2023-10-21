using RiskOfChaos.ConfigHandling;
using RiskOfChaos.UI.ActiveEffectsPanel;
using RiskOfChaos.UI.ChatVoting;
using RiskOfChaos.UI.NextEffectDisplay;
using RoR2;
using RoR2.UI;
using UnityEngine;

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
                self.gameObject.AddComponent<ChaosUIController>();
            };
        }

        static ChaosUIController _instance;

        public static ChaosUIController Instance => _instance;

        public HUD HUD { get; private set; }

        public ChaosEffectVoteDisplayController EffectVoteDisplayController { get; private set; }

        public ChaosActiveEffectsDisplayController ActiveEffectsDisplayController { get; private set; }

        public NextEffectDisplayPanelController NextEffectDisplayController { get; private set; }

        void Awake()
        {
            HUD = GetComponent<HUD>();

            EffectVoteDisplayController = ChaosEffectVoteDisplayController.Create(this);

            ActiveEffectsDisplayController = ChaosActiveEffectsDisplayController.Create(this);
            setActiveEffectsDisplayActive(!Configs.UI.HideActiveEffectsPanel.Value);

            NextEffectDisplayController = NextEffectDisplayPanelController.Create(this);
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            Configs.UI.HideActiveEffectsPanel.SettingChanged += HideActiveEffectsPanelConfigChanged;
        }

        void HideActiveEffectsPanelConfigChanged(object sender, ConfigChangedArgs<bool> e)
        {
            setActiveEffectsDisplayActive(!e.NewValue);
        }

        void setActiveEffectsDisplayActive(bool active)
        {
            ActiveEffectsDisplayController.gameObject.SetActive(active);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            Configs.UI.HideActiveEffectsPanel.SettingChanged -= HideActiveEffectsPanelConfigChanged;
        }
    }
}
