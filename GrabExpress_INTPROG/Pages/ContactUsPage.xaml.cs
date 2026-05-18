namespace GrabExpress_INTPROG.Pages;

public partial class ContactUsPage : ContentPage
{
	public ContactUsPage()
	{
		InitializeComponent();
	}

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Support", "Your message has been sent successfully. We will get back to you soon!", "OK");
        await Shell.Current.GoToAsync("..");
    }
}
