namespace MediBook.Pages;

public class OnboardingSlide
{
    public string ImageSource { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public partial class OnboardingPage : ContentPage
{
    private List<OnboardingSlide> _slides = new();

    public OnboardingPage()
    {
        InitializeComponent();
        LoadSlides();
    }

    private void LoadSlides()
    {
        _slides = new List<OnboardingSlide>
        {
            new() { ImageSource = "icon_doctor.svg", Title = "Find Trusted Doctors", Description = "Book appointments with top-rated medical specialists in Melbourne." },
            new() { ImageSource = "icon_calendar.svg", Title = "Easy Scheduling", Description = "Choose your preferred time slot and book an appointment in under a minute." },
            new() { ImageSource = "icon_lock.svg", Title = "Secure Documents", Description = "Store and access your prescriptions and lab reports safely in one place." }
        };
        SlidesCarousel.ItemsSource = _slides;
        SlidesCarousel.IndicatorView = SlidesIndicator;
    }

    private void OnPositionChanged(object sender, PositionChangedEventArgs e)
    {
        int currentPosition = e.CurrentPosition;
        
        // Show/Hide buttons based on page index
        SkipBtn.IsVisible = currentPosition < _slides.Count - 1;
        PrevBtn.IsVisible = currentPosition > 0;
        
        // Change text of NextBtn on the last page
        NextBtn.Text = currentPosition == _slides.Count - 1 ? "Get Started" : "Next";
    }

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        Preferences.Set("medibook_onboarding_seen", true);
        await Shell.Current.GoToAsync("//login");
    }

    private void OnPrevClicked(object sender, EventArgs e)
    {
        int currentPosition = SlidesCarousel.Position;
        if (currentPosition > 0)
        {
            SlidesCarousel.Position = currentPosition - 1;
        }
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        int currentPosition = SlidesCarousel.Position;
        if (currentPosition < _slides.Count - 1)
        {
            SlidesCarousel.Position = currentPosition + 1;
        }
        else
        {
            Preferences.Set("medibook_onboarding_seen", true);
            await Shell.Current.GoToAsync("//login");
        }
    }
}
