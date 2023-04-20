using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.GravityModifier
{
    public class GravityModificationManager : NetworkBehaviour
    {
        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            IL.RoR2.CharacterMotor.PreMove += il =>
            {
                ILCursor c = new ILCursor(il);

                ILCursor[] foundCursors;
                if (c.TryFindNext(out foundCursors,
                                  x => x.MatchLdarg(0),
                                  x => x.MatchCall(AccessTools.DeclaredPropertyGetter(typeof(CharacterMotor), nameof(CharacterMotor.useGravity))),
                                  x => x.MatchBrfalse(out _)))
                {
                    ILCursor cursor = foundCursors[2];
                    cursor.Index++;

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.EmitDelegate((CharacterMotor instance, float deltaTime) =>
                    {
                        if (!Instance || !Instance.AnyGravityModificationActive)
                            return;

                        Vector3 xzGravity = new Vector3(Physics.gravity.x, 0f, Physics.gravity.z);
                        instance.velocity += xzGravity * deltaTime;
                    });
                }
            };

            IL.RoR2.ModelLocator.UpdateTargetNormal += il =>
            {
                ILCursor c = new ILCursor(il);

                while (c.TryGotoNext(MoveType.After,
                                     x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Vector3), nameof(Vector3.up)))))
                {
                    c.EmitDelegate((Vector3 up) =>
                    {
                        if (Instance && Instance.AnyGravityModificationActive)
                        {
                            return -Physics.gravity.normalized;
                        }
                        else
                        {
                            return up;
                        }
                    });
                }
            };

            _hasAppliedPatches = true;
        }

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
                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncGravityModificationActive(value);
                    syncVarHookGuard = false;
                }

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

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncGravityModificationActive(_anyGravityModificationActive);
        }

        void syncGravityModificationActive(bool active)
        {
            AnyGravityModificationActive = active;

            if (active)
            {
                tryApplyPatches();
            }
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
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

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
                syncGravityModificationActive(reader.ReadBoolean());
            }
        }
    }
}
