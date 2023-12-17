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
            GameObject hudCountdownPanel = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HudCountdownPanel.prefab").WaitForCompletion();
            if (hudCountdownPanel)
            {
                hudCountdownPanel.AddComponent<HudCountdownPanelTracker>();
            }
            else
            {
                Log.Error("Unable to load countdown panel prefab asset");
            }
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
