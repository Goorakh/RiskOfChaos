using RoR2;
using RoR2.UI;
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

        [SystemInitializer]
        static void Init()
        {
            Configs.ChatVoting.OnReconnectButtonPressed += () =>
            {
                if (Configs.ChatVoting.VotingMode == Configs.ChatVoting.ChatVotingMode.Twitch && !_loginCredentials.IsValid())
                {
                    SimpleDialogBox notLoggedInDialog = SimpleDialogBox.Create();

                    notLoggedInDialog.headerToken = new SimpleDialogBox.TokenParamsPair("ROC_ATTEMPT_RECONNECT_NOT_LOGGED_IN_HEADER");
                    notLoggedInDialog.descriptionToken = new SimpleDialogBox.TokenParamsPair("ROC_ATTEMPT_RECONNECT_NOT_LOGGED_IN_DESCRIPTION_TWITCH");

                    notLoggedInDialog.AddCancelButton(CommonLanguageTokens.ok);
                }
            };
        }

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

        float _scheduledAttemptJoinChannelTime;
        bool _channelJoinAttemptScheduled;

        void scheduleAttemptJoinChannel(float waitTime)
        {
            _channelJoinAttemptScheduled = true;
            _scheduledAttemptJoinChannelTime = Time.time + waitTime;
        }

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

            scheduleAttemptJoinChannel(1.5f);

            Configs.ChatVoting.OnReconnectButtonPressed += onReconnectButtonPressed;
        }

        protected override void Update()
        {
            base.Update();

            if (_channelJoinAttemptScheduled && Time.time >= _scheduledAttemptJoinChannelTime)
            {
                _channelJoinAttemptScheduled = false;

                if (!_loginCredentials.IsValid())
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT",
                        paramTokens = new string[] { Language.GetString("TWITCH_LOGIN_FAIL_NOT_LOGGED_IN") }
                    });

                    scheduleAttemptJoinChannel(5f);
                }
                else if (_client != null)
                {
                    if (_client.IsConnected)
                    {
                        _client.JoinChannel(_loginCredentials.Username);
                    }
                    else
                    {
                        scheduleAttemptJoinChannel(2f);
                    }
                }
            }
        }

        void onReconnectButtonPressed()
        {
            if (_client == null)
                return;

            string channel = _joinedChannel;
            if (channel != null)
            {
                _client.LeaveChannel(channel);

                scheduleAttemptJoinChannel(1f);
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
                _client.OnJoinedChannel -= onJoinedChannel;
                _client.OnMessageReceived -= onMessageReceived;
            }

            if (_joinedChannel != null)
            {
                _client.LeaveChannel(_joinedChannel);
                _joinedChannel = null;
            }

            Configs.ChatVoting.OnReconnectButtonPressed -= onReconnectButtonPressed;
        }
    }
}
