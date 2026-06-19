namespace MediBook.Helpers;

public static class ConnectivityHelper
{
    public static bool IsConnected
        => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public static void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("No internet connection. Please check your network and try again.");
    }

    public static async Task<T> WithConnectivityFallbackAsync<T>(
        Func<Task<T>> onlineAction,
        Func<Task<T>> offlineAction)
    {
        if (IsConnected)
            return await onlineAction();
        return await offlineAction();
    }
}
