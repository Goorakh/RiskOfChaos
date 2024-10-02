﻿namespace RiskOfTwitch.EventSub
{
    public class TokenAccessRevokedEventData
    {
        public readonly string SubscriptionType;
        public readonly string Status;

        public TokenAccessRevokedEventData(string subscriptionType, string status)
        {
            SubscriptionType = subscriptionType;
            Status = status;
        }
    }
}
