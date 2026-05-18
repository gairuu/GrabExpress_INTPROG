using GrabExpress_INTPROG.Services;
using GrabExpress_INTPROG.Models;

namespace GrabExpress_INTPROG.Pages;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;
    private bool _isDriversTab = true;

    public AdminDashboardPage(AuthService authService, DatabaseService databaseService)
    {
        InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshData();
    }

    private async Task RefreshData()
    {
        if (_isDriversTab)
        {
            await LoadDrivers();
        }
        else
        {
            await LoadCustomers();
        }
    }

    private async Task LoadDrivers()
    {
        var drivers = await _databaseService.GetAllDriversAsync();
        DriversList.ItemsSource = drivers;
    }

    private async Task LoadCustomers()
    {
        var customers = await _databaseService.GetAllCustomersAsync();
        CustomersList.ItemsSource = customers;
    }

    private async void OnDriversTabClicked(object sender, EventArgs e)
    {
        _isDriversTab = true;
        DriversTabBtn.BackgroundColor = Color.FromArgb("#1A237E");
        DriversTabBtn.TextColor = Colors.White;

        CustomersTabBtn.BackgroundColor = Color.FromArgb("#E0E0E0");
        CustomersTabBtn.TextColor = Color.FromArgb("#333333");

        DriversSection.IsVisible = true;
        CustomersSection.IsVisible = false;

        await LoadDrivers();
    }

    private async void OnCustomersTabClicked(object sender, EventArgs e)
    {
        _isDriversTab = false;
        CustomersTabBtn.BackgroundColor = Color.FromArgb("#1A237E");
        CustomersTabBtn.TextColor = Colors.White;

        DriversTabBtn.BackgroundColor = Color.FromArgb("#E0E0E0");
        DriversTabBtn.TextColor = Color.FromArgb("#333333");

        DriversSection.IsVisible = false;
        CustomersSection.IsVisible = true;

        await LoadCustomers();
    }

    private async void OnDeleteDriverClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var driver = button.CommandParameter as Driver;

        if (driver == null || string.IsNullOrEmpty(driver.DriverId)) return;

        if (driver.IsSuspended)
        {
            bool confirm = await DisplayAlert("Reactivate Driver", $"Are you sure you want to reactivate {driver.Name}?", "Yes, Reactivate", "Cancel");
            if (!confirm) return;

            try
            {
                await _databaseService.SaveUserRoleAsync(driver.DriverId, "Driver");
                await DisplayAlert("Success", "Driver account reactivated successfully.", "OK");
                await LoadDrivers();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not reactivate driver: {ex.Message}", "OK");
            }
        }
        else
        {
            bool confirm = await DisplayAlert("Suspend Driver", $"Are you sure you want to suspend {driver.Name}? They will be blocked from logging in.", "Yes, Suspend", "Cancel");
            if (!confirm) return;

            try
            {
                await _databaseService.DeleteDriverAsync(driver.DriverId);
                await DisplayAlert("Success", "Driver account suspended successfully.", "OK");
                await LoadDrivers();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not suspend driver: {ex.Message}", "OK");
            }
        }
    }

    private async void OnDeleteCustomerClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var customer = button.CommandParameter as Customer;

        if (customer == null || string.IsNullOrEmpty(customer.CustomerId)) return;

        if (customer.IsSuspended)
        {
            bool confirm = await DisplayAlert("Reactivate Customer", $"Are you sure you want to reactivate {customer.Name}?", "Yes, Reactivate", "Cancel");
            if (!confirm) return;

            try
            {
                await _databaseService.SaveUserRoleAsync(customer.CustomerId, "Customer");
                await DisplayAlert("Success", "Customer account reactivated successfully.", "OK");
                await LoadCustomers();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not reactivate customer: {ex.Message}", "OK");
            }
        }
        else
        {
            bool confirm = await DisplayAlert("Suspend Customer", $"Are you sure you want to suspend {customer.Name}? They will be blocked from logging in.", "Yes, Suspend", "Cancel");
            if (!confirm) return;

            try
            {
                await _databaseService.DeleteCustomerAsync(customer.CustomerId);
                await DisplayAlert("Success", "Customer account suspended successfully.", "OK");
                await LoadCustomers();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not suspend customer: {ex.Message}", "OK");
            }
        }
    }

    private async void OnToggleDriverStatusClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        string driverId = (string)button.CommandParameter;

        if (string.IsNullOrEmpty(driverId)) return;

        try
        {
            var driver = await _databaseService.GetDriverAsync(driverId);
            if (driver != null)
            {
                // Toggle status loop: Available -> Busy -> Offline -> Available
                string nextStatus = "Available";
                if (driver.Status == "Available") nextStatus = "Busy";
                else if (driver.Status == "Busy") nextStatus = "Offline";

                await _databaseService.UpdateDriverStatusAsync(driverId, nextStatus);
                await DisplayAlert("Status Updated", $"Driver status set to: {nextStatus}", "OK");
                await LoadDrivers();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not toggle status: {ex.Message}", "OK");
        }
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
