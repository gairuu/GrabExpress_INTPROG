using Microsoft.Extensions.Logging;

namespace GrabExpress_INTPROG
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddSingleton<GrabExpress_INTPROG.Services.AuthService>();
            builder.Services.AddSingleton<GrabExpress_INTPROG.Services.DatabaseService>();

            // Pages
            builder.Services.AddSingleton<GrabExpress_INTPROG.Pages.LoginPage>();
            builder.Services.AddSingleton<GrabExpress_INTPROG.Pages.RegisterPage>();
            builder.Services.AddSingleton<GrabExpress_INTPROG.Pages.DashboardPage>();
            builder.Services.AddSingleton<GrabExpress_INTPROG.Pages.DriverDashboardPage>();
            builder.Services.AddSingleton<GrabExpress_INTPROG.Pages.AdminDashboardPage>();
            builder.Services.AddSingleton<GrabExpress_INTPROG.Pages.ProfilePage>();
            builder.Services.AddSingleton<GrabExpress_INTPROG.Pages.BookingPage>();
            builder.Services.AddTransient<GrabExpress_INTPROG.Pages.DeliveryTrackingPage>();
            builder.Services.AddTransient<GrabExpress_INTPROG.Pages.PaymentPage>();
            builder.Services.AddTransient<GrabExpress_INTPROG.Pages.DeliveryHistoryPage>();

#if WINDOWS
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderlineWindows", (handler, view) =>
            {
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            });
#endif

            return builder.Build();
        }
    }
}
