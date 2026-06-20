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
        var signin_tcs = new TaskCompletionSource<GoogleSignInResult>();

        Log($"Configured Client ID: {AppConfig.GoogleWebClientId}");

        // Build standard google signin options
        var cfg_opts = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestIdToken(AppConfig.GoogleWebClientId)
            .RequestEmail()
            .RequestProfile()
            .Build();

        var curr_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (curr_activity == null)
        {
            Log("Error: Android Activity is null!");
            throw new Exception("Android Activity is not available.");
        }

        Log("Creating GoogleSignIn client...");
        var signin_client = GoogleSignIn.GetClient(curr_activity, cfg_opts);

        try
        {
            Log("Signing out of previous session...");
            await signin_client.SignOutAsync();
            Log("Sign out complete.");
        }
        catch (Exception signOutErr)
        {
            // Minor warning, usually fine to ignore
            Log($"Could not sign out prior google session (non-fatal): {signOutErr.Message}");
        }

        const int SigninRequestCode = 9001;

        Action<int, Result, Intent?>? on_act_res = null;
        on_act_res = async (reqCode, resultCode, intentData) =>
        {
            Log($"ActivityResult handler triggered. Request: {reqCode}, Result: {resultCode}");
            if (reqCode == SigninRequestCode)
            {
                MainActivity.ActivityResult -= on_act_res;
                Log("Unsubscribed from MainActivity.ActivityResult.");

                if (resultCode == Result.Canceled)
                {
                    Log("User cancelled Google Sign-In (Result.Canceled).");
                    if (intentData != null)
                    {
                        try
                        {
                            var cancel_task = GoogleSignIn.GetSignedInAccountFromIntent(intentData);
                            if (!cancel_task.IsSuccessful && cancel_task.Exception != null)
                            {
                                var cancel_ex = cancel_task.Exception;
                                Log($"Exception details during cancellation: {cancel_ex.GetType().Name}: {cancel_ex.Message}");
                                if (cancel_ex is Android.Gms.Common.Apis.ApiException apiEx)
                                {
                                    Log($"API Exception Status Code: {apiEx.StatusCode}");
                                    string friendlyError = apiEx.StatusCode switch
                                    {
                                        7 => "Network connection failed. Check your internet access and try again.",
                                        10 => "OAuth client config issue. Verify SHA-1 setup.",
                                        12501 => "Sign-in cancelled by user.",
                                        _ => $"Sign-in failed with error code: {apiEx.StatusCode}."
                                    };
                                    signin_tcs.TrySetException(new Exception(friendlyError));
                                    return;
                                }
                            }
                        }
                        catch (Exception parseErr)
                        {
                            Log($"Failed to parse cancellation intent data: {parseErr.Message}");
                        }
                    }
                    signin_tcs.TrySetCanceled();
                    return;
                }

                try
                {
                    if (intentData == null)
                    {
                        Log("Error: intentData is null!");
                        signin_tcs.TrySetException(new Exception("No data received from Google Sign-In."));
                        return;
                    }

                    Log("Parsing account from intent data...");
                    var signin_task = GoogleSignIn.GetSignedInAccountFromIntent(intentData);
                    Log($"Task returned. IsComplete: {signin_task.IsComplete}, IsSuccessful: {signin_task.IsSuccessful}");

                    if (!signin_task.IsSuccessful)
                    {
                        var taskEx = signin_task.Exception;
                        var errorMsg = taskEx != null ? $"{taskEx.GetType().Name}: {taskEx.Message}" : "Unknown task error";
                        Log($"Google Sign-In Task failed: {errorMsg}");
                        
                        if (taskEx is Android.Gms.Common.Apis.ApiException apiEx)
                        {
                            Log($"API Exception Status Code: {apiEx.StatusCode}");
                        }
                        
                        signin_tcs.TrySetException(new Exception($"Google Sign-In task failed: {errorMsg}"));
                        return;
                    }

                    var google_usr = signin_task.Result as GoogleSignInAccount;
                    if (google_usr == null)
                    {
                        Log("Error: GoogleSignInAccount is null!");
                        signin_tcs.TrySetException(new Exception("Google account data is null."));
                        return;
                    }

                    Log($"Google Account details retrieved: Email={google_usr.Email}, DisplayName={google_usr.DisplayName}");

                    var raw_token = google_usr.IdToken;
                    if (string.IsNullOrEmpty(raw_token))
                    {
                        Log("Error: Google ID Token is null or empty!");
                        signin_tcs.TrySetException(new Exception("Failed to retrieve Google ID Token. (Likely SHA-1 fingerprint mismatch in Firebase console)"));
                        return;
                    }

                    Log($"Google ID Token retrieved successfully. Length: {raw_token.Length}");
                    Log("Exchanging Google ID Token with Firebase...");
                    
                    var fb_creds = await SignInWithGoogleIdTokenAsync(raw_token);
                    Log("Firebase exchange successful!");
                    signin_tcs.TrySetResult(fb_creds);
                }
                catch (Exception handlerErr)
                {
                    Log($"Exception inside activity result handler: {handlerErr.Message}");
                    signin_tcs.TrySetException(new Exception($"Google Sign-In handler failed: {handlerErr.Message}"));
                }
            }
            else
            {
                Log($"Ignoring ActivityResult with RequestCode: {reqCode}");
            }
        };

        MainActivity.ActivityResult += on_act_res;
        Log("Subscribed to MainActivity.ActivityResult.");

        try
        {
            Log("Launching Google Sign-In activity...");
            curr_activity.StartActivityForResult(signin_client.SignInIntent, SigninRequestCode);
            Log("Activity launched successfully.");
        }
        catch (Exception launchErr)
        {
            Log($"Failed to launch Google Sign-In: {launchErr.Message}");
            MainActivity.ActivityResult -= on_act_res;
            throw new Exception($"Failed to launch Google Sign-In screen: {launchErr.Message}");
        }

        return await signin_tcs.Task;
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

        var target_url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signInWithIdp?key={AppConfig.FirebaseWebApiKey}";
        Log($"Firebase Request URL: {target_url}");

        var http_resp = await _http.PostAsJsonAsync(target_url, payload);
        var raw_json_str = await http_resp.Content.ReadAsStringAsync();
        Log($"Firebase Response status: {http_resp.StatusCode}");

        var json_doc = JsonDocument.Parse(raw_json_str);

        if (!http_resp.IsSuccessStatusCode)
        {
            var errorMsg = json_doc.RootElement
                .TryGetProperty("error", out var err)
                ? err.GetProperty("message").GetString() ?? "Sign-in failed"
                : "Google Sign-In failed";
            Log($"Firebase exchange error: {errorMsg}");
            throw new Exception(errorMsg);
        }

        var root_node = json_doc.RootElement;
        return new GoogleSignInResult
        {
            IdToken = root_node.TryGetProperty("idToken", out var tok) ? tok.GetString() ?? "" : "",
            RefreshToken = root_node.TryGetProperty("refreshToken", out var rt) ? rt.GetString() ?? "" : "",
            UserId = root_node.TryGetProperty("localId", out var uid) ? uid.GetString() ?? "" : "",
            Email = root_node.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
            DisplayName = root_node.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
            PhotoUrl = root_node.TryGetProperty("photoUrl", out var ph) ? ph.GetString() ?? "" : "",
            ExpiresIn = int.TryParse(
                root_node.TryGetProperty("expiresIn", out var exp) ? exp.GetString() : "3600", out var expVal)
                ? expVal : 3600,
            IsNewUser = root_node.TryGetProperty("isNewUser", out var nu) && nu.GetBoolean()
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
