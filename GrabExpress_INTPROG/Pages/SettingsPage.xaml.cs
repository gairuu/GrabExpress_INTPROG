namespace GrabExpress_INTPROG.Pages;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
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
}
