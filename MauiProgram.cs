
using FastNLose.Services;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace FastNLose
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            var builder = MauiApp.CreateBuilder();         
       

            builder
                .UseMauiApp<App>()
                .UseLocalNotification()                
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "FastNLose.db3");
            builder.Services.AddSingleton(new DatabaseService(dbPath));

            builder.Services.AddTransient<Pages.MainPage>();
            builder.Services.AddTransient<Pages.SettingsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
