using FastNLose.Pages;
using FastNLose.Services;
using System.Globalization;

namespace FastNLose;


public partial class App : Application
{
    public static DatabaseService Database { get; private set; }
    public static string dbPath { get; private set; }

    public App()
    {
        InitializeComponent();
        dbPath = Path.Combine(FileSystem.AppDataDirectory, "fastnlose.db3");
        Database = new DatabaseService(dbPath);
        MainPage = new MainPage();
    }

    protected override void OnResume()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
           if (Current?.MainPage is MainPage mp)
               mp.ForceDailyRefresh();
        });
    }

}

