using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class StageCompleteMessage : INetMessage
    {
        public delegate void OnReceiveDelegate(Stage instance);
        public static event OnReceiveDelegate OnReceive;

        [SystemInitializer]
        static void Init()
        {
            Stage.onServerStageComplete += instance =>
            {
                new StageCompleteMessage(instance).Send(NetworkDestination.Clients | NetworkDestination.Server);
            };
        }

        Stage _stageInstance;

        public StageCompleteMessage(Stage stageInstance)
        {
            _stageInstance = stageInstance;
        }

        public StageCompleteMessage()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_stageInstance.gameObject);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _stageInstance = reader.ReadGameObject()?.GetComponent<Stage>();
        }

        void INetMessage.OnReceived()
        {
#if DEBUG
            Log.Debug("Stage completed");
#endif

            OnReceive?.Invoke(_stageInstance);
        }
    }
}
