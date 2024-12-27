using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Trackers
{
    public class HudCountdownPanelTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HudCountdownPanel.prefab").OnSuccess(hudCountdownPanel =>
            {
                hudCountdownPanel.AddComponent<HudCountdownPanelTracker>();
            });
        }

        public HUD HUD { get; private set; }

        void refreshOwnerHUD()
        {
            HUD = GetComponentInParent<HUD>();
        }

        void OnTransformParentChanged()
        {
            refreshOwnerHUD();
        }

        void Awake()
        {
            refreshOwnerHUD();
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
