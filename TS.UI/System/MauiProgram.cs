using Microsoft.Extensions.Logging;
using TS.Engine.Abstractions;
using TS.AWS;

namespace TS.UI.System
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Load main app and font
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Assistant-VariableFont_wght.ttf", "Assistant");
                });

            // Dependency Injection setup
            builder.Services.AddSingleton<IAuthService, AwsAuthService>(); // Auth service
            builder.Services.AddSingleton<IShoppingListServiceFactory, AwsShoppingListServiceFactory>(); // Shopping list factory
            builder.Services.AddTransient<AppPages.LoginPage>(); // Login page

            #if DEBUG
            builder.Logging.AddDebug(); // Enable debug logging
            #endif

            return builder.Build();
        }
    }
}
