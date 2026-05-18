namespace GrabExpress_INTPROG.Pages;

public partial class SplashScreenPage : ContentPage
{
	public SplashScreenPage()
	{
		InitializeComponent();
        NavigateToLanding();
	}

    private async void NavigateToLanding()
    {
        await Task.Delay(3000); // 3 seconds splash
        await Shell.Current.GoToAsync($"///{nameof(LandingPage)}");
    }
}
