using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace MediBook;

[Activity(NoHistory = true, Exported = true, LaunchMode = LaunchMode.SingleTop)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.akash.medibook")]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
