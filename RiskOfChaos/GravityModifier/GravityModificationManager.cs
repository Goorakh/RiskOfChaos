using RoR2;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.GravityModifier
{
    public class GravityModificationManager : NetworkBehaviour
    {
        static GravityModificationManager _instance;
        public static GravityModificationManager Instance => _instance;

        static readonly Vector3 _baseGravity = new Vector3(0f, Run.baseGravity, 0f);

        readonly HashSet<IGravityModificationProvider> _modificationProviders = new HashSet<IGravityModificationProvider>();

        const uint ANY_GRAVITY_MODIFICATION_ACTIVE_DIRTY_BIT = 1 << 0;

        bool _anyGravityModificationActive;
        public bool AnyGravityModificationActive
        {
            get
            {
                return _anyGravityModificationActive;
            }

            [param: In]
            private set
            {
                SetSyncVar(value, ref _anyGravityModificationActive, ANY_GRAVITY_MODIFICATION_ACTIVE_DIRTY_BIT);
            }
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        public void RegisterModificationProvider(IGravityModificationProvider provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Add(provider))
            {
                updateGravity();
            }
        }

        public void UnregisterModificationProvider(IGravityModificationProvider provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Remove(provider))
            {
                updateGravity();
            }
        }

        void updateGravity()
        {
            Vector3 gravity = _baseGravity;

            foreach (IGravityModificationProvider modificationProvider in _modificationProviders)
            {
                modificationProvider.ModifyGravity(ref gravity);
            }

            Physics.gravity = gravity;
            AnyGravityModificationActive = _modificationProviders.Count > 0;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_anyGravityModificationActive);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & ANY_GRAVITY_MODIFICATION_ACTIVE_DIRTY_BIT) != 0)
            {
                writer.Write(_anyGravityModificationActive);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _anyGravityModificationActive = reader.ReadBoolean();
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & ANY_GRAVITY_MODIFICATION_ACTIVE_DIRTY_BIT) != 0)
            {
                _anyGravityModificationActive = reader.ReadBoolean();
            }
        }
    }
}
