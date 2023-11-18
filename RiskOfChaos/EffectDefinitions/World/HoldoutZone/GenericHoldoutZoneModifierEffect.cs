using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    public abstract class GenericHoldoutZoneModifierEffect : TimedEffect
    {
        [RequireComponent(typeof(HoldoutZoneController))]
        sealed class HoldoutZoneModifierEvents : MonoBehaviour
        {
            [SystemInitializer]
            static void Init()
            {
                On.RoR2.HoldoutZoneController.Awake += (orig, self) =>
                {
                    orig(self);
                    self.gameObject.AddComponent<HoldoutZoneModifierEvents>();
                };
            }

            public delegate void ModifyRadiusDelegate(HoldoutZoneController controller, ref float radius);
            public static event ModifyRadiusDelegate ModifyRadius;

            public delegate void ModifyChargeRateDelegate(HoldoutZoneController controller, ref float chargeRate);
            public static event ModifyChargeRateDelegate ModifyChargeRate;

            public delegate void ModifyColorDelegate(HoldoutZoneController controller, ref Color color);
            public static event ModifyColorDelegate ModifyColor;

            HoldoutZoneController _holdoutZoneController;

            void Awake()
            {
                _holdoutZoneController = GetComponent<HoldoutZoneController>();
            }

            void OnEnable()
            {
                if (NetworkServer.active)
                {
                    _holdoutZoneController.calcRadius += calcRadius;
                    _holdoutZoneController.calcChargeRate += calcChargeRate;
                    _holdoutZoneController.calcColor += calcColor;
                }
            }

            void OnDisable()
            {
                if (NetworkServer.active)
                {
                    _holdoutZoneController.calcRadius -= calcRadius;
                    _holdoutZoneController.calcChargeRate -= calcChargeRate;
                    _holdoutZoneController.calcColor -= calcColor;
                }
            }

            void calcRadius(ref float radius)
            {
                ModifyRadius?.Invoke(_holdoutZoneController, ref radius);
            }

            void calcChargeRate(ref float rate)
            {
                ModifyChargeRate?.Invoke(_holdoutZoneController, ref rate);
            }

            void calcColor(ref Color color)
            {
                ModifyColor?.Invoke(_holdoutZoneController, ref color);
            }
        }

        public override void OnStart()
        {
            if (NetworkServer.active)
            {
                HoldoutZoneModifierEvents.ModifyRadius += modifyRadius;
                HoldoutZoneModifierEvents.ModifyChargeRate += modifyChargeRate;
                HoldoutZoneModifierEvents.ModifyColor += modifyColor;
            }
        }

        public override void OnEnd()
        {
            if (NetworkServer.active)
            {
                HoldoutZoneModifierEvents.ModifyRadius -= modifyRadius;
                HoldoutZoneModifierEvents.ModifyChargeRate -= modifyChargeRate;
                HoldoutZoneModifierEvents.ModifyColor -= modifyColor;
            }
        }

        protected virtual void modifyRadius(HoldoutZoneController controller, ref float radius)
        {
        }

        protected virtual void modifyChargeRate(HoldoutZoneController controller, ref float rate)
        {
        }

        protected virtual void modifyColor(HoldoutZoneController controller, ref Color color)
        {
        }
    }
}
