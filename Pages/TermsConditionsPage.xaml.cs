using System;
using Microsoft.Maui.Controls;

namespace MediBook.Pages;

public partial class TermsConditionsPage : ContentPage
{
    public TermsConditionsPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
