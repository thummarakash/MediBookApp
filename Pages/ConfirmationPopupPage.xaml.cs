using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MediBook.Pages;

public partial class ConfirmationPopupPage : ContentPage
{
    private readonly Action? _onConfirm;
    private readonly Action? _onCancel;
    private readonly TaskCompletionSource<bool>? _tcs;

    // Constructor for Action callbacks (fire-and-forget style)
    public ConfirmationPopupPage(
        string title,
        string message,
        string iconSource = "icon_lock_confirm.svg",
        string confirmText = "OK",
        Action? onConfirm = null,
        string? cancelText = null,
        Action? onCancel = null)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        PopupIcon.Source = iconSource;
        ConfirmBtnLabel.Text = confirmText;
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (!string.IsNullOrEmpty(cancelText))
        {
            CancelBtnLabel.Text = cancelText;
            CancelBtn.IsVisible = true;
        }
    }

    // Constructor for awaitable bool result style
    private ConfirmationPopupPage(
        string title,
        string message,
        string iconSource,
        string confirmText,
        string cancelText,
        TaskCompletionSource<bool> tcs)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        PopupIcon.Source = iconSource;
        ConfirmBtnLabel.Text = confirmText;
        CancelBtnLabel.Text = cancelText;
        CancelBtn.IsVisible = true;
        _tcs = tcs;
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(animated: false);
        _tcs?.TrySetResult(true);
        _onConfirm?.Invoke();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(animated: false);
        _tcs?.TrySetResult(false);
        _onCancel?.Invoke();
    }

    private void OnCardTapped(object sender, TappedEventArgs e)
    {
        // Swallow — prevent card tap from dismissing
    }

    // --- Static helpers ---

    /// <summary>Show an info/success popup (single OK button, no return value).</summary>
    public static async Task ShowAsync(
        INavigation navigation,
        string title,
        string message,
        string iconSource = "icon_lock_confirm.svg",
        string confirmText = "OK",
        Action? onConfirm = null,
        string? cancelText = null,
        Action? onCancel = null)
    {
        var popup = new ConfirmationPopupPage(title, message, iconSource, confirmText, onConfirm, cancelText, onCancel);
        await navigation.PushModalAsync(popup, animated: false);
    }

    /// <summary>Show a Yes/No confirmation popup and await the user's choice. Returns true = confirmed.</summary>
    public static async Task<bool> ShowConfirmAsync(
        INavigation navigation,
        string title,
        string message,
        string confirmText = "Yes",
        string cancelText = "No",
        string iconSource = "icon_lock_confirm.svg")
    {
        var tcs = new TaskCompletionSource<bool>();
        var popup = new ConfirmationPopupPage(title, message, iconSource, confirmText, cancelText, tcs);
        await navigation.PushModalAsync(popup, animated: false);
        return await tcs.Task;
    }
}
