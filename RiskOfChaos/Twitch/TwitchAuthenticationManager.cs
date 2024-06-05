using RiskOfChaos.Utilities;
using RiskOfTwitch;
using RiskOfTwitch.User;
using RoR2;
using RoR2.UI;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RiskOfChaos.Twitch
{
    static class TwitchAuthenticationManager
    {
        public static readonly string[] ScopesArray = ["user:read:chat"];

        public static readonly string CombinedScopes = string.Join(" ", ScopesArray);

        static TwitchUserAccessToken _currentAccessToken = TwitchUserAccessToken.Empty;
        public static TwitchUserAccessToken CurrentAccessToken
        {
            get
            {
                return _currentAccessToken;
            }
            private set
            {
                if (_currentAccessToken == value)
                    return;

                _currentAccessToken = value;
                OnAccessTokenChanged?.Invoke();
            }
        }

        public static event Action OnAccessTokenChanged;

        static bool _isAuthenticatingToken;

        public static void SetTokenFromFile(TwitchUserAccessToken accessToken)
        {
            CurrentAccessToken = accessToken;

            string[] tokenScopes = accessToken.Scopes.Split(' ');
            bool missingScopes = !Array.TrueForAll(ScopesArray, s => Array.Exists(tokenScopes, ts => string.Equals(ts, s)));

            Task<AuthenticationTokenValidationResponse> validateTokenTask = Task.Run(async () =>
            {
                return await Authentication.GetAccessTokenValidationAsync(accessToken.Token).ConfigureAwait(false);
            });

            validateTokenTask.ContinueWith(validateTask =>
            {
                if (validateTask.Status == TaskStatus.Canceled)
                    return;

                if (validateTask.Exception != null)
                {
                    Log.Error($"Token validation failed: {validateTask.Exception}");
                }
                else if (validateTask.Status == TaskStatus.RanToCompletion)
                {
                    AuthenticationTokenValidationResponse validationResponse = validateTask.Result;

                    TimeStamp tokenExpiryDate = validationResponse.ExpiryDate;

                    if (tokenExpiryDate.HasPassed || tokenExpiryDate.TimeUntil.TotalMinutes < 10)
                    {
                        PopupAlertQueue.EnqueueAlert(dialogBox =>
                        {
                            dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_EXPIRED_HEADER");
                            dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_EXPIRED_DESCRIPTION");

                            dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                        });

                        CurrentAccessToken = TwitchUserAccessToken.Empty;

                        return;
                    }
                    else if (tokenExpiryDate.TimeUntil.TotalHours <= 12)
                    {
                        PopupAlertQueue.EnqueueAlert(dialogBox =>
                        {
                            // Not really correct, but makes sure "0 hour(s)" is not displayed
                            double hoursRemaining = Math.Max(tokenExpiryDate.TimeUntil.TotalHours, 1);

                            dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_ABOUT_TO_EXPIRE_HEADER");
                            dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_ABOUT_TO_EXPIRE_DESCRIPTION", hoursRemaining.ToString("0"));

                            dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                        });

                        CurrentAccessToken = TwitchUserAccessToken.Empty;

                        return;
                    }
                    else
                    {
#if DEBUG
                        Log.Debug($"Stored token expires in {tokenExpiryDate.TimeUntil:g}");
#endif
                    }
                }

                if (missingScopes)
                {
                    PopupAlertQueue.EnqueueAlert(dialogBox =>
                    {
                        dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_MISSING_SCOPES_HEADER");
                        dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_MISSING_SCOPES_DESCRIPTION");

                        dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                    });

                    CurrentAccessToken = TwitchUserAccessToken.Empty;
                }
            }, UnityUpdateTaskScheduler.Instance);
        }

        public static void AuthenticateNewToken()
        {
            if (_isAuthenticatingToken)
            {
                Log.Warning("Already authenticating!");
                return;
            }

            _isAuthenticatingToken = true;

            SimpleDialogBox authenticatingDialog = SimpleDialogBox.Create();
            authenticatingDialog.headerToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATING_HEADER");
            authenticatingDialog.descriptionToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATING_DESCRIPTION");

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

            authenticatingDialog.AddActionButton(() =>
            {
                try
                {
                    cancelTokenSource.Cancel();
                }
                catch (AggregateException e)
                {
                    Log.Error_NoCallerPrefix(e);
                }
            }, CommonLanguageTokens.cancel);

            Task<AuthenticationResult> authenticateTokenTask = Task.Run(async () =>
            {
                string token = await Authentication.AuthenticateUserAccessToken(CombinedScopes, cancelTokenSource.Token).ConfigureAwait(false);

                TwitchUserData user = null;

                AuthenticationTokenValidationResponse validationResponse = await Authentication.GetAccessTokenValidationAsync(token, cancelTokenSource.Token).ConfigureAwait(false);
                if (validationResponse != null)
                {
                    GetUsersResponse getUsersResponse = await StaticTwitchAPI.GetUsers(token, [validationResponse.UserID], [], cancelTokenSource.Token).ConfigureAwait(false);
                    if (getUsersResponse != null && getUsersResponse.Users.Length > 0)
                    {
                        user = getUsersResponse.Users[0];
                    }
                }

                return new AuthenticationResult(token, user);
            }, cancelTokenSource.Token);

            authenticateTokenTask.ContinueWith(task =>
            {
                _isAuthenticatingToken = false;

                GameObject.Destroy(authenticatingDialog.rootObject);

                TwitchUserAccessToken accessToken = TwitchUserAccessToken.Empty;
                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
                        accessToken = new TwitchUserAccessToken(CombinedScopes, task.Result.Token);

                        SimpleDialogBox authenticationCompleteDialog = SimpleDialogBox.Create();

                        authenticationCompleteDialog.headerToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATED_HEADER");
                        authenticationCompleteDialog.descriptionToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATED_DESCRIPTION", task.Result.User?.UserDisplayName);

                        authenticationCompleteDialog.AddCancelButton(CommonLanguageTokens.ok);
                        break;
                    case TaskStatus.Canceled:
                        accessToken = CurrentAccessToken;
                        break;
                    case TaskStatus.Faulted:
                        Log.Error_NoCallerPrefix(task.Exception);

                        SimpleDialogBox errorDialog = SimpleDialogBox.Create();

                        errorDialog.headerToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATION_ERROR_HEADER");
                        errorDialog.descriptionToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATION_ERROR_DESCRIPTION");

                        errorDialog.AddCancelButton(CommonLanguageTokens.ok);
                        break;
                    default:
                        Log.Error($"Unexpected task status {task.Status}");
                        break;
                }

                CurrentAccessToken = accessToken;
            }, UnityUpdateTaskScheduler.Instance);
        }

        readonly record struct AuthenticationResult(string Token, TwitchUserData User);
    }
}
