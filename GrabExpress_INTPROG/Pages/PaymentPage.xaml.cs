using GrabExpress_INTPROG.Services;
using GrabExpress_INTPROG.Models;

namespace GrabExpress_INTPROG.Pages;

[QueryProperty(nameof(DeliveryId), "deliveryId")]
public partial class PaymentPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private string _deliveryId = string.Empty;
    private decimal _amount;

    public string DeliveryId
    {
        get => _deliveryId;
        set
        {
            _deliveryId = value;
            LoadPaymentData();
        }
    }

	public PaymentPage(DatabaseService databaseService)
	{
		InitializeComponent();
        _databaseService = databaseService;
	}

    private async void LoadPaymentData()
    {
        var delivery = await _databaseService.GetDeliveryAsync(DeliveryId);
        if (delivery != null)
        {
            _amount = delivery.DeliveryFee;
            AmountLabel.Text = $"₱{_amount:N2}";
        }
    }

    private async void OnPayClicked(object sender, EventArgs e)
    {
        try
        {
            string method = "Cash";
            if (CardRadio.IsChecked) method = "Card";
            else if (WalletRadio.IsChecked) method = "E-wallet";

            var payment = new Payment
            {
                DeliveryId = DeliveryId,
                Amount = _amount,
                PaymentMethod = method,
                PaymentStatus = "Paid"
            };

            await _databaseService.RecordPaymentAsync(payment);

            // Release driver back to Available
            var delivery = await _databaseService.GetDeliveryAsync(DeliveryId);
            if (delivery != null && !string.IsNullOrEmpty(delivery.DriverId))
            {
                await _databaseService.UpdateDriverStatusAsync(delivery.DriverId, "Available");
            }

            // Mark delivery as Completed so it no longer appears as "active"
            await _databaseService.UpdateDeliveryStatusAsync(DeliveryId, "Completed");

            await DisplayAlert("Payment Successful", "Thank you for using GrabExpress!", "OK");
            
            await Shell.Current.GoToAsync($"///{nameof(DashboardPage)}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Payment Error", $"An error occurred while processing your payment: {ex.Message}", "OK");
        }
    }
}
