using RoR2;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Clients;
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
                _client.Initialize(_loginCredentials.BuildConnectionCredentials());
                _client.RemoveChatCommandIdentifier('!');

                _client.OnConnectionError += onConnectionError;

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

        static void onConnectionError(object s, OnConnectionErrorArgs e)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_CONNECTION_ERROR",
                paramTokens = new string[] { e.Error.Message }
            });
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

        bool _hasAttemptedJoinChannel;

        string _joinedChannel;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_client == null)
            {
                createClient();
            }

            if (_client != null)
            {
                _client.OnJoinedChannel += onJoinedChannel;
                _client.OnMessageReceived += onMessageReceived;
            }

            _hasAttemptedJoinChannel = false;
        }

        protected override void Update()
        {
            base.Update();

            if (!_hasAttemptedJoinChannel && canDispatchEffects)
            {
                _hasAttemptedJoinChannel = true;

                if (_client.IsConnected)
                {
                    _client.JoinChannel(_loginCredentials.Username);
                }
                else
                {
                    _client.OnConnected += onConnected;
                }
            }
        }

        void onConnected(object sender, OnConnectedArgs e)
        {
            if (sender is TwitchClient client)
            {
                client.JoinChannel(_loginCredentials.Username);
                client.OnConnected -= onConnected;
            }
        }

        void onJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS",
                paramTokens = new string[] { e.Channel }
            });

            _joinedChannel = e.Channel;
        }

        void onMessageReceived(object s, OnMessageReceivedArgs e)
        {
            processVoteMessage(e.ChatMessage.UserId, e.ChatMessage.Message);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_client != null)
            {
                _client.OnConnected -= onConnected;
                _client.OnJoinedChannel -= onJoinedChannel;
                _client.OnMessageReceived -= onMessageReceived;
            }

            if (_joinedChannel != null)
            {
                _client.LeaveChannel(_joinedChannel);
                _joinedChannel = null;
            }
        }
    }
}
