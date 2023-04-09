using RoR2;
using System;
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

                _client.OnMessageReceived += (s, e) =>
                {
                    onChatMessageReceived(e.ChatMessage.UserId, e.ChatMessage.Message);
                };

                _client.OnConnected += (s, e) =>
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS",
                        paramTokens = new string[] { _loginCredentials.Username }
                    });
                };

                _client.OnConnectionError += (s, e) =>
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "TWITCH_EFFECT_VOTING_CONNECTION_ERROR",
                        paramTokens = new string[] { e.Error.Message }
                    });
                };

                _client.Connect();
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

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_client != null)
            {
                _client.Disconnect();
                _client = null;
            }
        }
    }
}
