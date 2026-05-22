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
            var allDeliveries = await _databaseService.GetAllDeliveriesAsync();

            // Auto-fix: If driver is stuck in "Busy" but has no active jobs, reset them to "Available"
            var myActiveJobs = allDeliveries.Where(d => d.DriverId == currentUser.Uid && (d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned" || d.DeliveryStatus == "Payment Pending" || d.DeliveryStatus == "Paid")).ToList();
            
            if (!myActiveJobs.Any())
            {
                await _databaseService.UpdateDriverStatusAsync(currentUser.Uid, "Available");
            }

            var availableJobs = allDeliveries.Where(d => 
                (d.DeliveryStatus == "Pending" && (d.DeclinedDrivers == null || !d.DeclinedDrivers.ContainsKey(currentUser.Uid))) || 
                (d.DriverId == currentUser.Uid && (d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned" || d.DeliveryStatus == "Payment Pending" || d.DeliveryStatus == "Paid"))
            ).ToList();

            JobsList.ItemsSource = availableJobs
                .OrderByDescending(j => j.DeliveryStatus == "In Transit" || j.DeliveryStatus == "Assigned" || j.DeliveryStatus == "Payment Pending" || j.DeliveryStatus == "Paid") // Active ones first
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

            // Smarter check: See if they ACTUALLY have an active job in the database
            var allDeliveries = await _databaseService.GetAllDeliveriesAsync();
            bool hasActiveJob = allDeliveries.Any(d => d.DriverId == currentUser.Uid && (d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Assigned" || d.DeliveryStatus == "Payment Pending" || d.DeliveryStatus == "Paid"));

            if (hasActiveJob)
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

        if (string.IsNullOrEmpty(deliveryId)) return;

        bool confirm = await DisplayAlert("Decline Job", "Are you sure you want to decline this job? It will remain available for other drivers.", "Yes, Decline", "No");
        if (!confirm) return;

        try
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser != null)
            {
                await _databaseService.DeclineDeliveryAsync(deliveryId, currentUser.Uid);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not decline job: {ex.Message}", "OK");
            return;
        }

        // Refresh the list immediately
        await LoadJobs();

        await DisplayAlert("Job Declined", "You have declined this delivery request. It will no longer show up for you.", "OK");
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
