using MediBook.Models;
using MediBook.Services;
using MediBook.ViewModels;

namespace MediBook.Pages;

[QueryProperty(nameof(DoctorId), "doctorId")]
public partial class BookAppointmentPage : ContentPage
{
    private readonly BookAppointmentViewModel _vm = new();

    public string DoctorId
    {
        get => _vm.DoctorId;
        set => _vm.DoctorId = value;
    }

    public BookAppointmentPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.PropertyChanged += OnVmPropertyChanged;
        await _vm.InitializeAsync();
        UpdateStepperVisuals(_vm.CurrentStep);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.PropertyChanged -= OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BookAppointmentViewModel.CurrentStep))
            UpdateStepperVisuals(_vm.CurrentStep);
    }

    private void UpdateStepperVisuals(int step)
    {
        ResetStepperCircles();
        // each step number activates its circle and all previous ones
        if (step >= 1) HighlightCircle(Step1Circle, Step1Label);
        if (step >= 2) HighlightCircle(Step2Circle, Step2Label);
        if (step >= 3) HighlightCircle(Step3Circle, Step3Label);
        if (step >= 4) HighlightCircle(Step4Circle, Step4Label);
    }

    private void ResetStepperCircles()
    {
        var navy = (Color)(Application.Current?.Resources["PrimaryNavy"] ?? Color.FromArgb("#042C53"));

        Step1Circle.BackgroundColor = navy;
        Step1Circle.StrokeThickness = 1;
        Step1Circle.Stroke = Colors.White;
        Step1Label.TextColor = Color.FromArgb("#B5D4F4");

        Step2Circle.BackgroundColor = navy;
        Step2Circle.StrokeThickness = 1;
        Step2Circle.Stroke = Colors.White;
        Step2Label.TextColor = Color.FromArgb("#B5D4F4");

        Step3Circle.BackgroundColor = navy;
        Step3Circle.StrokeThickness = 1;
        Step3Circle.Stroke = Colors.White;
        Step3Label.TextColor = Color.FromArgb("#B5D4F4");

        Step4Circle.BackgroundColor = navy;
        Step4Circle.StrokeThickness = 1;
        Step4Circle.Stroke = Colors.White;
        Step4Label.TextColor = Color.FromArgb("#B5D4F4");
    }

    private void HighlightCircle(Border circle, Label label)
    {
        var blue = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#185FA5"));
        circle.BackgroundColor = blue;
        circle.StrokeThickness = 0;
        label.TextColor = Colors.White;
    }
}
