using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class ChaosInteractor : MonoBehaviour
    {
        static ChaosInteractor _instance;
        public static ChaosInteractor Instance => _instance;

        public CharacterBody Body { get; private set; }
        public Interactor Interactor { get; private set; }

        void Awake()
        {
            Body = GetComponent<CharacterBody>();
            Interactor = GetComponent<Interactor>();
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        public static CharacterBody GetBody()
        {
            if (!Instance)
                return null;

            return Instance.Body;
        }

        public static Interactor GetInteractor()
        {
            if (!Instance)
                return null;

            return Instance.Interactor;
        }
    }
}
