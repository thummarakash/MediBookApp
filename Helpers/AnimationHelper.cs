namespace MediBook.Helpers;

public static class AnimationHelper
{
    public static async Task PageEntranceAsync(VisualElement view, uint duration = 350)
    {
        view.Opacity = 0;
        view.TranslationY = 30;
        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    public static async Task CardEntranceAsync(VisualElement view, uint duration = 280, uint delay = 0)
    {
        if (delay > 0) await Task.Delay((int)delay);
        view.Opacity = 0;
        view.Scale = 0.92;
        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.ScaleTo(1, duration, Easing.CubicOut)
        );
    }

    public static async Task StaggeredListEntranceAsync(IEnumerable<VisualElement> views, uint itemDelay = 60, uint duration = 260)
    {
        var tasks = views.Select((view, index) => CardEntranceAsync(view, duration, (uint)(index * itemDelay)));
        await Task.WhenAll(tasks);
    }

    public static async Task ButtonPressAsync(VisualElement button)
    {
        await button.ScaleTo(0.95, 80, Easing.CubicIn);
        await button.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    public static async Task SuccessPulseAsync(VisualElement view)
    {
        await view.ScaleTo(1.08, 150, Easing.CubicOut);
        await view.ScaleTo(1.0, 150, Easing.CubicIn);
    }

    public static async Task ErrorShakeAsync(VisualElement view)
    {
        for (int i = 0; i < 3; i++)
        {
            await view.TranslateTo(-8, 0, 60, Easing.CubicOut);
            await view.TranslateTo(8, 0, 60, Easing.CubicOut);
        }
        await view.TranslateTo(0, 0, 60, Easing.CubicOut);
    }

    public static Task FadeInAsync(VisualElement view, uint duration = 250)
        => view.FadeTo(1, duration, Easing.CubicOut);

    public static Task FadeOutAsync(VisualElement view, uint duration = 200)
        => view.FadeTo(0, duration, Easing.CubicIn);

    public static async Task SlideDownEntranceAsync(VisualElement view, double fromY = -20, uint duration = 300)
    {
        view.Opacity = 0;
        view.TranslationY = fromY;
        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    public static async Task SlideUpExitAsync(VisualElement view, double toY = -20, uint duration = 200)
    {
        await Task.WhenAll(
            view.FadeTo(0, duration, Easing.CubicIn),
            view.TranslateTo(0, toY, duration, Easing.CubicIn)
        );
    }

    public static async Task PopupOpenAsync(VisualElement popup)
    {
        popup.Opacity = 0;
        popup.Scale = 0.85;
        await Task.WhenAll(
            popup.FadeTo(1, 250, Easing.CubicOut),
            popup.ScaleTo(1.0, 250, Easing.SpringOut)
        );
    }

    public static async Task PopupCloseAsync(VisualElement popup)
    {
        await Task.WhenAll(
            popup.FadeTo(0, 180, Easing.CubicIn),
            popup.ScaleTo(0.9, 180, Easing.CubicIn)
        );
    }

    public static async Task LoadingEntranceAsync(VisualElement spinner)
    {
        spinner.Opacity = 0;
        spinner.Scale = 0.7;
        await Task.WhenAll(
            spinner.FadeTo(1, 200, Easing.CubicOut),
            spinner.ScaleTo(1, 200, Easing.SpringOut)
        );
    }

    public static async Task TabSwitchAsync(VisualElement outgoing, VisualElement incoming)
    {
        await outgoing.FadeTo(0, 120, Easing.CubicIn);
        outgoing.IsVisible = false;
        incoming.IsVisible = true;
        incoming.Opacity = 0;
        await incoming.FadeTo(1, 180, Easing.CubicOut);
    }
}
