using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Twitch;
using RiskOfTwitch.Chat.Message;
using RiskOfTwitch.Chat.Notification;
using RiskOfTwitch.EventSub;
using RoR2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch
{
    [ChaosEffectActivationSignaler(Configs.ChatVoting.ChatVotingMode.Twitch)]
    public class ChaosEffectActivationSignaler_TwitchVote : ChaosEffectActivationSignaler_ChatVote
    {
        static ChaosEffectActivationSignaler_TwitchVote _instance;
        public static ChaosEffectActivationSignaler_TwitchVote Instance => _instance;

        public static bool IsConnectionMessageToken(string token)
        {
            switch (token)
            {
                case "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT":
                case "TWITCH_LOGIN_FAIL_NOT_LOGGED_IN":
                case "TWITCH_EFFECT_VOTING_CONNECTION_ERROR":
                case "TWITCH_EFFECT_VOTING_GENERIC_CLIENT_CONNECT_FAIL":
                case "TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_USER_REMOVED":
                case "TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_AUTHORIZATION_REVOKED":
                case "TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_VERSION_REMOVED":
                case "TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_USER_RETRIEVE_FAILED":
                case "TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_SUBSCRIBE_FAILED":
                case "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS":
                case "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS_CHANNEL_FALLBACK":
                    return true;
                default:
                    return false;
            }
        }

        protected override Configs.ChatVoting.ChatVotingMode votingMode { get; } = Configs.ChatVoting.ChatVotingMode.Twitch;

        TwitchEventSubClient _twitchClient;

        readonly Queue<ChatMessageBase> _messageQueue = [];

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);

            connectClient();

            Configs.ChatVoting.OnReconnectButtonPressed += ChatVoting_OnReconnectButtonPressed;
            Configs.ChatVoting.OverrideChannelName.SettingChanged += OverrideChannelName_SettingChanged;
            TwitchAuthenticationManager.OnAccessTokenChanged += TwitchAuthenticationManager_OnAccessTokenChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);

            disconnectClient();

            Configs.ChatVoting.OnReconnectButtonPressed -= ChatVoting_OnReconnectButtonPressed;
            Configs.ChatVoting.OverrideChannelName.SettingChanged -= OverrideChannelName_SettingChanged;
            TwitchAuthenticationManager.OnAccessTokenChanged -= TwitchAuthenticationManager_OnAccessTokenChanged;
        }

        protected override void Update()
        {
            base.Update();

            if (_messageQueue.Count > 0 && Run.FixedTimeStamp.tNow >= 1f)
            {
                do
                {
                    ChatMessageBase message = _messageQueue.Dequeue();

                    Chat.SendBroadcastChat(message);
                }
                while (_messageQueue.Count > 0);
            }
        }

        void connectClient()
        {
            if (_twitchClient != null)
            {
                Log.Warning("Attempting to connect while client connection is already active");
                return;
            }

            if (TwitchAuthenticationManager.CurrentAccessToken.IsEmpty)
            {
                _messageQueue.Enqueue(new Chat.SimpleChatMessage
                {
                    baseToken = "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT",
                    paramTokens = [Language.GetString("TWITCH_LOGIN_FAIL_NOT_LOGGED_IN")]
                });

                return;
            }

            _twitchClient = new TwitchEventSubClient(TwitchAuthenticationManager.CurrentAccessToken.Token, Configs.ChatVoting.OverrideChannelName.Value);
            _twitchClient.OnChannelChatMessage += onChannelChatMessage;
            _twitchClient.OnChannelChatNotification += onChannelChatNotification;
            _twitchClient.OnFullyConnected += onFullyConnected;
            _twitchClient.OnConnectionError += onConnectionError;
            _twitchClient.OnTokenAccessRevoked += onTokenAccessRevoked;

            TwitchEventSubClient client = _twitchClient;
            Task.Run(() => client.Connect());
        }

        void disconnectClient()
        {
            if (_twitchClient != null)
            {
                _twitchClient.OnChannelChatMessage -= onChannelChatMessage;
                _twitchClient.OnChannelChatNotification -= onChannelChatNotification;
                _twitchClient.OnFullyConnected -= onFullyConnected;
                _twitchClient.OnConnectionError -= onConnectionError;
                _twitchClient.OnTokenAccessRevoked -= onTokenAccessRevoked;

                TwitchEventSubClient client = _twitchClient;
                Task.Run(() => client.Disconnect());

                _twitchClient = null;
            }
        }

        void reconnectClient()
        {
            disconnectClient();
            connectClient();
        }

        void ChatVoting_OnReconnectButtonPressed()
        {
            reconnectClient();
        }

        void OverrideChannelName_SettingChanged(object sender, ConfigChangedArgs<string> e)
        {
            reconnectClient();
        }

        void TwitchAuthenticationManager_OnAccessTokenChanged()
        {
            reconnectClient();
        }

        void onConnectionError(object sender, ConnectionErrorEventArgs e)
        {
            string reasonString;
            switch (e.Type)
            {
                case ConnectionErrorType.FailedRetrieveUser:
                    reasonString = Language.GetString("TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_USER_RETRIEVE_FAILED");
                    break;
                case ConnectionErrorType.FailedEventSubscribe:
                    reasonString = Language.GetString("TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_SUBSCRIBE_FAILED");
                    break;
                default:
                    reasonString = Language.GetString("TWITCH_EFFECT_VOTING_GENERIC_CLIENT_CONNECT_FAIL");
                    break;
            }

            _messageQueue.Enqueue(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_CONNECTION_ERROR",
                paramTokens = [reasonString]
            });
        }

        void onFullyConnected(object sender, EventArgs e)
        {
            TwitchEventSubClient client = sender as TwitchEventSubClient;

            if (!string.IsNullOrEmpty(Configs.ChatVoting.OverrideChannelName.Value) &&
                !string.Equals(client.ConnectedToChannel, Configs.ChatVoting.OverrideChannelName.Value))
            {
                _messageQueue.Enqueue(new Chat.SimpleChatMessage
                {
                    baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS_CHANNEL_FALLBACK",
                    paramTokens = [Configs.ChatVoting.OverrideChannelName.Value]
                });
            }

            _messageQueue.Enqueue(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS",
                paramTokens = [client.ConnectedToChannel]
            });
        }

        void onChannelChatMessage(object sender, ChannelChatMessageEvent messageEvent)
        {
            if (string.IsNullOrEmpty(messageEvent.ChatterUserId) || messageEvent.MessageData == null)
                return;

            processVoteMessage(messageEvent.ChatterUserId, messageEvent.MessageData.FullText);
        }

        void onChannelChatNotification(object sender, ChannelChatNotificationEvent notificationEvent)
        {
            if (notificationEvent.AnonymousChatter || string.IsNullOrEmpty(notificationEvent.ChatterUserId) || notificationEvent.MessageData == null)
                return;

            processVoteMessage(notificationEvent.ChatterUserId, notificationEvent.MessageData.FullText);
        }

        void onTokenAccessRevoked(object sender, TokenAccessRevokedEventData e)
        {
            string reason = e.Status switch
            {
                "user_removed" => Language.GetString("TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_USER_REMOVED"),
                "authorization_revoked" => Language.GetString("TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_AUTHORIZATION_REVOKED"),
                "version_removed" => Language.GetString("TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_VERSION_REMOVED"),
                _ => e.Status,
            };

            _messageQueue.Enqueue(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_CONNECTION_ERROR",
                paramTokens = [reason]
            });

            disconnectClient();
        }
    }
}
