using GrabExpress_INTPROG.Services;
using GrabExpress_INTPROG.Models;
using Microsoft.Maui.Graphics;
using Firebase.Database;
using Firebase.Database.Query;

namespace GrabExpress_INTPROG.Pages;

[QueryProperty(nameof(DeliveryId), "deliveryId")]
public partial class DeliveryTrackingPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly AuthService _authService;
    private string _deliveryId = string.Empty;
    private bool _isDriverView = false;
    private bool _hasNavigatedToPayment = false;

    public string DeliveryId
    {
        get => _deliveryId;
        set => _deliveryId = value;
    }

    public DeliveryTrackingPage(DatabaseService databaseService, AuthService authService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var currentUser = _authService.GetCurrentUser();
        if (currentUser != null)
        {
            string? role = await _databaseService.GetUserRoleAsync(currentUser.Uid);
            _isDriverView = (role == "Driver");
        }
        else
        {
            _isDriverView = false;
        }

        // Set header based on role
        HeaderTitle.Text = _isDriverView ? "Active Delivery Job" : "Track My Delivery";
        HeaderIcon.Text  = _isDriverView ? "🚗" : "📦";

        // Show correct card sections per role
        ProgressCard.IsVisible      = !_isDriverView;
        DriverCard.IsVisible        = !_isDriverView;
        // Route card is now visible to BOTH

        if (!string.IsNullOrEmpty(_deliveryId))
        {
            StartListening();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopListening();
    }

    // ── Realtime Listener ───────────────────────────────────────────────────    
    private IDispatcherTimer? _pollTimer;

    private void StartListening()
    {
        StopListening();

        // Initial fetch
        _ = PollDatabaseAsync();

        // Start polling every 3 seconds to guarantee state synchronization
        _pollTimer = Dispatcher.CreateTimer();
        _pollTimer.Interval = TimeSpan.FromSeconds(3);
        _pollTimer.Tick += async (s, e) => await PollDatabaseAsync();
        _pollTimer.Start();
    }

    private async Task PollDatabaseAsync()
    {
        try
        {
            var delivery = await _databaseService.GetDeliveryAsync(_deliveryId);
            if (delivery != null)
            {
                delivery.DeliveryId = _deliveryId;
                await ApplyDeliveryStateAsync(delivery);
            }
        }
        catch { /* Ignore network hiccups during polling */ }
    }

    private void StopListening()
    {
        if (_pollTimer != null)
        {
            _pollTimer.Stop();
            _pollTimer = null;
        }
    }

    // ── UI Updates ──────────────────────────────────────────────────────────

    private async Task ApplyDeliveryStateAsync(Delivery delivery)
    {
        var status = delivery.DeliveryStatus ?? "Pending";

        UpdateStatusBadge(status);

        if (_isDriverView)
        {
            if (status == "In Transit")
            {
                CompleteButton.Text = "💳  REQUEST PAYMENT";
                CompleteButton.IsVisible = true;
                CompleteButton.BackgroundColor = Color.FromArgb("#F57F17");
            }
            else if (status == "Paid")
            {
                CompleteButton.Text = "✅  MARK AS DELIVERED";
                CompleteButton.IsVisible = true;
                CompleteButton.BackgroundColor = Color.FromArgb("#00B14F");
            }
            else
            {
                CompleteButton.IsVisible = false;
            }
        }
        else
        {
            CompleteButton.IsVisible = false;
        }

        PayButton.IsVisible      = !_isDriverView && status == "Payment Pending";
        CancelButton.IsVisible   = status != "Payment Pending" && status != "Paid" && status != "Delivered" && status != "Cancelled" && status != "Completed";

        if (!_isDriverView)
        {
            UpdateProgressSteps(status);
        }

        // Fetch driver info if customer and driver assigned
        if (!_isDriverView && !string.IsNullOrEmpty(delivery.DriverId))
        {
            try
            {
                var driver = await _databaseService.GetDriverAsync(delivery.DriverId);
                if (driver != null)
                {
                    var vehicle = await _databaseService.GetVehicleAsync(driver.DriverId ?? string.Empty);
                    DriverNameLabel.Text = driver.Name ?? "Unknown Driver";
                    VehicleLabel.Text    = vehicle != null
                        ? $"🚗 {vehicle.Color} {vehicle.VehicleModel} · {vehicle.PlateNumber}"
                        : (driver.LicenseNumber ?? "No Vehicle Info");
                    ContactLabel.Text    = $"📞 {driver.ContactNumber ?? "No Contact"}";
                    VehicleLabel.IsVisible = true;
                    ContactLabel.IsVisible = true;
                }
                else
                {
                    DriverNameLabel.Text = "Driver details unavailable";
                }
            }
            catch { /* skip non-critical */ }
        }
        else if (!_isDriverView && string.IsNullOrEmpty(delivery.DriverId) && status == "Pending")
        {
            DriverNameLabel.Text   = "Searching for driver...";
            VehicleLabel.IsVisible = false;
            ContactLabel.IsVisible = false;
        }

        // Route info is now visible to BOTH
        RoutePickupLabel.Text  = delivery.PickupLocation  ?? "—";
        RouteDropoffLabel.Text = delivery.DropoffLocation ?? "—";

        // Terminal states
        if (status == "Completed" || status == "Cancelled")
        {
            StopListening();
        }

        // Handle navigation for customer
        if (!_isDriverView && status == "Payment Pending" && !_hasNavigatedToPayment)
        {
            _hasNavigatedToPayment = true;
            StopListening();
            await DisplayAlert("💳 Payment Required", "Your package has arrived! Proceeding to payment.", "OK");
            await Shell.Current.GoToAsync($"{nameof(PaymentPage)}?deliveryId={_deliveryId}");
        }
    }

    // ── UI Helpers ────────────────────────────────────────────────────────────

    private void UpdateStatusBadge(string status)
    {
        switch (status)
        {
            case "Pending":
                StatusLabel.Text = "🔍  SEARCHING FOR DRIVER...";
                StatusBadge.BackgroundColor = Color.FromArgb("#FFF8E1");
                StatusLabel.TextColor = Color.FromArgb("#F57F17");
                break;
            case "In Transit":
                StatusLabel.Text = "🚗  IN TRANSIT";
                StatusBadge.BackgroundColor = Color.FromArgb("#E8F5E9");
                StatusLabel.TextColor = Color.FromArgb("#00B14F");
                break;
            case "Payment Pending":
                StatusLabel.Text = "💳  PAYMENT PENDING";
                StatusBadge.BackgroundColor = Color.FromArgb("#FFF8E1");
                StatusLabel.TextColor = Color.FromArgb("#F57F17");
                break;
            case "Paid":
                StatusLabel.Text = "✅  PAID";
                StatusBadge.BackgroundColor = Color.FromArgb("#E3F2FD");
                StatusLabel.TextColor = Color.FromArgb("#1565C0");
                break;
            case "Delivered":
                StatusLabel.Text = "✅  DELIVERED";
                StatusBadge.BackgroundColor = Color.FromArgb("#E3F2FD");
                StatusLabel.TextColor = Color.FromArgb("#1565C0");
                break;
            case "Completed":
                StatusLabel.Text = "✅  COMPLETED";
                StatusBadge.BackgroundColor = Color.FromArgb("#E8F5E9");
                StatusLabel.TextColor = Color.FromArgb("#00B14F");
                break;
            case "Cancelled":
                StatusLabel.Text = "✖  CANCELLED";
                StatusBadge.BackgroundColor = Color.FromArgb("#FFEBEE");
                StatusLabel.TextColor = Color.FromArgb("#C62828");
                break;
            default:
                StatusLabel.Text = status.ToUpper();
                StatusBadge.BackgroundColor = Color.FromArgb("#F5F5F5");
                StatusLabel.TextColor = Color.FromArgb("#333333");
                break;
        }
    }

    private void UpdateProgressSteps(string status)
    {
        Step1Badge.BackgroundColor = Color.FromArgb("#00B14F");

        bool driverAssigned = status == "Assigned" || status == "In Transit" || status == "Payment Pending" || status == "Paid" || status == "Delivered" || status == "Completed";
        bool delivered      = status == "Delivered" || status == "Completed";

        Step2Badge.BackgroundColor = Color.FromArgb(driverAssigned ? "#00B14F" : "#E0E0E0");
        Step2Icon.Text      = driverAssigned ? "✓" : "2";
        Step2Icon.TextColor = Color.FromArgb(driverAssigned ? "#FFFFFF" : "#999999");
        Step2Title.TextColor = Color.FromArgb(driverAssigned ? "#1A1A1A" : "#AAAAAA");
        Step2Sub.Text       = driverAssigned ? "A driver has accepted your booking" : "Waiting for a driver to accept";
        Step2Sub.TextColor  = Color.FromArgb(driverAssigned ? "#757575" : "#BBBBBB");
        Line1.BackgroundColor = Color.FromArgb(driverAssigned ? "#00B14F" : "#E0E0E0");

        Step3Badge.BackgroundColor = Color.FromArgb(driverAssigned ? "#00B14F" : "#E0E0E0");
        Step3Icon.Text      = driverAssigned ? "✓" : "3";
        Step3Icon.TextColor = Color.FromArgb(driverAssigned ? "#FFFFFF" : "#999999");
        Step3Title.TextColor = Color.FromArgb(driverAssigned ? "#1A1A1A" : "#AAAAAA");
        Step3Sub.Text       = driverAssigned ? "Driver is on the way" : "Driver is heading to you";
        Step3Sub.TextColor  = Color.FromArgb(driverAssigned ? "#757575" : "#BBBBBB");
        Line2.BackgroundColor = Color.FromArgb(driverAssigned ? "#00B14F" : "#E0E0E0");

        Step4Badge.BackgroundColor = Color.FromArgb(delivered ? "#00B14F" : "#E0E0E0");
        Step4Icon.Text      = delivered ? "✓" : "4";
        Step4Icon.TextColor = Color.FromArgb(delivered ? "#FFFFFF" : "#999999");
        Step4Title.TextColor = Color.FromArgb(delivered ? "#1A1A1A" : "#AAAAAA");
        Step4Sub.Text       = delivered ? "Package delivered successfully! 🎉" : "Package delivered successfully";
        Step4Sub.TextColor  = Color.FromArgb(delivered ? "#757575" : "#BBBBBB");
        Line3.BackgroundColor = Color.FromArgb(delivered ? "#00B14F" : "#E0E0E0");
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private async void OnCompleteClicked(object sender, EventArgs e)
    {
        CompleteButton.IsEnabled = false; // Prevent double-tap
        try
        {
            var delivery = await _databaseService.GetDeliveryAsync(_deliveryId);
            if (delivery == null) return;

            if (delivery.DeliveryStatus == "In Transit")
            {
                await _databaseService.UpdateDeliveryStatusAsync(_deliveryId, "Payment Pending");
            }
            else if (delivery.DeliveryStatus == "Paid")
            {
                await _databaseService.UpdateDeliveryStatusAsync(_deliveryId, "Completed");
                var currentUser = _authService.GetCurrentUser();
                if (currentUser != null)
                {
                    await _databaseService.UpdateDriverStatusAsync(currentUser.Uid, "Available");
                }
                
                if (_isDriverView)
                {
                    await DisplayAlert("Success", "Delivery marked as Completed!", "OK");
                    await Shell.Current.GoToAsync("///DriverDashboardPage");
                }
            }
        }
        catch { }
        finally
        {
            CompleteButton.IsEnabled = true;
        }
    }

    private async void OnPayClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(PaymentPage)}?deliveryId={_deliveryId}");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Cancel Booking", "Are you sure you want to cancel this booking?", "Yes", "No");
        if (!confirm) return;

        // Release driver back to Available
        try
        {
            var delivery = await _databaseService.GetDeliveryAsync(_deliveryId);
            if (delivery != null && !string.IsNullOrEmpty(delivery.DriverId))
            {
                await _databaseService.UpdateDriverStatusAsync(delivery.DriverId, "Available");
            }
        }
        catch { /* Ignore non-critical fetch error */ }

        await _databaseService.UpdateDeliveryStatusAsync(_deliveryId, "Cancelled");
        await DisplayAlert("Cancelled", "Booking has been cancelled.", "OK");

        if (_isDriverView)
            await Shell.Current.GoToAsync("///DriverDashboardPage");
        else
            await Shell.Current.GoToAsync("///DashboardPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _authService.Logout();
        await Shell.Current.GoToAsync("///LoginPage");
        if (Shell.Current is AppShell appShell)
        {
            appShell.SetRoleTabs("");
        }
    }

    private bool _isNavigating = false;

    private async void OnBackClicked(object sender, EventArgs e)
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
