using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MediBook.Services;

namespace MediBook.Pages
{
    [QueryProperty(nameof(Mode), "mode")]
    public partial class PinPage : ContentPage
    {
        private string _mode = "Verify"; // Default is Verify
        private string _enteredPin = "";
        private string _tempPinForSetup = "";
        private const int PinLength = 4;

        // Static fields so the lockout state persists across navigation
        private static int _failedAttempts = 0;
        private static int _lockoutCount = 0;
        private static DateTime _lockoutEndTime = DateTime.MinValue;

        public string Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                ApplyModeSettings();
            }
        }

        public PinPage()
        {
            InitializeComponent();
        }

        public static void ResetLockout()
        {
            _failedAttempts = 0;
            _lockoutCount = 0;
            _lockoutEndTime = DateTime.MinValue;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ResetPinState();

            if (_lockoutCount >= 3)
            {
                ShowPasswordUnlockState();
            }
            else if (CheckLockout())
            {
                StartLockoutTimer((int)(_lockoutEndTime - DateTime.UtcNow).TotalSeconds);
            }
            else
            {
                ApplyModeSettings();
                if (_mode == "Verify" && BiometricService.Instance.IsBiometricsEnabled())
                {
                    Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(450), async () =>
                    {
                        await PerformBiometricAuthenticationAsync();
                    });
                }
            }
        }

        private bool CheckLockout()
        {
            if (_lockoutCount >= 3) return true;
            return DateTime.UtcNow < _lockoutEndTime;
        }

        private void StartLockoutTimer(int seconds)
        {
            if (InstructionLabel == null) return;
            
            InstructionLabel.Text = "App Disabled";
            SubtitleLabel.Text = $"Please wait {seconds} seconds to try again.";

            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (_lockoutCount >= 3)
                {
                    ShowPasswordUnlockState();
                    return false;
                }
                if (DateTime.UtcNow < _lockoutEndTime)
                {
                    var secondsLeft = (int)Math.Max(0, (_lockoutEndTime - DateTime.UtcNow).TotalSeconds);
                    InstructionLabel.Text = "App Disabled";
                    SubtitleLabel.Text = $"Please wait {secondsLeft} seconds to try again.";
                    return true;
                }
                _failedAttempts = 0;
                ApplyModeSettings();
                ResetPinState();
                return false;
            });
        }

        private async Task PerformBiometricAuthenticationAsync()
        {
            if (CheckLockout()) return;
            bool success = await BiometricService.Instance.AuthenticateAsync("Sign in to your MediBook account", allowPinFallback: false);
            if (success)
            {
                ResetLockout();
                await Shell.Current.GoToAsync("//home");
            }
        }

        private void ShowPasswordUnlockState()
        {
            if (InstructionLabel == null) return;
            InstructionLabel.Text = "PIN Blocked";
            SubtitleLabel.Text = "Please tap 'Unlock' and verify password.";
            if (LeftBottomBtn != null)
            {
                LeftBottomBtn.ImageSource = null;
                LeftBottomBtn.Text = "Unlock";
            }
        }

        private void ApplyModeSettings()
        {
            if (InstructionLabel == null) return;

            if (_lockoutCount >= 3)
            {
                ShowPasswordUnlockState();
                return;
            }

            if (_mode == "Setup")
            {
                BackBtn.IsVisible = true;
                PageTitleText.Text = "Set PIN";
                InstructionLabel.Text = "Create a Security PIN";
                SubtitleLabel.Text = "Enter a 4-digit code to secure your app";
                if (LeftBottomBtn != null)
                {
                    LeftBottomBtn.ImageSource = null;
                    LeftBottomBtn.Text = "Clear";
                }
            }
            else if (_mode == "VerifyOldPin")
            {
                BackBtn.IsVisible = true;
                PageTitleText.Text = "Verify Old PIN";
                InstructionLabel.Text = "Enter Old PIN";
                SubtitleLabel.Text = "Enter your current 4-digit code first";
                if (LeftBottomBtn != null)
                {
                    LeftBottomBtn.ImageSource = null;
                    LeftBottomBtn.Text = "Clear";
                }
            }
            else if (_mode == "ConfirmSetup")
            {
                BackBtn.IsVisible = true;
                PageTitleText.Text = "Confirm PIN";
                InstructionLabel.Text = "Confirm your PIN";
                SubtitleLabel.Text = "Please re-enter your 4-digit code";
                if (LeftBottomBtn != null)
                {
                    LeftBottomBtn.ImageSource = null;
                    LeftBottomBtn.Text = "Clear";
                }
            }
            else // Verify Mode
            {
                BackBtn.IsVisible = true; 
                PageTitleText.Text = "Enter PIN";
                InstructionLabel.Text = "Enter Security PIN";
                SubtitleLabel.Text = "Enter your 4-digit code to unlock";
                
                if (LeftBottomBtn != null)
                {
                    if (BiometricService.Instance.IsBiometricsEnabled())
                    {
                        LeftBottomBtn.Text = "";
                        LeftBottomBtn.ImageSource = "icon_fingerprint.png";
                    }
                    else
                    {
                        LeftBottomBtn.ImageSource = null;
                        LeftBottomBtn.Text = "Clear";
                    }
                }
            }
        }

        private void OnKeypadClicked(object sender, EventArgs e)
        {
            if (_lockoutCount >= 3)
            {
                _ = PromptPasswordToUnlockAsync();
                return;
            }
            if (CheckLockout()) return;

            if (sender is Button button)
            {
                string digit = button.Text;
                if (_enteredPin.Length < PinLength)
                {
                    _enteredPin += digit;
                    UpdateDots();

                    if (_enteredPin.Length == PinLength)
                    {
                        // Brief delay for user to see the last dot filled
                        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), async () =>
                        {
                            await ProcessPinAsync();
                        });
                    }
                }
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            if (_lockoutCount >= 3)
            {
                _ = PromptPasswordToUnlockAsync();
                return;
            }
            if (CheckLockout()) return;

            if (_mode == "Verify" && BiometricService.Instance.IsBiometricsEnabled())
            {
                _ = PerformBiometricAuthenticationAsync();
            }
            else
            {
                ResetPinState();
            }
        }

        private void OnBackspaceClicked(object sender, EventArgs e)
        {
            if (_lockoutCount >= 3)
            {
                _ = PromptPasswordToUnlockAsync();
                return;
            }
            if (CheckLockout()) return;

            if (_enteredPin.Length > 0)
            {
                _enteredPin = _enteredPin.Substring(0, _enteredPin.Length - 1);
                UpdateDots();
            }
        }

        private void ResetPinState()
        {
            _enteredPin = "";
            UpdateDots();
        }

        private void UpdateDots()
        {
            if (Dot1 == null) return;

            Color filledColor = (Color)Application.Current.Resources["PrimaryNavy"];
            Color emptyColor = Colors.Transparent;

            Dot1.BackgroundColor = _enteredPin.Length >= 1 ? filledColor : emptyColor;
            Dot2.BackgroundColor = _enteredPin.Length >= 2 ? filledColor : emptyColor;
            Dot3.BackgroundColor = _enteredPin.Length >= 3 ? filledColor : emptyColor;
            Dot4.BackgroundColor = _enteredPin.Length >= 4 ? filledColor : emptyColor;
        }

        private async Task PromptPasswordToUnlockAsync()
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user == null)
            {
                await DisplayAlert("Session Expired", "Please login with your credentials.", "OK");
                ResetLockout();
                await Shell.Current.GoToAsync("//login");
                return;
            }

            if (user.AuthProvider == "Google")
            {
                bool confirm = await DisplayAlert("Unlock PIN", "Please verify your identity using Google Sign-In.", "Verify", "Cancel");
                if (confirm)
                {
                    try
                    {
                        await GoogleAuthService.Instance.SignInAsync();
                        ResetLockout();
                        ApplyModeSettings();
                        ResetPinState();
                        await DisplayAlert("Success", "PIN unlocked successfully.", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Verification Failed", ex.Message, "OK");
                    }
                }
                return;
            }

            string password = await DisplayPromptAsync("Unlock PIN", "Enter your account password to unlock:", "Unlock", "Cancel", "Password", -1, Keyboard.Text, "");
            if (string.IsNullOrEmpty(password)) return;

            try
            {
                // Authenticate to verify the password
                await MediBook.Services.Firebase.FirebaseAuthService.Instance.SignInWithEmailPasswordAsync(user.Email, password);
                ResetLockout();
                ApplyModeSettings();
                ResetPinState();
                await DisplayAlert("Success", "PIN unlocked successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Verification Failed", "Incorrect password: " + ex.Message, "OK");
            }
        }

        private async Task ProcessPinAsync()
        {
            if (CheckLockout()) return;

            if (_mode == "Setup")
            {
                _tempPinForSetup = _enteredPin;
                _mode = "ConfirmSetup";
                ResetPinState();
                ApplyModeSettings();
            }
            else if (_mode == "ConfirmSetup")
            {
                if (_enteredPin == _tempPinForSetup)
                {
                    BiometricService.Instance.SetSecurityPin(_enteredPin);
                    BiometricService.Instance.SetPinEnabled(true);
                    await DisplayAlert("Success", "Security PIN set successfully.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "PINs did not match. Please try again.", "OK");
                    _mode = "Setup";
                    ResetPinState();
                    ApplyModeSettings();
                }
            }
            else // Verify Mode or VerifyOldPin Mode
            {
                string savedPin = BiometricService.Instance.GetSecurityPin();
                if (_enteredPin == savedPin)
                {
                    ResetLockout();
                    if (_mode == "VerifyOldPin")
                    {
                        _mode = "Setup";
                        ResetPinState();
                        ApplyModeSettings();
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("//home");
                    }
                }
                else
                {
                    _failedAttempts++;
                    if (_failedAttempts >= 3)
                    {
                        _lockoutCount++;
                        _failedAttempts = 0;

                        if (_lockoutCount == 1)
                        {
                            _lockoutEndTime = DateTime.UtcNow.AddSeconds(30);
                            await DisplayAlert("App Disabled", "Too many incorrect attempts. App disabled for 30 seconds.", "OK");
                            StartLockoutTimer(30);
                        }
                        else if (_lockoutCount == 2)
                        {
                            _lockoutEndTime = DateTime.UtcNow.AddSeconds(60);
                            await DisplayAlert("App Disabled", "Too many incorrect attempts. App disabled for 60 seconds.", "OK");
                            StartLockoutTimer(60);
                        }
                        else
                        {
                            await DisplayAlert("App Locked", "Too many incorrect attempts. Please verify your account password to unlock PIN.", "OK");
                            ShowPasswordUnlockState();
                            _ = PromptPasswordToUnlockAsync();
                        }
                    }
                    else
                    {
                        int remaining = 3 - _failedAttempts;
                        await DisplayAlert("Incorrect PIN", $"The Security PIN entered is incorrect. {remaining} attempt(s) remaining.", "OK");
                        ResetPinState();
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            if (_mode == "Verify")
            {
                // In Verify mode, if they click back/cancel, take them to the login screen for manual password entry
                await Shell.Current.GoToAsync("//login");
            }
            else
            {
                // In Setup modes, just go back to Settings Page
                await Navigation.PopAsync();
            }
        }
    }
}
