using CommunityToolkit.Mvvm.DependencyInjection;
using EventLook.Model;
using EventLook.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace EventLook;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Ioc.Default.ConfigureServices(
           new ServiceCollection()
           .AddSingleton<IDataService, DataService>()
           .AddTransient<MainViewModel>()
           .BuildServiceProvider());
    }
}
