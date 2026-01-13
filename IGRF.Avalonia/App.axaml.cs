using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IGRF_Interface.Core.Interfaces;
using IGRF_Interface.Core.Services;
using IGRF_Interface.Infrastructure.Interfaces;
using IGRF.Avalonia.Services;
using IGRF.Avalonia.ViewModels;
using IGRF.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;

namespace IGRF.Avalonia
{
    public partial class App : Application
    {
        /// <summary>
        /// Global service provider for DI
        /// </summary>
        public static IServiceProvider Services { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Build DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Setup navigation service with view factories
                var navigationService = (NavigationService)
                    Services.GetRequiredService<INavigationService>();
                navigationService.RegisterView(
                    "Dashboard",
                    () =>
                        new DashboardView
                        {
                            DataContext = Services.GetRequiredService<DashboardViewModel>(),
                        }
                );
                navigationService.RegisterView(
                    "Tuning",
                    () =>
                        new TuningView
                        {
                            DataContext = Services.GetRequiredService<TuningViewModel>(),
                        }
                );
                navigationService.RegisterView(
                    "Satellite",
                    () =>
                        new SatelliteView
                        {
                            DataContext = Services.GetRequiredService<SatelliteViewModel>(),
                        }
                );

                // Create main window with MainViewModel
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core Services
            services.AddSingleton<ISensorService, SensorService>();
            services.AddSingleton<ISatelliteCacheService, SatelliteCacheService>();
            services.AddSingleton<ICalculationService, CalculationService>();
            services.AddSingleton<ISatelliteService, SatelliteService>();
            services.AddSingleton<ISpaceTrackService, SpaceTrackService>();

            // Infrastructure Communication
            services.AddSingleton<
                ISerialPortManager,
                IGRF_Interface.Infrastructure.Communication.SerialPortManager
            >();
            services.AddSingleton<
                ITcpClientManager,
                IGRF_Interface.Infrastructure.Communication.TcpClientManager
            >();

            // Navigation Service
            services.AddSingleton<INavigationService, NavigationService>();

            // Avalonia Services
            services.AddSingleton<GlobePipeService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<TuningViewModel>();
            services.AddTransient<SatelliteViewModel>();
        }
    }
}
