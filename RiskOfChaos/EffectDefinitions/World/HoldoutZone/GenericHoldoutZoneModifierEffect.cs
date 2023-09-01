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

            public static event HoldoutZoneController.CalcRadiusDelegate ModifyRadius;
            public static event HoldoutZoneController.CalcChargeRateDelegate ModifyChargeRate;
            public static event HoldoutZoneController.CalcColorDelegate ModifyColor;

            HoldoutZoneController _holdoutZoneController;

            void Awake()
            {
                _holdoutZoneController = GetComponent<HoldoutZoneController>();
            }

            void OnEnable()
            {
                _holdoutZoneController.calcRadius += calcRadius;
                _holdoutZoneController.calcChargeRate += calcChargeRate;
                _holdoutZoneController.calcColor += calcColor;
            }

            void OnDisable()
            {
                _holdoutZoneController.calcRadius -= calcRadius;
                _holdoutZoneController.calcChargeRate -= calcChargeRate;
                _holdoutZoneController.calcColor -= calcColor;
            }

            void calcRadius(ref float radius)
            {
                ModifyRadius?.Invoke(ref radius);
            }

            void calcChargeRate(ref float rate)
            {
                ModifyChargeRate?.Invoke(ref rate);
            }

            void calcColor(ref Color color)
            {
                ModifyColor?.Invoke(ref color);
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

        protected virtual void modifyRadius(ref float radius)
        {
        }

        protected virtual void modifyChargeRate(ref float rate)
        {
        }

        protected virtual void modifyColor(ref Color color)
        {
        }
    }
}
