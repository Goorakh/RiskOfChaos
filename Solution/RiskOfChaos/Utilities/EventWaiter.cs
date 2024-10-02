using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities
{
    public class EventWaiter
    {
        public event Action OnAnyEventInvoked;
        public event Action OnAllEventsInvoked;

        bool _allEventsInvoked;
        public bool ResetAfterAllEventsInvoked;

        record struct EventInfo(bool HasInvoked);
        readonly List<EventInfo> _events = [];

        public Action GetListener()
        {
            _events.Add(new EventInfo(false));
            int eventIndex = _events.Count - 1;

            return () =>
            {
                if (eventIndex < 0 || eventIndex >= _events.Count)
                    return;

                if (!_events[eventIndex].HasInvoked)
                {
                    setHasEventInvoked(eventIndex, true);
                    onEventInvoked();
                }
            };
        }

        void setHasEventInvoked(int index, bool isInvoked)
        {
            if (index < 0 || index >= _events.Count)
                return;

            EventInfo eventInfo = _events[index];
            eventInfo.HasInvoked = isInvoked;
            _events[index] = eventInfo;
        }

        void onEventInvoked()
        {
            OnAnyEventInvoked?.Invoke();

            if (!_allEventsInvoked)
            {
                if (_events.All(e => e.HasInvoked))
                {
                    OnAllEventsInvoked?.Invoke();

                    if (ResetAfterAllEventsInvoked)
                    {
                        for (int i = 0; i < _events.Count; i++)
                        {
                            setHasEventInvoked(i, false);
                        }
                    }
                    else
                    {
                        _allEventsInvoked = true;
                    }
                }
            }
        }
    }
}
