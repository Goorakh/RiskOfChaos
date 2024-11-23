using RiskOfChaos.Utilities.BodySnapshots;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Utilities.Extensions
{
    [Flags]
    public enum CharacterRespawnFlags : byte
    {
        None,
        KeepVehicle = 1 << 0,
        SkipIntroState = 1 << 1,
        KeepCurseStacks = 1 << 2,
        KeepHealth = 1 << 3,
        KeepDesperado = 1 << 4,

        KeepStatusEffects = KeepCurseStacks | KeepDesperado,

        KeepState = KeepVehicle | KeepStatusEffects | KeepHealth,

        Seamless = SkipIntroState | KeepState
    }

    public static class CharacterMasterExtensions
    {
        public static bool IsAlive(this CharacterMaster master)
        {
            if (NetworkServer.active)
            {
                return !master.IsDeadAndOutOfLivesServer();
            }
            else
            {
                CharacterBody body = master.GetBody();
                return body && body.healthComponent.alive;
            }
        }

        record struct ConditionalBodySnapshot(IBodySnapshot Snapshot, CharacterRespawnFlags RequiredFlag)
        {
            public readonly void TryApplyTo(CharacterBody body, CharacterRespawnFlags flags)
            {
                if ((flags & RequiredFlag) != 0)
                {
                    Snapshot.ApplyTo(body);
                }
            }
        }

        public static CharacterBody Respawn(this CharacterMaster master, CharacterRespawnFlags flags)
        {
            CharacterBody body = master.GetBody();
            if (!body)
            {
                master.SpawnBodyHere();
                return master.GetBody();
            }

            VehicleSeat oldVehicleSeat = body.currentVehicle;

            if ((flags & CharacterRespawnFlags.KeepVehicle) != 0)
            {
                Log.Debug($"seat={oldVehicleSeat}");

                if (oldVehicleSeat)
                {
                    oldVehicleSeat.EjectPassenger();
                }
            }

            ConditionalBodySnapshot[] snapshots = [
                new ConditionalBodySnapshot(BuffSnapshot.FromBody(body, RoR2Content.Buffs.PermanentCurse.buffIndex), CharacterRespawnFlags.KeepCurseStacks),
                new ConditionalBodySnapshot(BuffSnapshot.FromBody(body, RoR2Content.Buffs.BanditSkull.buffIndex), CharacterRespawnFlags.KeepDesperado),
                new ConditionalBodySnapshot(HealthSnapshot.FromBody(body), CharacterRespawnFlags.KeepHealth),
            ];

            body = master.Respawn(body.footPosition, body.transform.rotation);

            if ((flags & CharacterRespawnFlags.SkipIntroState) != 0)
            {
                foreach (EntityStateMachine esm in body.GetComponents<EntityStateMachine>())
                {
                    esm.initialStateType = esm.mainStateType;
                }
            }

            if ((flags & CharacterRespawnFlags.KeepVehicle) != 0)
            {
                if (oldVehicleSeat)
                {
                    oldVehicleSeat.AssignPassenger(body.gameObject);
                }
            }

            foreach (ConditionalBodySnapshot conditionalSnapshot in snapshots)
            {
                conditionalSnapshot.TryApplyTo(body, flags);
            }

            return body;
        }

        public static bool TryGetBodyPosition(this CharacterMaster master, out Vector3 position)
        {
            if (!master)
            {
                position = default;
                return false;
            }

            if (master.lostBodyToDeath)
            {
                position = master.deathFootPosition;
                return true;
            }

            CharacterBody body = master.GetBody();
            if (body)
            {
                position = body.corePosition;
                return true;
            }

            position = default;
            return false;
        }
    }
}
