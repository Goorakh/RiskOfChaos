using RiskOfChaos.Content;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class DummyDamageInflictor : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            Run.onRunStartGlobal += _ =>
            {
                if (!NetworkServer.active)
                    return;

                NetworkServer.Spawn(GameObject.Instantiate(RoCContent.NetworkedPrefabs.DummyDamageInflictor));
            };
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
