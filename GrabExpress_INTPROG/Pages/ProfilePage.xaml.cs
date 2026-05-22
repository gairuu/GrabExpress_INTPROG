using GrabExpress_INTPROG.Services;

namespace GrabExpress_INTPROG.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

	public ProfilePage(AuthService authService, DatabaseService databaseService)
	{
		InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserProfile();
    }

    private async void LoadUserProfile()
    {
        ProfileNameLabel.Text = "Loading...";
        ProfileEmailLabel.Text = "";

        var currentUser = _authService.GetCurrentUser();
        if (currentUser != null)
        {
            var profile = await _databaseService.GetUserProfileAsync(currentUser.Uid);
            if (profile != null)
            {
                ProfileNameLabel.Text = profile.Name;
                ProfileEmailLabel.Text = profile.Email;
            }
        }
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnEditProfileTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Profile", "Edit Profile feature coming soon!", "OK");
    }

    private async void OnSettingsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void OnContactUsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ContactUsPage));
    }

    private async void OnPlaceholderTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "This feature is under development.", "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
        if (answer)
        {
            _authService.Logout();
            await Shell.Current.GoToAsync($"///{nameof(LoginPage)}");
            if (Shell.Current is AppShell appShell)
            {
                appShell.SetRoleTabs("");
            }
        }
    }
}
