namespace GrabExpress_INTPROG.Pages;

public partial class ContactUsPage : ContentPage
{
	public ContactUsPage()
	{
		InitializeComponent();
	}

    private bool _isNavigating = false;

    private async void OnBackTapped(object sender, EventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;

        await Shell.Current.GoToAsync("..");
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_isNavigating)
        {
            _isNavigating = true;
            Dispatcher.Dispatch(async () =>
            {
                await Shell.Current.GoToAsync("..");
            });
        }
        return true;
    }

    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        
        await DisplayAlert("Support", "Your message has been sent successfully. We will get back to you soon!", "OK");
        await Shell.Current.GoToAsync("..");
    }
}
