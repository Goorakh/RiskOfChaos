using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Projectile
{
    public class ProjectileModificationManager : ValueModificationManager<IProjectileModificationProvider, ProjectileModificationData>
    {
        static ProjectileModificationManager _instance;
        public static ProjectileModificationManager Instance => _instance;

        const uint TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT = 1 << 1;

        float _totalProjectileSpeedMultiplier = 1f;
        public float NetworkedTotalProjectileSpeedMultiplier
        {
            get
            {
                return _totalProjectileSpeedMultiplier;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _totalProjectileSpeedMultiplier, TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT);
            }
        }

        protected override void updateValueModifications()
        {
            ProjectileModificationData modificationData = getModifiedValue(new ProjectileModificationData());
            NetworkedTotalProjectileSpeedMultiplier = modificationData.SpeedMultiplier;
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseResult = base.serialize(writer, initialState, dirtyBits);
            if (initialState)
            {
                writer.Write(_totalProjectileSpeedMultiplier);
                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_totalProjectileSpeedMultiplier);
                anythingWritten = true;
            }

            return baseResult || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _totalProjectileSpeedMultiplier = reader.ReadSingle();
                return;
            }

            if ((dirtyBits & TOTAL_PROJECTILE_SPEED_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _totalProjectileSpeedMultiplier = reader.ReadSingle();
            }
        }
    }
}
