namespace GrabExpress_INTPROG
{
    public partial class AppShell : Shell
    {
        public AppShell(GrabExpress_INTPROG.Services.DatabaseService db)
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
            Routing.RegisterRoute(nameof(Pages.RegisterPage), typeof(Pages.RegisterPage));
            Routing.RegisterRoute(nameof(Pages.ForgotPasswordPage), typeof(Pages.ForgotPasswordPage));
            Routing.RegisterRoute(nameof(Pages.LandingPage), typeof(Pages.LandingPage));
            Routing.RegisterRoute(nameof(Pages.DashboardPage), typeof(Pages.DashboardPage));
            Routing.RegisterRoute(nameof(Pages.ProfilePage), typeof(Pages.ProfilePage));
            Routing.RegisterRoute(nameof(Pages.SettingsPage), typeof(Pages.SettingsPage));
            Routing.RegisterRoute(nameof(Pages.ContactUsPage), typeof(Pages.ContactUsPage));
            Routing.RegisterRoute(nameof(Pages.SplashScreenPage), typeof(Pages.SplashScreenPage));
            Routing.RegisterRoute(nameof(Pages.BookingPage), typeof(Pages.BookingPage));
            Routing.RegisterRoute("TrackingPage", typeof(Pages.DeliveryTrackingPage));
            Routing.RegisterRoute(nameof(Pages.PaymentPage), typeof(Pages.PaymentPage));
            Routing.RegisterRoute(nameof(Pages.DeliveryHistoryPage), typeof(Pages.DeliveryHistoryPage));

            // Start with all role tabs hidden on launch
            SetRoleTabs("");
        }

        public void SetRoleTabs(string role)
        {
            CustomerTab.IsVisible = role == "Customer";
            DriverTab.IsVisible   = role == "Driver";
            AdminTab.IsVisible    = role == "Admin";
        }
    }
}
