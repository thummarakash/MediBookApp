using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using MediBook.Configuration;

#if ANDROID
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Api;
using Android.Util;
#endif

namespace MediBook.Services.Auth;

public class GoogleSignInService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(AppConfig.HttpTimeoutSeconds) };
    private const string LogTag = "MediBookAuth";

    public static GoogleSignInService Instance { get; } = new();
    private GoogleSignInService() { }

    private void Log(string message)
    {
        Debug.WriteLine($"[{LogTag}] {message}");
#if ANDROID
        Android.Util.Log.Info(LogTag, message);
#endif
    }

    public async Task<GoogleSignInResult> SignInAsync()
    {
        Log("SignInAsync called.");
#if ANDROID
        var signInTcs = new TaskCompletionSource<GoogleSignInResult>();

        Log($"Configured Client ID: {AppConfig.GoogleWebClientId}");

        var signInOptions = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestIdToken(AppConfig.GoogleWebClientId)
            .RequestEmail()
            .RequestProfile()
            .Build();

        var currentActivity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (currentActivity == null)
        {
            Log("Error: Android Activity is null!");
            throw new Exception("Android Activity is not available.");
        }

        Log("Creating GoogleSignIn client...");
        var signInClient = GoogleSignIn.GetClient(currentActivity, signInOptions);

        try
        {
            Log("Signing out of previous session...");
            await signInClient.SignOutAsync();
            Log("Sign out complete.");
        }
        catch (Exception ex)
        {
            Log($"Could not sign out prior google session (non-fatal): {ex.Message}");
        }

        const int SignInRequestCode = 9001;

        Action<int, Result, Intent?>? onActivityResult = null;
        onActivityResult = async (reqCode, resultCode, intentData) =>
        {
            Log($"ActivityResult handler triggered. Request: {reqCode}, Result: {resultCode}");
            if (reqCode == SignInRequestCode)
            {
                MainActivity.ActivityResult -= onActivityResult;
                Log("Unsubscribed from MainActivity.ActivityResult.");

                if (resultCode == Result.Canceled)
                {
                    Log("User cancelled Google Sign-In (Result.Canceled).");
                    if (intentData != null)
                    {
                        try
                        {
                            var cancelTask = GoogleSignIn.GetSignedInAccountFromIntent(intentData);
                            if (!cancelTask.IsSuccessful && cancelTask.Exception != null)
                            {
                                var cancelEx = cancelTask.Exception;
                                Log($"Exception details during cancellation: {cancelEx.GetType().Name}: {cancelEx.Message}");
                                if (cancelEx is Android.Gms.Common.Apis.ApiException apiEx)
                                {
                                    Log($"API Exception Status Code: {apiEx.StatusCode}");
                                    string friendlyError = apiEx.StatusCode switch
                                    {
                                        7 => "Network connection failed. Check your internet access and try again.",
                                        10 => "OAuth client config issue. Verify SHA-1 setup.",
                                        12501 => "Sign-in cancelled by user.",
                                        _ => $"Sign-in failed with error code: {apiEx.StatusCode}."
                                    };
                                    signInTcs.TrySetException(new Exception(friendlyError));
                                    return;
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            Log($"Failed to parse cancellation intent data: {parseEx.Message}");
                        }
                    }
                    signInTcs.TrySetCanceled();
                    return;
                }

                try
                {
                    if (intentData == null)
                    {
                        Log("Error: intentData is null!");
                        signInTcs.TrySetException(new Exception("No data received from Google Sign-In."));
                        return;
                    }

                    Log("Parsing account from intent data...");
                    var signInTask = GoogleSignIn.GetSignedInAccountFromIntent(intentData);
                    Log($"Task returned. IsComplete: {signInTask.IsComplete}, IsSuccessful: {signInTask.IsSuccessful}");

                    if (!signInTask.IsSuccessful)
                    {
                        var taskEx = signInTask.Exception;
                        var errorMsg = taskEx != null ? $"{taskEx.GetType().Name}: {taskEx.Message}" : "Unknown task error";
                        Log($"Google Sign-In Task failed: {errorMsg}");

                        if (taskEx is Android.Gms.Common.Apis.ApiException failedApiEx)
                        {
                            Log($"API Exception Status Code: {failedApiEx.StatusCode}");
                        }

                        signInTcs.TrySetException(new Exception($"Google Sign-In task failed: {errorMsg}"));
                        return;
                    }

                    var googleAccount = signInTask.Result as GoogleSignInAccount;
                    if (googleAccount == null)
                    {
                        Log("Error: GoogleSignInAccount is null!");
                        signInTcs.TrySetException(new Exception("Google account data is null."));
                        return;
                    }

                    Log($"Google Account details retrieved: Email={googleAccount.Email}, DisplayName={googleAccount.DisplayName}");

                    var idToken = googleAccount.IdToken;
                    if (string.IsNullOrEmpty(idToken))
                    {
                        Log("Error: Google ID Token is null or empty!");
                        signInTcs.TrySetException(new Exception("Failed to retrieve Google ID Token. (Likely SHA-1 fingerprint mismatch in Firebase console)"));
                        return;
                    }

                    Log($"Google ID Token retrieved successfully. Length: {idToken.Length}");
                    Log("Exchanging Google ID Token with Firebase...");

                    var firebaseCredentials = await SignInWithGoogleIdTokenAsync(idToken);
                    Log("Firebase exchange successful!");
                    signInTcs.TrySetResult(firebaseCredentials);
                }
                catch (Exception handlerEx)
                {
                    Log($"Exception inside activity result handler: {handlerEx.Message}");
                    signInTcs.TrySetException(new Exception($"Google Sign-In handler failed: {handlerEx.Message}"));
                }
            }
            else
            {
                Log($"Ignoring ActivityResult with RequestCode: {reqCode}");
            }
        };

        MainActivity.ActivityResult += onActivityResult;
        Log("Subscribed to MainActivity.ActivityResult.");

        try
        {
            Log("Launching Google Sign-In activity...");
            currentActivity.StartActivityForResult(signInClient.SignInIntent, SignInRequestCode);
            Log("Activity launched successfully.");
        }
        catch (Exception launchEx)
        {
            Log($"Failed to launch Google Sign-In: {launchEx.Message}");
            MainActivity.ActivityResult -= onActivityResult;
            throw new Exception($"Failed to launch Google Sign-In screen: {launchEx.Message}");
        }

        return await signInTcs.Task;
#else
        Log("Error: Platform is not Android!");
        throw new NotImplementedException("Google Sign-In is only implemented for Android in this project.");
#endif
    }

    private async Task<GoogleSignInResult> SignInWithGoogleIdTokenAsync(string googleIdToken)
    {
        var payload = new
        {
            postBody = $"id_token={googleIdToken}&providerId=google.com",
            requestUri = "http://localhost",
            returnIdpCredential = true,
            returnSecureToken = true
        };

        var targetUrl = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signInWithIdp?key={AppConfig.FirebaseWebApiKey}";
        Log($"Firebase Request URL: {targetUrl}");

        var httpResponse = await _http.PostAsJsonAsync(targetUrl, payload);
        var rawJson = await httpResponse.Content.ReadAsStringAsync();
        Log($"Firebase Response status: {httpResponse.StatusCode}");

        var jsonDocument = JsonDocument.Parse(rawJson);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorMsg = jsonDocument.RootElement
                .TryGetProperty("error", out var err)
                ? err.GetProperty("message").GetString() ?? "Sign-in failed"
                : "Google Sign-In failed";
            Log($"Firebase exchange error: {errorMsg}");
            throw new Exception(errorMsg);
        }

        var root = jsonDocument.RootElement;
        return new GoogleSignInResult
        {
            IdToken = root.TryGetProperty("idToken", out var tok) ? tok.GetString() ?? "" : "",
            RefreshToken = root.TryGetProperty("refreshToken", out var rt) ? rt.GetString() ?? "" : "",
            UserId = root.TryGetProperty("localId", out var uid) ? uid.GetString() ?? "" : "",
            Email = root.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
            DisplayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
            PhotoUrl = root.TryGetProperty("photoUrl", out var ph) ? ph.GetString() ?? "" : "",
            ExpiresIn = int.TryParse(
                root.TryGetProperty("expiresIn", out var exp) ? exp.GetString() : "3600", out var expVal)
                ? expVal : 3600,
            IsNewUser = root.TryGetProperty("isNewUser", out var nu) && nu.GetBoolean()
        };
    }
}

public class GoogleSignInResult
{
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 3600;
    public bool IsNewUser { get; set; }
}
