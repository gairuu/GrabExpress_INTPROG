using GrabExpress_INTPROG.Models;
using GrabExpress_INTPROG.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace GrabExpress_INTPROG.Pages;

public partial class DeliveryHistoryPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly AuthService _authService;

    public DeliveryHistoryPage(DatabaseService databaseService, AuthService authService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistory();
    }

    private async Task LoadHistory()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        EmptyState.IsVisible = false;
        HistoryContainer.Children.Clear();

        try
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null) return;

            var history = await _databaseService.GetCustomerDeliveryHistoryAsync(currentUser.Uid);

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;

            if (history == null || history.Count == 0)
            {
                EmptyState.IsVisible = true;
                return;
            }

            foreach (var delivery in history)
            {
                HistoryContainer.Children.Add(BuildCard(delivery));
            }
        }
        catch
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            EmptyState.IsVisible = true;
        }
    }

    private static RoundRectangle RR(double r) =>
        new RoundRectangle { CornerRadius = new CornerRadius(r) };

    private View BuildCard(Delivery delivery)
    {
        bool isCompleted = delivery.DeliveryStatus == "Completed";
        string icon    = isCompleted ? "✅" : "✖";
        string bgColor = isCompleted ? "#E8F5E9" : "#FFEBEE";
        string badgeBg = isCompleted ? "#00B14F" : "#C62828";
        string label   = isCompleted ? "Completed" : "Cancelled";

        // Status icon circle
        var iconBadge = new Border
        {
            BackgroundColor = Color.FromArgb(bgColor),
            StrokeThickness = 0,
            StrokeShape     = RR(14),
            WidthRequest    = 46,
            HeightRequest   = 46,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text              = icon,
                FontSize          = 20,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center
            }
        };

        // Status pill
        var pill = new Border
        {
            BackgroundColor = Color.FromArgb(badgeBg),
            StrokeThickness = 0,
            StrokeShape     = RR(8),
            Padding         = new Thickness(8, 3),
            HorizontalOptions = LayoutOptions.End,
            Content = new Label
            {
                Text              = label,
                FontSize          = 10,
                FontAttributes    = FontAttributes.Bold,
                TextColor         = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            }
        };

        // Middle column
        var centre = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text           = $"From: {delivery.PickupLocation ?? "—"}",
                    FontSize       = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor      = Color.FromArgb("#1A1A1A"),
                    LineBreakMode  = LineBreakMode.TailTruncation
                },
                new Label
                {
                    Text          = $"To: {delivery.DropoffLocation ?? "—"}",
                    FontSize      = 12,
                    TextColor     = Color.FromArgb("#555555"),
                    LineBreakMode = LineBreakMode.TailTruncation
                },
                new Label
                {
                    Text      = delivery.BookingTime.ToString("MMM dd, yyyy"),
                    FontSize  = 11,
                    TextColor = Color.FromArgb("#AAAAAA")
                }
            }
        };

        // Right column
        var right = new VerticalStackLayout
        {
            VerticalOptions   = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
            Spacing           = 4,
            Children =
            {
                new Label
                {
                    Text              = $"₱{delivery.DeliveryFee:N0}",
                    FontSize          = 14,
                    FontAttributes    = FontAttributes.Bold,
                    TextColor         = Color.FromArgb("#00B14F"),
                    HorizontalOptions = LayoutOptions.End
                },
                pill
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 14
        };
        grid.Add(iconBadge, 0, 0);
        grid.Add(centre,    1, 0);
        grid.Add(right,     2, 0);

        var border = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape     = RR(16),
            Padding         = new Thickness(18, 16),
            Shadow          = new Shadow
            {
                Brush   = new SolidColorBrush(Color.FromArgb("#000000")),
                Offset  = new Point(0, 2),
                Radius  = 8,
                Opacity = 0.06f
            },
            Content = grid
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Shell.Current.GoToAsync($"TrackingPage?deliveryId={delivery.DeliveryId}");
        };
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
