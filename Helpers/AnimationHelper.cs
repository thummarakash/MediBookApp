namespace MediBook.Helpers;

public static class AnimationHelper
{
    // Page entry: fade in + slide up from bottom
    public static async Task PageEntranceAsync(View view, uint duration = 350)
    {
        view.Opacity = 0;
        view.TranslationY = 30;
        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    // Card entrance: fade in + scale from 0.92 to 1
    public static async Task CardEntranceAsync(View view, uint duration = 280, uint delay = 0)
    {
        if (delay > 0) await Task.Delay((int)delay);
        view.Opacity = 0;
        view.Scale = 0.92;
        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.ScaleTo(1, duration, Easing.CubicOut)
        );
    }

    // Staggered list: animate each item with increasing delay
    public static async Task StaggeredListEntranceAsync(IEnumerable<View> views, uint itemDelay = 60, uint duration = 260)
    {
        var tasks = views.Select((view, index) => CardEntranceAsync(view, duration, (uint)(index * itemDelay)));
        await Task.WhenAll(tasks);
    }

    // Button press feedback: quick scale down + back up
    public static async Task ButtonPressAsync(View button)
    {
        await button.ScaleTo(0.95, 80, Easing.CubicIn);
        await button.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    // Success animation: scale pulse
    public static async Task SuccessPulseAsync(View view)
    {
        await view.ScaleTo(1.08, 150, Easing.CubicOut);
        await view.ScaleTo(1.0, 150, Easing.CubicIn);
    }

    // Error shake: horizontal oscillation
    public static async Task ErrorShakeAsync(View view)
    {
        for (int i = 0; i < 3; i++)
        {
            await view.TranslateTo(-8, 0, 60, Easing.CubicOut);
            await view.TranslateTo(8, 0, 60, Easing.CubicOut);
        }
        await view.TranslateTo(0, 0, 60, Easing.CubicOut);
    }

    // Fade in only
    public static Task FadeInAsync(View view, uint duration = 250)
        => view.FadeTo(1, duration, Easing.CubicOut);

    // Fade out only
    public static Task FadeOutAsync(View view, uint duration = 200)
        => view.FadeTo(0, duration, Easing.CubicIn);

    // Slide down entrance (modal/bottom sheet style)
    public static async Task SlideDownEntranceAsync(View view, double fromY = -20, uint duration = 300)
    {
        view.Opacity = 0;
        view.TranslationY = fromY;
        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    // Slide up exit
    public static async Task SlideUpExitAsync(View view, double toY = -20, uint duration = 200)
    {
        await Task.WhenAll(
            view.FadeTo(0, duration, Easing.CubicIn),
            view.TranslateTo(0, toY, duration, Easing.CubicIn)
        );
    }

    // Popup open: scale from 0.85 + fade
    public static async Task PopupOpenAsync(View popup)
    {
        popup.Opacity = 0;
        popup.Scale = 0.85;
        await Task.WhenAll(
            popup.FadeTo(1, 250, Easing.CubicOut),
            popup.ScaleTo(1.0, 250, Easing.SpringOut)
        );
    }

    // Popup close
    public static async Task PopupCloseAsync(View popup)
    {
        await Task.WhenAll(
            popup.FadeTo(0, 180, Easing.CubicIn),
            popup.ScaleTo(0.9, 180, Easing.CubicIn)
        );
    }

    // Loading spinner entrance
    public static async Task LoadingEntranceAsync(View spinner)
    {
        spinner.Opacity = 0;
        spinner.Scale = 0.7;
        await Task.WhenAll(
            spinner.FadeTo(1, 200, Easing.CubicOut),
            spinner.ScaleTo(1, 200, Easing.SpringOut)
        );
    }

    // Tab switch: quick fade
    public static async Task TabSwitchAsync(View outgoing, View incoming)
    {
        await outgoing.FadeTo(0, 120, Easing.CubicIn);
        outgoing.IsVisible = false;
        incoming.IsVisible = true;
        incoming.Opacity = 0;
        await incoming.FadeTo(1, 180, Easing.CubicOut);
    }
}
