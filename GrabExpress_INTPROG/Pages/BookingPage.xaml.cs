using GrabExpress_INTPROG.Models;
using GrabExpress_INTPROG.Services;

namespace GrabExpress_INTPROG.Pages;

public partial class BookingPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;
    private decimal _calculatedFee = 0;

	public BookingPage(AuthService authService, DatabaseService databaseService)
	{
		InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;
	}

    private void OnCalculateClicked(object sender, EventArgs e)
    {
        string pickup = PickupEntry.Text;
        string dropoff = DropoffEntry.Text;

        if (string.IsNullOrWhiteSpace(pickup) || string.IsNullOrWhiteSpace(dropoff))
        {
            DisplayAlert("Error", "Please enter both locations", "OK");
            return;
        }

        _calculatedFee = _databaseService.CalculateDeliveryFee(pickup, dropoff);
        FeeLabel.Text = $"₱{_calculatedFee:N2}";
        SummaryBorder.IsVisible = true;


    }



    private void OnLocationTextChanged(object sender, TextChangedEventArgs e)
    {
        // Reset the calculated fee and hide summary card when text changes to prevent location-fee mismatch
        _calculatedFee = 0;
        SummaryBorder.IsVisible = false;

        var entry = (Entry)sender;
        var suggestions = entry == PickupEntry ? PickupSuggestions : DropoffSuggestions;
        
        if (string.IsNullOrWhiteSpace(e.NewTextValue) || e.NewTextValue.Length < 2)
        {
            suggestions.IsVisible = false;
            return;
        }

        var cities = new List<string> { "Manila", "Makati", "Quezon City", "Pasig", "Taguig", "Mandaluyong", "Paranaque", "Pasay", "Caloocan", "Marikina" };
        var filtered = cities.Where(c => c.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Any())
        {
            suggestions.ItemsSource = filtered;
            suggestions.IsVisible = true;
        }
        else
        {
            suggestions.IsVisible = false;
        }
    }

    private void OnSuggestionTapped(object sender, EventArgs e)
    {
        var label = (Label)sender;
        var text = label.Text;
        
        // Find which entry this suggestion belongs to
        if (PickupSuggestions.IsVisible)
        {
            PickupEntry.Text = text;
            PickupSuggestions.IsVisible = false;
        }
        else if (DropoffSuggestions.IsVisible)
        {
            DropoffEntry.Text = text;
            DropoffSuggestions.IsVisible = false;
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        SummaryBorder.IsVisible = false;
        SearchingLayout.IsVisible = true;

        var currentUser = _authService.GetCurrentUser();

        var delivery = new Delivery
        {
            CustomerId = currentUser?.Uid,
            DriverId = "", // Real flow: leave empty until a driver accepts
            PickupLocation = PickupEntry.Text,
            DropoffLocation = DropoffEntry.Text,
            DeliveryFee = _calculatedFee,
            DeliveryStatus = "Pending" // Wait for driver to accept
        };

        string deliveryId = await _databaseService.CreateDeliveryAsync(delivery);

        SearchingLayout.IsVisible = false;
        
        await DisplayAlert("Booking Confirmed!", "Searching for nearby drivers. Please hold.", "OK");

        // Navigate to tracking (Passing locations to ensure the map shows them)
        await Shell.Current.GoToAsync($"TrackingPage?deliveryId={deliveryId}");
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
