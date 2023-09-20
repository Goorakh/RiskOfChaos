using RiskOfChaos.ConfigHandling;
using RiskOfChaos.UI.ActiveEffectsPanel;
using RiskOfChaos.UI.ChatVoting;
using RoR2;
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

        public ChaosEffectVoteDisplayController EffectVoteDisplayController { get; private set; }

        public ChaosActiveEffectsDisplayController ActiveEffectsDisplayController { get; private set; }

        void Awake()
        {
            EffectVoteDisplayController = ChaosEffectVoteDisplayController.Create(this);

            ActiveEffectsDisplayController = ChaosActiveEffectsDisplayController.Create(this);
            setActiveEffectsDisplayActive(!Configs.UI.HideActiveEffectsPanel.Value);
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
