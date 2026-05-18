using GrabExpress_INTPROG.Services;

namespace GrabExpress_INTPROG.Pages;

public partial class DriverDashboardPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

	public DriverDashboardPage(AuthService authService, DatabaseService databaseService)
	{
		InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadJobs();
    }

    private async Task LoadJobs()
    {
        var currentUser = _authService.GetCurrentUser();
        if (currentUser != null)
        {
            // For the demo, show ALL pending deliveries so any driver can accept them, 
            // plus any deliveries already assigned to this specific driver.
            var allDeliveries = await _databaseService.GetAllDeliveriesAsync();
            
            var availableJobs = allDeliveries.Where(d => 
                d.DeliveryStatus == "Pending" || 
                (d.DriverId == currentUser.Uid && d.DeliveryStatus == "In Transit")
            ).ToList();

            JobsList.ItemsSource = availableJobs
                .OrderByDescending(j => j.DeliveryStatus == "In Transit") // Active ones first
                .ThenByDescending(j => j.BookingTime) // Newest pending ones next
                .ToList();
        }
    }

    private async void OnAcceptJobClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        string deliveryId = (string)button.CommandParameter;

        if (string.IsNullOrEmpty(deliveryId)) return;

        try
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null) return;

            // Enforce Rule 4 & 5: Only available drivers can accept and driver can only handle one job
            var driver = await _databaseService.GetDriverAsync(currentUser.Uid);
            if (driver != null && driver.Status != "Available")
            {
                await DisplayAlert("Unavailable", "You cannot accept new jobs because you are currently handling another active delivery!", "OK");
                return;
            }

            // Assign this driver to the delivery
            await _databaseService.AssignDriverToDeliveryAsync(deliveryId, currentUser.Uid);
            
            await DisplayAlert("Success", "Delivery Accepted! You can now start the delivery.", "OK");
            await Shell.Current.GoToAsync($"TrackingPage?deliveryId={deliveryId}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not accept job: {ex.Message}", "OK");
        }
    }

    private async void OnViewJobClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        string deliveryId = (string)button.CommandParameter;

        if (!string.IsNullOrEmpty(deliveryId))
        {
            await Shell.Current.GoToAsync($"TrackingPage?deliveryId={deliveryId}");
        }
    }

    private async void OnDeclineJobClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        string deliveryId = (string)button.CommandParameter;

        bool confirm = await DisplayAlert("Decline Job", "Are you sure you want to decline this job? It will remain available for other drivers.", "Yes, Decline", "No");
        if (!confirm) return;

        // Remove from local list so the driver doesn't see it again this session.
        // The delivery stays Pending in the database for other drivers to pick up.
        var current = JobsList.ItemsSource as IEnumerable<GrabExpress_INTPROG.Models.Delivery>;
        if (current != null)
        {
            var updated = current.Where(d => d.DeliveryId != deliveryId).ToList();
            JobsList.ItemsSource = updated;
        }

        await DisplayAlert("Job Declined", "You have declined this delivery request.", "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _authService.Logout();
        await Shell.Current.GoToAsync($"///{nameof(LoginPage)}");
        if (Shell.Current is AppShell appShell)
        {
            appShell.SetRoleTabs("");
        }
    }
}
