using GrabExpress_INTPROG.Services;

namespace GrabExpress_INTPROG.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;
    private string? _activeDeliveryId;

	public DashboardPage(AuthService authService, DatabaseService databaseService)
	{
		InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;


	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserData();
        await LoadActiveDelivery();
    }

    private async Task LoadActiveDelivery()
    {
        var currentUser = _authService.GetCurrentUser();
        if (currentUser != null)
        {
            var activeDelivery = await _databaseService.GetCustomerActiveDeliveryAsync(currentUser.Uid);
            if (activeDelivery != null)
            {
                _activeDeliveryId = activeDelivery.DeliveryId;
                ActiveDeliveryBanner.IsVisible = true;

                // Update subtitle to reflect live status
                ActiveDeliverySubLabel.Text = activeDelivery.DeliveryStatus switch
                {
                    "Pending"    => "🔍 Searching for a driver... Tap to view",
                    "In Transit" => "🚗 Driver is on the way! Tap to track",
                    _            => "Tap to track your package →"
                };
            }
            else
            {
                ActiveDeliveryBanner.IsVisible = false;
            }
        }
    }

    private async Task LoadUserData()
    {
        // Reset label to default first to prevent visual caching between different account logins
        UserNameLabel.Text = "User";

        var currentUser = _authService.GetCurrentUser();
        if (currentUser != null)
        {
            var profile = await _databaseService.GetUserProfileAsync(currentUser.Uid);
            if (profile != null)
            {
                UserNameLabel.Text = profile.Name;
            }
        }
    }

    private async void OnExpressTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BookingPage));
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProfilePage));
    }

    private async void OnResumeDeliveryTapped(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_activeDeliveryId))
        {
            await Shell.Current.GoToAsync($"TrackingPage?deliveryId={_activeDeliveryId}");
        }
    }

    private async void OnHistoryTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DeliveryHistoryPage));
    }
}
