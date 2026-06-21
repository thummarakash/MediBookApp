using MediBook.Helpers;
using MediBook.Services;
using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _vm = new();

    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user != null)
        {
            CustomTabBarControl.IsAdmin = user.Role == "Admin";
        }
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        var grid = sender as VisualElement;
        if (grid != null) await AnimationHelper.ButtonPressAsync(grid);
        await Shell.Current.GoToAsync(nameof(NotificationSettingsPage));
    }

    private async void OnSecurityClicked(object sender, EventArgs e)
    {
        var grid = sender as VisualElement;
        if (grid != null) await AnimationHelper.ButtonPressAsync(grid);
        await Shell.Current.GoToAsync(nameof(SecuritySettingsPage));
    }

    private async void OnSignOutClicked(object sender, EventArgs e)
    {
        var grid = sender as VisualElement;
        if (grid != null) await AnimationHelper.ButtonPressAsync(grid);

        bool confirm = await ConfirmationPopupPage.ShowConfirmAsync(Navigation, "Sign Out", "Are you sure you want to sign out?", "Yes", "No", "icon_warning.svg");
        if (confirm)
        {
            if (grid != null) grid.IsEnabled = false;
            try
            {
                await DatabaseService.Instance.LogoutAsync();
                await Shell.Current.GoToAsync("//login");
            }
            finally
            {
                if (grid != null) grid.IsEnabled = true;
            }
        }
    }

    private async void OnMedicalRecordsClicked(object sender, EventArgs e)
    {
        var grid = sender as VisualElement;
        if (grid != null) await AnimationHelper.ButtonPressAsync(grid);
        await Shell.Current.GoToAsync("//documents");
    }

    private async void OnAvatarClicked(object sender, EventArgs e)
    {
        var border = sender as VisualElement;
        if (border != null) await AnimationHelper.ButtonPressAsync(border);

        // Show options using custom popup
        bool isCamera = await ConfirmationPopupPage.ShowConfirmAsync(
            Navigation,
            "Profile Picture",
            "How would you like to select your profile picture?",
            "Camera",
            "Gallery",
            "icon_user.svg"
        );

        try
        {
            FileResult? result = null;
            if (isCamera)
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    result = await MediaPicker.Default.CapturePhotoAsync();
                }
                else
                {
                    await ConfirmationPopupPage.ShowAsync(Navigation, "Error", "Camera is not supported on this device.", "icon_warning.svg");
                    return;
                }
            }
            else
            {
                result = await MediaPicker.Default.PickPhotoAsync();
            }

            if (result != null)
            {
                // Load UI indicator
                _vm.IsLoading = true;

                // Read image stream
                byte[] imageBytes;
                using (var stream = await result.OpenReadAsync())
                {
                    using (var ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        imageBytes = ms.ToArray();
                    }
                }

                // Compress image to fit Firestore 1MB document limit comfortably (e.g. 300px max width/height)
                var compressedBytes = ImageCompressor.ResizeAndCompress(imageBytes, 300, 0.8f);
                string mimeType = "image/jpeg";
                if (compressedBytes != null)
                {
                    imageBytes = compressedBytes;
                }
                else
                {
                    string ext = Path.GetExtension(result.FileName).ToLowerInvariant();
                    mimeType = ext == ".png" ? "image/png" : "image/jpeg";
                }
                
                string base64String = Convert.ToBase64String(imageBytes);
                string avatarDataUrl = $"data:{mimeType};base64,{base64String}";

                var user = await DatabaseService.Instance.GetCurrentUserAsync();
                if (user != null)
                {
                    user.AvatarUrl = avatarDataUrl;
                    await Repositories.UserRepository.Instance.UpdateAsync(user);
                    
                    // Reload data to reflect changes
                    await _vm.LoadCommand.ExecuteAsync(null);
                    
                    // Success alert with green confirmation lock checkmark
                    await ConfirmationPopupPage.ShowAsync(
                        Navigation, 
                        "Success", 
                        "Profile picture updated successfully!", 
                        "icon_lock_confirm.svg",
                        "OK"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Error", $"Failed to update profile picture: {ex.Message}", "icon_warning.svg");
        }
        finally
        {
            _vm.IsLoading = false;
        }
    }
}


