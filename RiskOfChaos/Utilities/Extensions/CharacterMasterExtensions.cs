using RoR2;
using System;
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
        KeepState = KeepVehicle | KeepCurseStacks | KeepHealth,
        All = byte.MaxValue
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
#if DEBUG
                Log.Debug($"seat={oldVehicleSeat}");
#endif

                if (oldVehicleSeat)
                {
                    oldVehicleSeat.EjectPassenger();
                }
            }

            int curseStacks = body.GetBuffCount(RoR2Content.Buffs.PermanentCurse);

            HealthSnapshot healthSnapshot = HealthSnapshot.FromBody(body);

            body = master.Respawn(body.footPosition, body.GetRotation());

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

            if ((flags & CharacterRespawnFlags.KeepCurseStacks) != 0)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                body.SetBuffCount(RoR2Content.Buffs.PermanentCurse.buffIndex, curseStacks);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }

            if ((flags & CharacterRespawnFlags.KeepHealth) != 0)
            {
                void bodyStart(CharacterBody b)
                {
                    if (b != body)
                        return;

                    healthSnapshot.ApplyTo(body);

                    CharacterBody.onBodyStartGlobal -= bodyStart;
                }

                CharacterBody.onBodyStartGlobal += bodyStart;
            }

            return body;
        }
    }
}
