namespace GrabExpress_INTPROG.Pages;

public partial class ForgotPasswordPage : ContentPage
{
	public ForgotPasswordPage()
	{
		InitializeComponent();
	}

    private async void OnResetClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Success", "Reset link sent to your email.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        if (this.Width > 0)
        {
            double availableWidth = this.Width - 60; // 30 padding on each side
            FormCard.WidthRequest = Math.Min(400, availableWidth);
        }
    }
}
