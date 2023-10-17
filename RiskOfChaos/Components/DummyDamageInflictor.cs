using UnityEngine;

namespace RiskOfChaos.Components
{
    public class DummyDamageInflictor : MonoBehaviour
    {
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
