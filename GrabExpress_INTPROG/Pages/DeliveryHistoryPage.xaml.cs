using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GrabExpress_INTPROG.Models;
using GrabExpress_INTPROG.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace GrabExpress_INTPROG.Pages;

public partial class DeliveryHistoryPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

    private List<DeliveryHistoryItemViewModel> _allItems = new();
    private List<DeliveryHistoryItemViewModel> _filteredItems = new();
    private string _selectedStatus = "All";
    private bool _isNavigating = false;

    public List<DeliveryHistoryItemViewModel> FilteredItems
    {
        get => _filteredItems;
        set
        {
            _filteredItems = value;
            OnPropertyChanged();
        }
    }

    public DeliveryHistoryPage(AuthService authService, DatabaseService databaseService)
    {
        InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isNavigating = false;
        ReceiptModalOverlay.IsVisible = false;
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null) return;

            // Fetch ALL deliveries for this customer
            var allDeliveries = await _databaseService.GetAllDeliveriesAsync();
            var customerDeliveries = allDeliveries
                .Where(d => d.CustomerId == currentUser.Uid)
                .OrderByDescending(d => d.BookingTime)
                .ToList();

            // Calculate metrics
            decimal totalSpent = customerDeliveries.Where(d => d.DeliveryStatus == "Completed").Sum(d => d.DeliveryFee);
            int completedCount = customerDeliveries.Count(d => d.DeliveryStatus == "Completed");
            int activeCount = customerDeliveries.Count(d => IsActiveStatus(d.DeliveryStatus));

            TotalSpentLabel.Text = $"₱{totalSpent:N2}";
            CompletedCountLabel.Text = completedCount.ToString();
            ActiveCountLabel.Text = activeCount.ToString();

            // Map models
            _allItems = customerDeliveries.Select(d => new DeliveryHistoryItemViewModel
            {
                DeliveryId = d.DeliveryId,
                PickupLocation = d.PickupLocation,
                DropoffLocation = d.DropoffLocation,
                BookingTime = d.BookingTime,
                DeliveryStatus = d.DeliveryStatus,
                DeliveryFee = d.DeliveryFee,
                RawDelivery = d
            }).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading history: {ex.Message}");
        }
    }

    private void ApplyFilters()
    {
        var query = _allItems.AsEnumerable();

        // Filter by search entry
        var text = SearchEntry.Text?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(text))
        {
            query = query.Where(i => (i.PickupLocation?.ToLowerInvariant().Contains(text) == true) ||
                                     (i.DropoffLocation?.ToLowerInvariant().Contains(text) == true));
        }

        // Filter by active pill status
        if (_selectedStatus == "Active")
        {
            query = query.Where(i => IsActiveStatus(i.DeliveryStatus));
        }
        else if (_selectedStatus == "Completed")
        {
            query = query.Where(i => i.DeliveryStatus == "Completed");
        }
        else if (_selectedStatus == "Cancelled")
        {
            query = query.Where(i => i.DeliveryStatus == "Cancelled");
        }

        FilteredItems = query.ToList();
    }

    private bool IsActiveStatus(string? status)
    {
        return status == "Pending" || status == "In Transit" || status == "Payment Pending" || status == "Paid" || status == "Delivered";
    }

    // Filters Tap Handlers
    private void OnFilterAllTapped(object sender, EventArgs e) => UpdateFilterPill("All", PillAll, LabelAll);
    private void OnFilterActiveTapped(object sender, EventArgs e) => UpdateFilterPill("Active", PillActive, LabelActive);
    private void OnFilterCompletedTapped(object sender, EventArgs e) => UpdateFilterPill("Completed", PillCompleted, LabelCompleted);
    private void OnFilterCancelledTapped(object sender, EventArgs e) => UpdateFilterPill("Cancelled", PillCancelled, LabelCancelled);

    private void UpdateFilterPill(string status, Border activeBorder, Label activeLabel)
    {
        _selectedStatus = status;

        // Reset all pills
        ResetPill(PillAll, LabelAll);
        ResetPill(PillActive, LabelActive);
        ResetPill(PillCompleted, LabelCompleted);
        ResetPill(PillCancelled, LabelCancelled);

        // Highlight selected
        activeBorder.BackgroundColor = Color.FromArgb("#00B14F");
        activeBorder.StrokeThickness = 0;
        activeLabel.TextColor = Colors.White;

        ApplyFilters();
    }

    private void ResetPill(Border border, Label label)
    {
        border.BackgroundColor = Colors.White;
        border.Stroke = Color.FromArgb("#E2E8F0");
        border.StrokeThickness = 1;
        label.TextColor = Color.FromArgb("#4A5568");
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    // Modal Operations
    private async Task ShowReceiptAsync(DeliveryHistoryItemViewModel item)
    {
        ReceiptRefLabel.Text = $"#{item.DeliveryId?.Substring(Math.Max(0, (item.DeliveryId?.Length ?? 0) - 10))}";
        ReceiptPickupLabel.Text = item.PickupLocation;
        ReceiptDropoffLabel.Text = item.DropoffLocation;
        ReceiptBaseFareLabel.Text = item.FeeFormatted;
        ReceiptTotalPaidLabel.Text = item.FeeFormatted;

        // Populate Driver details dynamically
        ReceiptDriverContainer.Children.Clear();
        if (string.IsNullOrEmpty(item.RawDelivery.DriverId))
        {
            ReceiptDriverContainer.Children.Add(new Label { Text = "No driver assigned.", FontSize = 11, TextColor = Color.FromArgb("#718096") });
        }
        else
        {
            ReceiptDriverContainer.Children.Add(new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#00B14F"), HeightRequest = 20, WidthRequest = 20 });
            try
            {
                var driver = await _databaseService.GetDriverAsync(item.RawDelivery.DriverId);
                var vehicle = await _databaseService.GetVehicleAsync(item.RawDelivery.DriverId);

                ReceiptDriverContainer.Children.Clear();
                if (driver != null)
                {
                    ReceiptDriverContainer.Children.Add(new Label { Text = driver.Name, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2D3748") });
                    if (vehicle != null)
                    {
                        ReceiptDriverContainer.Children.Add(new Label { Text = $"Vehicle: {vehicle.Color} {vehicle.VehicleModel} ({vehicle.PlateNumber})", FontSize = 10, TextColor = Color.FromArgb("#718096") });
                    }
                    else
                    {
                        ReceiptDriverContainer.Children.Add(new Label { Text = $"Contact: {driver.ContactNumber}", FontSize = 10, TextColor = Color.FromArgb("#718096") });
                    }
                }
                else
                {
                    ReceiptDriverContainer.Children.Add(new Label { Text = "Driver details unavailable.", FontSize = 11, TextColor = Color.FromArgb("#718096") });
                }
            }
            catch
            {
                ReceiptDriverContainer.Children.Clear();
                ReceiptDriverContainer.Children.Add(new Label { Text = "Driver details unavailable.", FontSize = 11, TextColor = Color.FromArgb("#718096") });
            }
        }

        ReceiptModalOverlay.IsVisible = true;
    }

    private void OnCloseReceiptTapped(object sender, EventArgs e)
    {
        ReceiptModalOverlay.IsVisible = false;
    }

    private void OnCloseReceiptClicked(object sender, EventArgs e)
    {
        ReceiptModalOverlay.IsVisible = false;
    }

    private void OnModalBodyTapped(object sender, EventArgs e)
    {
        // Intercept to prevent backdrop tap close
    }

    private async void OnReceiptClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is DeliveryHistoryItemViewModel item)
        {
            await ShowReceiptAsync(item);
        }
    }

    private async void OnCardTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer recognizer)
        {
            var item = border.BindingContext as DeliveryHistoryItemViewModel;
            if (item != null && IsActiveStatus(item.DeliveryStatus))
            {
                if (_isNavigating) return;
                _isNavigating = true;

                await Shell.Current.GoToAsync($"TrackingPage?deliveryId={item.DeliveryId}");
            }
        }
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;

        await Shell.Current.GoToAsync("///DashboardPage");
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_isNavigating)
        {
            _isNavigating = true;
            Dispatcher.Dispatch(async () =>
            {
                await Shell.Current.GoToAsync("///DashboardPage");
            });
        }
        return true;
    }
}

public class DeliveryHistoryItemViewModel
{
    public string? DeliveryId { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropoffLocation { get; set; }
    public DateTime BookingTime { get; set; }
    public string BookingTimeFormatted => BookingTime.ToString("MMM dd, yyyy • hh:mm tt");
    public string? DeliveryStatus { get; set; }
    
    public string StatusText => DeliveryStatus ?? "Unknown";

    public Color StatusBgColor => DeliveryStatus switch
    {
        "Pending" => Color.FromArgb("#FEF3C7"), // Light Warning Amber
        "In Transit" => Color.FromArgb("#DBEAFE"), // Light Primary Blue
        "Payment Pending" => Color.FromArgb("#DBEAFE"),
        "Paid" => Color.FromArgb("#DBEAFE"),
        "Delivered" => Color.FromArgb("#D1FAE5"), // Light Success Green
        "Completed" => Color.FromArgb("#D1FAE5"),
        "Cancelled" => Color.FromArgb("#FEE2E2"), // Light Danger Red
        _ => Color.FromArgb("#EDF2F7")
    };

    public Color StatusTextColor => DeliveryStatus switch
    {
        "Pending" => Color.FromArgb("#B7791F"), // Amber Dark
        "In Transit" => Color.FromArgb("#1D4ED8"), // Blue Dark
        "Payment Pending" => Color.FromArgb("#1D4ED8"),
        "Paid" => Color.FromArgb("#1D4ED8"),
        "Delivered" => Color.FromArgb("#065F46"), // Green Dark
        "Completed" => Color.FromArgb("#065F46"),
        "Cancelled" => Color.FromArgb("#9B1C1C"), // Red Dark
        _ => Color.FromArgb("#4A5568")
    };

    public decimal DeliveryFee { get; set; }
    public string FeeFormatted => $"₱{DeliveryFee:0.00}";
    
    public Delivery RawDelivery { get; set; } = null!;
}
