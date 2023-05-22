using RoR2;
using UnityEngine;

namespace RiskOfChaos.ModifierController.Gravity
{
    public class GravityModificationManager : NetworkedValueModificationManager<IGravityModificationProvider, Vector3>
    {
        static GravityModificationManager _instance;
        public static GravityModificationManager Instance => _instance;

        static readonly Vector3 _baseGravity = new Vector3(0f, Run.baseGravity, 0f);

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        protected override void updateValueModifications()
        {
            Physics.gravity = getModifiedValue(_baseGravity);
        }
    }
}
