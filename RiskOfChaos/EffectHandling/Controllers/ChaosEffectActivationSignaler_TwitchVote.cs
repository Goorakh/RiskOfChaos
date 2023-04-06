using RoR2;
using System;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosEffectActivationSignaler(Configs.ChatVoting.ChatVotingMode.Twitch)]
    public class ChaosEffectActivationSignaler_TwitchVote : ChaosEffectActivationSignaler_ChatVote
    {
        static readonly ClientOptions _clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        static string _username;
        static string Username
        {
            get => _username;
            set
            {
                _username = value;
            }
        }

        static string _oauth;
        static string OAuth
        {
            get => _oauth;
            set
            {
                const string OAUTH_PREFIX = "oauth:";
                if (value.StartsWith(OAUTH_PREFIX))
                {
                    value = value.Substring(OAUTH_PREFIX.Length);
                }

                _oauth = value;
            }
        }

        static bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(OAuth);
        }

        [ConCommand(commandName = "roc_twitch_login")]
        static void CCLogin(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                if (IsLoggedIn())
                {
                    Debug.Log($"Logged in as {Username}");
                }
                else
                {
                    Debug.Log("Not currently logged in");
                }

                Debug.Log("Command usage: roc_twitch_login [username] [oauth]");
                return;
            }

            args.CheckArgumentCount(2);

            Username = args[0];
            OAuth = args[1];
        }

        public override event IChaosEffectActivationSignaler.SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public override void SkipAllScheduledEffects()
        {
        }

        TwitchClient _client;

        void OnEnable()
        {
            if (!IsLoggedIn())
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT",
                    paramTokens = new string[] { "TWITCH_LOGIN_FAIL_NOT_LOGGED_IN" }
                });

                return;
            }

            ConnectionCredentials credentials = new ConnectionCredentials(Username, OAuth);

            WebSocketClient socketClient = new WebSocketClient(_clientOptions);

            _client = new TwitchClient(socketClient, ClientProtocol.WebSocket, new BepInExLogger<TwitchClient>());
            _client.Initialize(credentials, Username);
            _client.RemoveChatCommandIdentifier('!');

            _client.OnMessageReceived += (s, e) =>
            {
                onChatMessageReceived(e.ChatMessage.UserId, e.ChatMessage.Message);
            };

            _client.OnMessageSent += onChatMessageSent;

            _client.OnConnected += onConnected;

            _client.Connect();
        }

        void onConnected(object sender, OnConnectedArgs e)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS",
                paramTokens = new string[] { Username }
            });

            _client.SendMessage(e.AutoJoinChannel, "work please???");
        }

        void onChatMessageSent(object sender, OnMessageSentArgs e)
        {
            Log.Debug(e.SentMessage.Message);
        }

        void OnDisable()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client = null;
            }
        }
    }
}
