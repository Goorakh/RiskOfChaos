using RoR2.UI;

namespace RiskOfChaos.Components
{
    public interface ICustomPingBehavior
    {
        void OnPingAdded(PingIndicator pingIndicator);

        void OnPingRemoved(PingIndicator pingIndicator);
    }
}
