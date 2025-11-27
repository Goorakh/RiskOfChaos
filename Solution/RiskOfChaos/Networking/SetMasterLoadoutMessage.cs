using R2API.Networking.Interfaces;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class SetMasterLoadoutMessage : INetMessage
    {
        GameObject _masterObject;
        Loadout _loadout;

        public SetMasterLoadoutMessage(GameObject masterObject, Loadout loadout)
        {
            if (!masterObject)
                throw new ArgumentNullException(nameof(masterObject));

            if (loadout is null)
                throw new ArgumentNullException(nameof(loadout));

            _masterObject = masterObject;
            _loadout = loadout;
        }

        public SetMasterLoadoutMessage()
        {
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(_masterObject);
            _loadout.Serialize(writer);
        }

        public void Deserialize(NetworkReader reader)
        {
            _masterObject = reader.ReadGameObject();

            _loadout = new Loadout();
            _loadout.Deserialize(reader);
        }

        public void OnReceived()
        {
            if (_masterObject && _masterObject.TryGetComponent(out CharacterMaster master))
            {
                master.SetLoadoutServer(_loadout);

                CharacterBody body = master.GetBody();
                if (body)
                {
                    body.SetLoadoutServer(_loadout);
                }
            }
        }
    }
}
