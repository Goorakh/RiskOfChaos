using RiskOfTwitch.Chat.Message;
using Newtonsoft.Json;

namespace RiskOfTwitch.Chat.Notification
{
    public class ChannelChatNotificationEvent
    {
        [JsonProperty("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; }

        [JsonProperty("broadcaster_user_name")]
        public string BroadcasterDisplayName { get; set; }

        [JsonProperty("broadcaster_user_login")]
        public string BroadcasterLoginName { get; set; }

        [JsonProperty("chatter_user_id")]
        public string ChatterUserId { get; set; }

        [JsonProperty("chatter_user_name")]
        public string ChatterDisplayName { get; set; }

        [JsonProperty("chatter_user_login")]
        public string ChatterLoginName { get; set; }

        [JsonProperty("chatter_is_anonymous")]
        public bool AnonymousChatter { get; set; }

        [JsonProperty("color")]
        public string ChatterNameColor { get; set; }

        [JsonProperty("badges")]
        public ChatterBadgeData[] Badges { get; set; }

        [JsonProperty("system_message")]
        public string SystemMessage { get; set; }

        [JsonProperty("message_id")]
        public string MessageId { get; set; }

        [JsonProperty("message")]
        public ChannelChatMessageData Message { get; set; }

        [JsonProperty("notice_type")]
        public string NoticeType { get; set; }

        [JsonProperty("sub")]
        public ChannelChatSubNotificationData SubData { get; set; }

        [JsonProperty("resub")]
        public ChannelChatResubNotificationData ResubData { get; set; }

        [JsonProperty("sub_gift")]
        public ChannelChatSubGiftNotificationData SubGiftData { get; set; }

        [JsonProperty("community_sub_gift")]
        public ChannelChatCommunitySubGiftNotificationData CommunitySubGiftData { get; set; }

        [JsonProperty("gift_paid_upgrade")]
        public ChannelChatGiftPaidUpgradeNotificationData GiftPaidUpgradeData { get; set; }

        [JsonProperty("prime_paid_upgrade")]
        public ChannelChatPrimePaidUpgradeNotificationData PrimePaidUpgradeData { get; set; }

        [JsonProperty("raid")]
        public ChannelChatRaidNotificationData RaidData { get; set; }

        [JsonProperty("unraid")]
        public ChannelChatUnraidNotificationData UnraidData { get; set; }

        [JsonProperty("pay_it_forward")]
        public ChannelChatPayItForwardNotificationData PayItForwardData { get; set; }

        [JsonProperty("announcement")]
        public ChannelChatAnnouncementNotificationData AnnouncementData { get; set; }

        [JsonProperty("charity_donation")]
        public ChannelChatCharityDonationNotificationData CharityDonationData { get; set; }

        [JsonProperty("bits_badge_tier")]
        public ChannelChatBitsBadgeTierNotificationData ChatBitsBadgeTierData { get; set; }
    }
}
