using GrabExpress_INTPROG.Services;
using GrabExpress_INTPROG.Models;

namespace GrabExpress_INTPROG.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

	public RegisterPage(AuthService authService, DatabaseService databaseService)
	{
		InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;
	}

    private void OnIsDriverChanged(object sender, CheckedChangedEventArgs e)
    {
        DriverDetailsBorder.IsVisible = e.Value;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string name = FullNameEntry.Text;
        string email = EmailEntry.Text;
        string phone = PhoneEntry.Text;
        string password = PasswordEntry.Text;
        string confirmPassword = ConfirmPasswordEntry.Text;
        bool isDriver = IsDriverCheckBox.IsChecked;
        string license = LicenseEntry.Text;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please fill in all fields", "OK");
            return;
        }

        if (isDriver && string.IsNullOrWhiteSpace(license))
        {
            await DisplayAlert("Error", "Please enter your license number", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Error", "Passwords do not match", "OK");
            return;
        }

        try
        {
            // 1. Create User in Firebase Auth
            var result = await _authService.RegisterAsync(email, password);
            
            if (result != null)
            {
                string userId = result.User.Uid;
                string role;

                // Auto-Admin Email Check for seamless local testing/demo onboarding
                if (email.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    role = "Admin";
                }
                else
                {
                    role = isDriver ? "Driver" : "Customer";
                }

                // 2. Save User Role
                await _databaseService.SaveUserRoleAsync(userId, role);

                // Hide tabs that do not belong to this role
                if (Shell.Current is AppShell appShell)
                {
                    appShell.SetRoleTabs(role);
                }

                if (role == "Admin")
                {
                    // Save Admin as a basic Customer Profile record for name/contact retrieval
                    var customer = new Customer
                    {
                        CustomerId = userId,
                        Name = name,
                        Email = email,
                        ContactNumber = phone
                    };
                    await _databaseService.SaveUserProfileAsync(userId, customer);
                    
                    await DisplayAlert("Success", "Admin account created successfully!", "OK");
                    await Shell.Current.GoToAsync($"///{nameof(AdminDashboardPage)}");
                }
                else if (isDriver)
                {
                    // 3a. Save Driver Profile
                    var driver = new Driver
                    {
                        DriverId = userId,
                        Name = name,
                        ContactNumber = phone,
                        LicenseNumber = license,
                        Status = "Available"
                    };
                    await _databaseService.SaveDriverAsync(driver);
                    
                    await DisplayAlert("Success", "Driver account created successfully!", "OK");
                    await Shell.Current.GoToAsync($"///{nameof(DriverDashboardPage)}");
                }
                else
                {
                    // 3b. Save Customer Profile
                    var customer = new Customer
                    {
                        CustomerId = userId,
                        Name = name,
                        Email = email,
                        ContactNumber = phone
                    };
                    await _databaseService.SaveUserProfileAsync(userId, customer);
                    
                    await DisplayAlert("Success", "Customer account created successfully!", "OK");
                    await Shell.Current.GoToAsync($"///{nameof(DashboardPage)}");
                }
            }
        }
        catch (Exception ex)
        {
            string errorMessage = "An unexpected error occurred. Please try again.";
            
            if (ex.Message.Contains("EMAIL_EXISTS"))
            {
                errorMessage = "This email is already registered. Please log in or use a different email.";
            }
            else if (ex.Message.Contains("INVALID_EMAIL"))
            {
                errorMessage = "The email address is not valid.";
            }
            else if (ex.Message.Contains("WEAK_PASSWORD"))
            {
                errorMessage = "The password is too weak. Please use at least 6 characters.";
            }

            await DisplayAlert("Registration Failed", errorMessage, "OK");
        }
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        if (this.Width > 0)
        {
            double availableWidth = this.Width - 60; // 30 padding on each side
            FormCard.WidthRequest = Math.Min(400, availableWidth);
        }
    }
}
