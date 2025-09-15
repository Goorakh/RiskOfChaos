using RiskOfChaos.Content;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class DummyDamageInflictor : MonoBehaviour
    {
        [ContentInitializer]
        static void InitContent(ContentIntializerArgs args)
        {
            GameObject dummyDamageInflictor = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.DummyDamageInflictor), [
                typeof(SetDontDestroyOnLoad),
                typeof(AutoCreateOnRunStart),
                typeof(DestroyOnRunEnd),
                typeof(DummyDamageInflictor)
            ]);

            args.ContentPack.networkedObjectPrefabs.Add([dummyDamageInflictor]);
        }

        public static DummyDamageInflictor Instance => _instance;
        static DummyDamageInflictor _instance;

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
