using HG;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class HudCountdownPanelTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_UI_HudCountdownPanel_prefab, AsyncReferenceHandleUnloadType.Preload).OnSuccess(hudCountdownPanel =>
            {
                hudCountdownPanel.EnsureComponent<HudCountdownPanelTracker>();
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
