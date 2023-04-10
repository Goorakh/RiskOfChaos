using RoR2;
using System;
using System.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
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

            _loginCredentials = new TwitchLoginCredentials(args[0], args[1]);
            _loginCredentials.WriteToFile();
        }

        TwitchClient _client;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_loginCredentials.IsValid())
            {
                ConnectionCredentials credentials = new ConnectionCredentials(_loginCredentials.Username, _loginCredentials.OAuth);

                WebSocketClient socketClient = new WebSocketClient();

                _client = new TwitchClient(socketClient, ClientProtocol.WebSocket, new BepInExLogger<TwitchClient>());
                _client.Initialize(credentials, _loginCredentials.Username);
                _client.RemoveChatCommandIdentifier('!');

                _client.OnMessageReceived += onMessageReceived;

                _client.OnConnected += onConnected;

                _client.OnConnectionError += onConnectionError;

                _client.Connect();

                OnVotingStarted += onVotingStarted;
            }
            else
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT",
                    paramTokens = new string[] { "TWITCH_LOGIN_FAIL_NOT_LOGGED_IN" }
                });
            }
        }

        void onMessageReceived(object s, OnMessageReceivedArgs e)
        {
            onChatMessageReceived(e.ChatMessage.UserId, e.ChatMessage.Message);
        }

        static void onConnected(object s, OnConnectedArgs e)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS",
                paramTokens = new string[] { _loginCredentials.Username }
            });
        }

        static void onConnectionError(object s, OnConnectionErrorArgs e)
        {
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
                foreach (JoinedChannel channel in _client.JoinedChannels)
                {
                    _client.LeaveChannel(channel);
                }

                _client.Disconnect();

                _client.OnMessageReceived -= onMessageReceived;
                _client.OnConnected -= onConnected;
                _client.OnConnectionError -= onConnectionError;

                // TwitchClient keeps auto-reconnecting, so this crime needs to be done.
                _client.OnConnected += (s, e) =>
                {
                    ((TwitchClient)s).Disconnect();
                };

                _client = null;
            }

            OnVotingStarted -= onVotingStarted;
        }

        void onVotingStarted()
        {
            if (_client == null)
                return;

            JoinedChannel channel = _client.JoinedChannels.FirstOrDefault();
            if (channel == null)
            {
                Log.Warning("No joined channel");
                return;
            }

            for (int i = 0; i < _effectVoteSelection.NumOptions; i++)
            {
                if (_effectVoteSelection.TryGetOption(i, out VoteSelection<EffectVoteHolder>.VoteOption voteOption))
                {
                    _client.SendMessage(channel, voteOption.ToString());
                }
            }
        }
    }
}
