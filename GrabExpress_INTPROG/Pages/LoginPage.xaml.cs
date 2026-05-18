using GrabExpress_INTPROG.Services;

namespace GrabExpress_INTPROG.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

	public LoginPage(AuthService authService, DatabaseService databaseService)
	{
		InitializeComponent();
        _authService = authService;
        _databaseService = databaseService;
	}

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please enter email and password", "OK");
            return;
        }

        try
        {
            var result = await _authService.LoginAsync(email, password);
            if (result != null)
            {
                // Fetch user role from database
                string? role = await _databaseService.GetUserRoleAsync(result.User.Uid);

                if (role == "Suspended")
                {
                    // Wipes local authentication immediately so they aren't remembered
                    _authService.Logout();
                    await DisplayAlert("Login Failed", "This account has been suspended by the administrator.", "OK");
                    return;
                }

                // Safe fallback to "Customer" for pre-existing accounts that do not have an explicit UserRoles node yet
                if (string.IsNullOrEmpty(role))
                {
                    role = "Customer";
                }

                // Hide tabs that do not belong to this role
                if (Shell.Current is AppShell appShell)
                {
                    appShell.SetRoleTabs(role);
                }

                if (role == "Admin")
                {
                    await Shell.Current.GoToAsync($"///{nameof(AdminDashboardPage)}");
                }
                else if (role == "Driver")
                {
                    await Shell.Current.GoToAsync($"///{nameof(DriverDashboardPage)}");
                }
                else
                {
                    await Shell.Current.GoToAsync($"///{nameof(DashboardPage)}");
                }
            }
        }
        catch (Exception ex)
        {
            string msg = ex.Message ?? "";

            string friendlyMessage;
            if (msg.Contains("INVALID_LOGIN_CREDENTIALS") || msg.Contains("INVALID_PASSWORD") || msg.Contains("EMAIL_NOT_FOUND"))
                friendlyMessage = "Wrong email or password. Please try again.";
            else if (msg.Contains("TOO_MANY_ATTEMPTS_TRY_LATER") || msg.Contains("too many"))
                friendlyMessage = "Too many failed attempts. Please wait a moment and try again.";
            else if (msg.Contains("USER_DISABLED"))
                friendlyMessage = "This account has been disabled. Please contact support.";
            else if (msg.Contains("INVALID_EMAIL") || msg.Contains("badly formatted"))
                friendlyMessage = "The email address you entered is not valid. Please check and try again.";
            else if (msg.Contains("network") || msg.Contains("connection"))
                friendlyMessage = "No internet connection. Please check your network and try again.";
            else
                friendlyMessage = "Login failed. Please check your credentials and try again.";

            await DisplayAlert("Login Failed", friendlyMessage, "OK");
        }
    }

    private async void OnSignUpTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));
    }

    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        if (this.Width > 0)
        {
            double availableWidth = this.Width - 48;
            FormCard.WidthRequest = Math.Min(400, availableWidth);
        }
    }
}
