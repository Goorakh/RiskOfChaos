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

        // Twitch requires that access tokens are validated at least hourly
        const float TOKEN_AUTHENTICATION_PERIOD = 60f * 50f;
        static float _tokenAuthenticationTimer = TOKEN_AUTHENTICATION_PERIOD;

        [SystemInitializer]
        static void Init()
        {
            RoR2Application.onUpdate += RoR2Application_onUpdate;
        }

        static void RoR2Application_onUpdate()
        {
            _tokenAuthenticationTimer -= Time.unscaledDeltaTime;
            if (_tokenAuthenticationTimer <= 0f)
            {
                _tokenAuthenticationTimer += TOKEN_AUTHENTICATION_PERIOD;

                if (!CurrentAccessToken.IsEmpty)
                {
                    validateToken();
                }
            }
        }

        static void validateToken()
        {
#if DEBUG
            Log.Debug("Validating current token");
#endif

            Task.Run(async () =>
            {
                Result<AuthenticationTokenValidationResponse> validationResult = await Authentication.GetAccessTokenValidationAsync(CurrentAccessToken.Token);

                // EventSub will notify us if the token is invalidated, and currently all API access relies on an EventSub connection
                // So there's not really anything to do here :)

                // A sudden popup about your access token when you're not playing with Twitch integration would probably be annoying anyway
            });
        }

        public static void SetTokenFromFile(TwitchUserAccessToken accessToken)
        {
            CurrentAccessToken = accessToken;

            string[] tokenScopes = accessToken.Scopes.Split(' ');
            bool missingScopes = !Array.TrueForAll(ScopesArray, s => Array.Exists(tokenScopes, ts => string.Equals(ts, s)));

            Task<Result<AuthenticationTokenValidationResponse>> validateTokenTask = Task.Run(async () =>
            {
                return await Authentication.GetAccessTokenValidationAsync(accessToken.Token).ConfigureAwait(false);
            });

            validateTokenTask.ContinueWith(validateTask =>
            {
                if (validateTask.Status == TaskStatus.Canceled)
                    return;

                Exception validationException = null;
                DateTimeStamp? tokenExpiryDate = null;

                if (validateTask.Exception != null || validateTask.Status != TaskStatus.RanToCompletion)
                {
                    validationException = validateTask.Exception ?? new Exception("Token validation did not complete");
                }
                else
                {
                    Result<AuthenticationTokenValidationResponse> validationResult = validateTask.Result;
                    if (!validationResult.IsSuccess)
                    {
                        validationException = validationResult.Exception;
                    }
                    else
                    {
                        AuthenticationTokenValidationResponse validationResponse = validationResult.Value;

                        tokenExpiryDate = validationResponse.ExpiryDate;
                    }
                }

                bool invalidateToken = false;
                if (!invalidateToken && validationException != null)
                {
                    if (validationException is InvalidAccessTokenException)
                    {
                        PopupAlertQueue.EnqueueAlert(dialogBox =>
                        {
                            dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_EXPIRED_HEADER");
                            dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_EXPIRED_DESCRIPTION");

                            dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                        });
                    }
                    else
                    {
                        Log.Error($"Token validation failed: {validationException}");

                        PopupAlertQueue.EnqueueAlert(dialogBox =>
                        {
                            dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_LOGIN_VALIDATION_FAILED_GENERIC_HEADER");
                            dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_LOGIN_VALIDATION_FAILED_GENERIC_DESCRIPTION");

                            dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                        });
                    }

                    invalidateToken = true;
                }

                if (!invalidateToken && tokenExpiryDate.HasValue)
                {
                    TimeSpan timeUntilExpired = tokenExpiryDate.Value.TimeUntil;
                    if (timeUntilExpired.TotalMinutes < 10)
                    {
                        PopupAlertQueue.EnqueueAlert(dialogBox =>
                        {
                            dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_EXPIRED_HEADER");
                            dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_EXPIRED_DESCRIPTION");

                            dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                        });

                        invalidateToken = true;
                    }
                    else
                    {
                        if (timeUntilExpired.TotalHours <= 12)
                        {
                            PopupAlertQueue.EnqueueAlert(dialogBox =>
                            {
                                // Not really correct, but makes sure "0 hour(s)" is not displayed
                                double hoursRemaining = Math.Max(tokenExpiryDate.Value.TimeUntil.TotalHours, 1);

                                dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_ABOUT_TO_EXPIRE_HEADER");
                                dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_ABOUT_TO_EXPIRE_DESCRIPTION", hoursRemaining.ToString("0"));

                                dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                            });
                        }
                        else
                        {
#if DEBUG
                            Log.Debug($"Stored token expires in {timeUntilExpired:g}");
#endif
                        }
                    }
                }

                if (!invalidateToken && missingScopes)
                {
                    PopupAlertQueue.EnqueueAlert(dialogBox =>
                    {
                        dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_MISSING_SCOPES_HEADER");
                        dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_TWITCH_USER_TOKEN_MISSING_SCOPES_DESCRIPTION");

                        dialogBox.AddCancelButton(CommonLanguageTokens.ok);
                    });

                    invalidateToken = true;
                }

                if (invalidateToken)
                {
                    CurrentAccessToken = TwitchUserAccessToken.Empty;
                }
            }, UnityUpdateTaskScheduler.Instance);
        }

        readonly record struct AuthenticationResult(string Token, TwitchUserData User);
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

            Task<Result<AuthenticationResult>> authenticateTokenTask = Task.Run(async () =>
            {
                Result<string> tokenResult = await Authentication.AuthenticateUserAccessToken(CombinedScopes, cancelTokenSource.Token).ConfigureAwait(false);
                if (!tokenResult.IsSuccess)
                {
                    return new Result<AuthenticationResult>(tokenResult.Exception);
                }

                string token = tokenResult.Value;

                Result<AuthenticationTokenValidationResponse> validationResult = await Authentication.GetAccessTokenValidationAsync(token, cancelTokenSource.Token).ConfigureAwait(false);
                if (!validationResult.IsSuccess)
                {
                    return new Result<AuthenticationResult>(validationResult.Exception);
                }

                AuthenticationTokenValidationResponse validationResponse = validationResult.Value;

                Result<GetUsersResponse> getUsersResult = await StaticTwitchAPI.GetUsers(token, [validationResponse.UserID], [], cancelTokenSource.Token).ConfigureAwait(false);
                if (!getUsersResult.IsSuccess)
                {
                    return new Result<AuthenticationResult>(getUsersResult.Exception);
                }

                GetUsersResponse getUsersResponse = getUsersResult.Value;
                if (getUsersResponse.Users.Length == 0)
                {
                    return new Result<AuthenticationResult>(new Exception("No user data was returned"));
                }

                return new AuthenticationResult(token, getUsersResponse.Users[0]);
            }, cancelTokenSource.Token);

            authenticateTokenTask.ContinueWith(task =>
            {
                _isAuthenticatingToken = false;

                GameObject.Destroy(authenticatingDialog.rootObject);

                if (task.Status == TaskStatus.Canceled)
                    return;

                Exception exception = null;
                TwitchUserAccessToken accessToken;

                if (task.Exception != null || task.Status != TaskStatus.RanToCompletion)
                {
                    exception = task.Exception ?? new Exception("Authentication did not complete");
                }
                else if (!task.Result.IsSuccess)
                {
                    exception = task.Result.Exception;
                }

                if (exception != null)
                {
                    Log.Error_NoCallerPrefix($"Error authenticating token: {exception}");

                    SimpleDialogBox errorDialog = SimpleDialogBox.Create();

                    errorDialog.headerToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATION_ERROR_HEADER");
                    errorDialog.descriptionToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATION_ERROR_DESCRIPTION");

                    errorDialog.AddCancelButton(CommonLanguageTokens.ok);

                    accessToken = TwitchUserAccessToken.Empty;
                }
                else
                {
                    AuthenticationResult authenticationResult = task.Result.Value;

                    accessToken = new TwitchUserAccessToken(CombinedScopes, authenticationResult.Token);

                    SimpleDialogBox authenticationCompleteDialog = SimpleDialogBox.Create();

                    authenticationCompleteDialog.headerToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATED_HEADER");
                    authenticationCompleteDialog.descriptionToken = new SimpleDialogBox.TokenParamsPair("TWITCH_USER_TOKEN_AUTHENTICATED_DESCRIPTION", authenticationResult.User?.UserDisplayName);

                    authenticationCompleteDialog.AddCancelButton(CommonLanguageTokens.ok);
                }

                CurrentAccessToken = accessToken;
            }, UnityUpdateTaskScheduler.Instance);
        }
    }
}
