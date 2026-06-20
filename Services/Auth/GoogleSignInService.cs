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

/// <summary>
/// Implements native Google Sign-In on Android, obtaining the ID token
/// and exchanging it with the Firebase Auth REST API.
/// </summary>
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
        var tcs = new TaskCompletionSource<GoogleSignInResult>();

        Log($"Configured Client ID: {AppConfig.GoogleWebClientId}");

        // Build Google Sign-In options with the Web Client ID
        // This requests an ID token that can be verified by Firebase
        var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestIdToken(AppConfig.GoogleWebClientId)
            .RequestEmail()
            .RequestProfile()
            .Build();

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null)
        {
            Log("Error: Android Activity is null!");
            throw new Exception("Android Activity is not available.");
        }

        Log("Creating GoogleSignIn client...");
        var client = GoogleSignIn.GetClient(activity, gso);

        // Sign out first to ensure the account selection prompt appears
        try
        {
            Log("Signing out of previous session...");
            await client.SignOutAsync();
            Log("Sign out complete.");
        }
        catch (Exception ex)
        {
            Log($"Sign out warning: {ex.Message}");
        }

        const int RequestCode = 9001;

        Action<int, Result, Intent?>? handler = null;
        handler = async (reqCode, resultCode, intentData) =>
        {
            Log($"ActivityResult handler triggered. Request: {reqCode}, Result: {resultCode}");
            if (reqCode == RequestCode)
            {
                // Unsubscribe from the event
                MainActivity.ActivityResult -= handler;
                Log("Unsubscribed from MainActivity.ActivityResult.");

                if (resultCode == Result.Canceled)
                {
                    Log("User cancelled Google Sign-In (Result.Canceled).");
                    if (intentData != null)
                    {
                        try
                        {
                            var task = GoogleSignIn.GetSignedInAccountFromIntent(intentData);
                            if (!task.IsSuccessful && task.Exception != null)
                            {
                                var taskEx = task.Exception;
                                Log($"Exception details during cancellation: {taskEx.GetType().Name}: {taskEx.Message}");
                                if (taskEx is Android.Gms.Common.Apis.ApiException apiEx)
                                {
                                    Log($"API Exception Status Code: {apiEx.StatusCode} (Common: 12500=Sign in failed, 12501=Sign in cancelled, 10=Developer error/SHA-1 mismatch)");
                                    tcs.TrySetException(new Exception($"Google Sign-In canceled with Status Code: {apiEx.StatusCode}."));
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to parse cancellation intent data: {ex.Message}");
                        }
                    }
                    tcs.TrySetCanceled();
                    return;
                }

                try
                {
                    if (intentData == null)
                    {
                        Log("Error: intentData is null!");
                        tcs.TrySetException(new Exception("No data received from Google Sign-In."));
                        return;
                    }

                    Log("Parsing account from intent data...");
                    var task = GoogleSignIn.GetSignedInAccountFromIntent(intentData);
                    Log($"Task returned. IsComplete: {task.IsComplete}, IsSuccessful: {task.IsSuccessful}");

                    if (!task.IsSuccessful)
                    {
                        var taskEx = task.Exception;
                        var errorMsg = taskEx != null ? $"{taskEx.GetType().Name}: {taskEx.Message}" : "Unknown task error";
                        Log($"Google Sign-In Task failed: {errorMsg}");
                        
                        if (taskEx is Android.Gms.Common.Apis.ApiException apiEx)
                        {
                            Log($"API Exception Status Code: {apiEx.StatusCode}");
                        }
                        
                        tcs.TrySetException(new Exception($"Google Sign-In task failed: {errorMsg}"));
                        return;
                    }

                    var account = task.Result as GoogleSignInAccount;
                    if (account == null)
                    {
                        Log("Error: GoogleSignInAccount is null!");
                        tcs.TrySetException(new Exception("Google account data is null."));
                        return;
                    }

                    Log($"Google Account details retrieved: Email={account.Email}, DisplayName={account.DisplayName}");

                    var idToken = account.IdToken;
                    if (string.IsNullOrEmpty(idToken))
                    {
                        Log("Error: Google ID Token is null or empty!");
                        tcs.TrySetException(new Exception("Failed to retrieve Google ID Token. (Likely SHA-1 fingerprint mismatch in Firebase console)"));
                        return;
                    }

                    Log($"Google ID Token retrieved successfully. Length: {idToken.Length}");

                    Log("Exchanging Google ID Token with Firebase...");
                    var result = await SignInWithGoogleIdTokenAsync(idToken);
                    Log("Firebase exchange successful!");
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    Log($"Exception inside activity result handler: {ex.Message}");
                    tcs.TrySetException(new Exception($"Google Sign-In failed: {ex.Message}"));
                }
            }
            else
            {
                Log($"Ignoring ActivityResult with RequestCode: {reqCode}");
            }
        };

        MainActivity.ActivityResult += handler;
        Log("Subscribed to MainActivity.ActivityResult.");

        try
        {
            Log("Launching Google Sign-In activity...");
            activity.StartActivityForResult(client.SignInIntent, RequestCode);
            Log("Activity launched successfully.");
        }
        catch (Exception ex)
        {
            Log($"Failed to launch Google Sign-In: {ex.Message}");
            MainActivity.ActivityResult -= handler;
            throw new Exception($"Failed to launch Google Sign-In screen: {ex.Message}");
        }

        return await tcs.Task;
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

        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signInWithIdp?key={AppConfig.FirebaseWebApiKey}";
        Log($"Firebase Request URL: {url}");

        var response = await _http.PostAsJsonAsync(url, payload);
        var json = await response.Content.ReadAsStringAsync();
        Log($"Firebase Response status: {response.StatusCode}");

        var doc = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = doc.RootElement
                .TryGetProperty("error", out var err)
                ? err.GetProperty("message").GetString() ?? "Sign-in failed"
                : "Google Sign-In failed";
            Log($"Firebase exchange error: {errorMsg}");
            throw new Exception(errorMsg);
        }

        var root = doc.RootElement;
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
