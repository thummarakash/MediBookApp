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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ResetPinState();
            ApplyModeSettings();

            if (_mode == "Verify" && BiometricService.Instance.IsBiometricsEnabled())
            {
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(450), async () =>
                {
                    await PerformBiometricAuthenticationAsync();
                });
            }
        }

        private async Task PerformBiometricAuthenticationAsync()
        {
            bool success = await BiometricService.Instance.AuthenticateAsync("Sign in to your MediBook account");
            if (success)
            {
                await Shell.Current.GoToAsync("//home");
            }
        }

        private void ApplyModeSettings()
        {
            if (InstructionLabel == null) return;

            if (_mode == "Setup")
            {
                BackBtn.IsVisible = true;
                PageTitleText.Text = "Set PIN";
                InstructionLabel.Text = "Create a Security PIN";
                SubtitleLabel.Text = "Enter a 4-digit code to secure your app";
                if (LeftBottomBtn != null) LeftBottomBtn.Text = "Clear";
            }
            else if (_mode == "ConfirmSetup")
            {
                BackBtn.IsVisible = true;
                PageTitleText.Text = "Confirm PIN";
                InstructionLabel.Text = "Confirm your PIN";
                SubtitleLabel.Text = "Please re-enter your 4-digit code";
                if (LeftBottomBtn != null) LeftBottomBtn.Text = "Clear";
            }
            else // Verify Mode
            {
                BackBtn.IsVisible = true; 
                PageTitleText.Text = "Enter PIN";
                InstructionLabel.Text = "Enter Security PIN";
                SubtitleLabel.Text = "Enter your 4-digit code to unlock";
                
                if (LeftBottomBtn != null)
                {
                    LeftBottomBtn.Text = BiometricService.Instance.IsBiometricsEnabled() ? "Bio" : "Clear";
                }
            }
        }

        private void OnKeypadClicked(object sender, EventArgs e)
        {
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

        private async Task ProcessPinAsync()
        {
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
            else // Verify Mode
            {
                string savedPin = BiometricService.Instance.GetSecurityPin();
                if (_enteredPin == savedPin)
                {
                    await Shell.Current.GoToAsync("//home");
                }
                else
                {
                    await DisplayAlert("Incorrect PIN", "The Security PIN entered is incorrect. Please try again.", "OK");
                    ResetPinState();
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
