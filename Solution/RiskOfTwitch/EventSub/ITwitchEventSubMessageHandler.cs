using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RiskOfTwitch.EventSub
{
    internal interface ITwitchEventSubMessageHandler
    {
        Task HandleEventAsync(JToken deserializedEvent, CancellationToken cancellationToken);
    }
}
