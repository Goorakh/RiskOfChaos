using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class HoldoutZoneModifier : NetworkBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HoldoutZoneController.Awake += (orig, self) =>
            {
                orig(self);

                HoldoutZoneModifier zoneModifier = self.gameObject.AddComponent<HoldoutZoneModifier>();
                zoneModifier.HoldoutZoneController = self;

                if (self.minimumRadius == 0f)
                {
                    // Make sure all holdout zones have a minimum radius to avoid softlocks
                    self.minimumRadius = 5f;
                }
            };
        }

        public static event Action<HoldoutZoneModifier> OnHoldoutZoneEnabled;

        HoldoutZoneController _holdoutZoneController;
        public HoldoutZoneController HoldoutZoneController
        {
            get
            {
                return _holdoutZoneController;
            }
            private set
            {
                if (_holdoutZoneController == value)
                    return;

                if (_holdoutZoneController)
                    unsubscribe(_holdoutZoneController);

                _holdoutZoneController = value;

                if (_holdoutZoneController && enabled)
                    subscribe(_holdoutZoneController);
            }
        }

        float _radiusMultiplier = 1f;
        const uint RADIUS_MULTIPLIER_DIRTY_BIT = 1 << 0;

        public float RadiusMultiplier
        {
            get
            {
                return _radiusMultiplier;
            }
            set
            {
                SetSyncVar(value, ref _radiusMultiplier, RADIUS_MULTIPLIER_DIRTY_BIT);
            }
        }

        float _chargeRateMultiplier = 1f;
        const uint CHARGE_RATE_MULTIPLIER_DIRTY_BIT = 1 << 1;

        public float ChargeRateMultiplier
        {
            get
            {
                return _chargeRateMultiplier;
            }
            set
            {
                SetSyncVar(value, ref _chargeRateMultiplier, CHARGE_RATE_MULTIPLIER_DIRTY_BIT);
            }
        }

        Color? _overrideColor = null;
        const uint OVERRIDE_COLOR_DIRTY_BIT = 1 << 2;

        public Color? OverrideColor
        {
            get
            {
                return _overrideColor;
            }
            set
            {
                if (_overrideColor != value)
                {
                    SetDirtyBit(OVERRIDE_COLOR_DIRTY_BIT);
                    _overrideColor = value;
                }
            }
        }

        void subscribe(HoldoutZoneController zoneController)
        {
            zoneController.calcRadius += calcRadius;
            zoneController.calcChargeRate += calcChargeRate;
            zoneController.calcColor += calcColor;
        }

        void unsubscribe(HoldoutZoneController zoneController)
        {
            zoneController.calcRadius -= calcRadius;
            zoneController.calcChargeRate -= calcChargeRate;
            zoneController.calcColor -= calcColor;
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);

            if (HoldoutZoneController)
            {
                subscribe(HoldoutZoneController);
            }

            OnHoldoutZoneEnabled?.Invoke(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);

            if (HoldoutZoneController)
            {
                unsubscribe(HoldoutZoneController);
            }
        }

        void calcRadius(ref float radius)
        {
            radius *= RadiusMultiplier;
        }

        void calcChargeRate(ref float rate)
        {
            rate *= ChargeRateMultiplier;
        }

        void calcColor(ref Color color)
        {
            color = OverrideColor.GetValueOrDefault(color);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_radiusMultiplier);

                writer.Write(_chargeRateMultiplier);

                writer.Write(_overrideColor.HasValue);
                if (_overrideColor.HasValue)
                {
                    writer.Write(_overrideColor.Value);
                }

                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & RADIUS_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_radiusMultiplier);

                anythingWritten = true;
            }

            if ((dirtyBits & CHARGE_RATE_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_chargeRateMultiplier);

                anythingWritten = true;
            }

            if ((dirtyBits & OVERRIDE_COLOR_DIRTY_BIT) != 0)
            {
                writer.Write(_overrideColor.HasValue);
                if (_overrideColor.HasValue)
                {
                    writer.Write(_overrideColor.Value);
                }

                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _radiusMultiplier = reader.ReadSingle();

                _chargeRateMultiplier = reader.ReadSingle();

                _overrideColor = reader.ReadBoolean() ? reader.ReadColor() : null;

                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & RADIUS_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _radiusMultiplier = reader.ReadSingle();
            }

            if ((dirtyBits & CHARGE_RATE_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _chargeRateMultiplier = reader.ReadSingle();
            }

            if ((dirtyBits & OVERRIDE_COLOR_DIRTY_BIT) != 0)
            {
                _overrideColor = reader.ReadBoolean() ? reader.ReadColor() : null;
            }
        }
    }
}
