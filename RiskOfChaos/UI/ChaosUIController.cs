using RiskOfChaos.UI.ChatVoting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.UI
{
    public class ChaosUIController : MonoBehaviour
    {
        internal static void Initialize()
        {
            AsyncOperationHandle<GameObject> loadHudPrefabHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HUDSimple.prefab");
            loadHudPrefabHandle.Completed += handle =>
            {
                GameObject hudSimple = handle.Result;
                if (!hudSimple)
                {
                    Log.Error("Unable to load hud prefab");
                    return;
                }

                hudSimple.AddComponent<ChaosUIController>();

#if DEBUG
                Log.Debug("Modified hud prefab");
#endif
            };
        }

        static ChaosUIController _instance;

        public static ChaosUIController Instance => _instance;

        public ChaosEffectVoteDisplayController EffectVoteDisplayController { get; private set; }

        void Awake()
        {
            EffectVoteDisplayController = ChaosEffectVoteDisplayController.Create(this);
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }
    }
}
