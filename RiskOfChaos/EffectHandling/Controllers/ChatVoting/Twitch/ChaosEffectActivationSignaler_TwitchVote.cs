using RoR2;
using System;
using System.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch
{
    [ChaosEffectActivationSignaler(Configs.ChatVoting.ChatVotingMode.Twitch)]
    public class ChaosEffectActivationSignaler_TwitchVote : ChaosEffectActivationSignaler_ChatVote
    {
        static TwitchLoginCredentials _loginCredentials = TwitchLoginCredentials.TryReadFromFile();

        [ConCommand(commandName = "roc_twitch_login")]
        static void CCLogin(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                if (_loginCredentials.IsValid())
                {
                    Debug.Log($"Logged in as {_loginCredentials.Username}");
                }
                else
                {
                    Debug.Log("Not currently logged in");
                }

                Debug.Log("Command usage: roc_twitch_login [username] [oauth]");
                return;
            }

            args.CheckArgumentCount(2);

            TwitchLoginCredentials newLoginCredentials = new TwitchLoginCredentials(args[0], args[1]);
            if (_loginCredentials != newLoginCredentials)
            {
                _loginCredentials = newLoginCredentials;
                _loginCredentials.WriteToFile();

                onClientCredentialsChanged();
            }
        }

        static TwitchClient _client;
        static void createClient()
        {
            if (_loginCredentials.IsValid())
            {
                WebSocketClient socketClient = new WebSocketClient();

                _client = new TwitchClient(socketClient, ClientProtocol.WebSocket, new BepInExLogger<TwitchClient>());
                _client.Initialize(_loginCredentials.BuildConnectionCredentials(), _loginCredentials.Username);
                _client.RemoveChatCommandIdentifier('!');

                if (!_client.Connect())
                {
                    Log.Warning("Twitch client failed to connect");
                }
            }
            else if (Run.instance)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT",
                    paramTokens = new string[] { "TWITCH_LOGIN_FAIL_NOT_LOGGED_IN" }
                });
            }
        }

        static void onClientCredentialsChanged()
        {
            if (_client == null)
                return;

            bool wasConnected = false;
            if (_client.IsConnected)
            {
                _client.Disconnect();
                wasConnected = true;
            }

            _client.SetConnectionCredentials(_loginCredentials.BuildConnectionCredentials());

            if (wasConnected)
            {
                _client.Reconnect();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_client == null)
            {
                createClient();
            }
            else if (_client.IsConnected)
            {
                onTwitchClientConnected();
            }

            _client.OnConnected += onConnected;
            _client.OnConnectionError += onConnectionError;
            _client.OnDisconnected += onDisconnected;
        }

        void onMessageReceived(object s, OnMessageReceivedArgs e)
        {
            onChatMessageReceived(e.ChatMessage.UserId, e.ChatMessage.Message);
        }

        void onConnected(object s, OnConnectedArgs e)
        {
            onTwitchClientConnected();
        }

        void onDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            _client.OnMessageReceived -= onMessageReceived;
        }

        void onTwitchClientConnected()
        {
            _client.OnMessageReceived += onMessageReceived;

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS",
                paramTokens = new string[] { _loginCredentials.Username }
            });
        }

        void onConnectionError(object s, OnConnectionErrorArgs e)
        {
            _client.OnMessageReceived -= onMessageReceived;

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_CONNECTION_ERROR",
                paramTokens = new string[] { e.Error.Message }
            });
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_client != null)
            {
                _client.OnMessageReceived -= onMessageReceived;
                _client.OnConnected -= onConnected;
                _client.OnConnectionError -= onConnectionError;
            }
        }
    }
}
