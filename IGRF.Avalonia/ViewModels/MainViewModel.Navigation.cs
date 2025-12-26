using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using IGRF.Avalonia.Views;

namespace IGRF.Avalonia.ViewModels
{
    /// <summary>
    /// MainViewModel - Navigation Logic
    /// Contains view navigation and navigation item management
    /// </summary>
    public partial class MainViewModel
    {
        // --- Navigation ---
        [ObservableProperty] private ObservableCollection<NavigationItem> _navigationItems = new();
        [ObservableProperty] private NavigationItem? _selectedNavigationItem;
        [ObservableProperty] private UserControl? _currentView;
        
        // View Cache
        private readonly DashboardView _dashboardView;
        private readonly TuningView _tuningView;
        private readonly SatelliteView _satelliteView;
        
        // Navigation Item Helper Class
        public class NavigationItem
        {
            public string Title { get; set; } = "";
            public string Icon { get; set; } = "";
            public string ViewName { get; set; } = "";
        }
        
        private void InitializeNavigation()
        {
            NavigationItems.Add(new NavigationItem { Title = "Dashboard", Icon = "📊", ViewName = "Dashboard" });
            NavigationItems.Add(new NavigationItem { Title = "PID Tuning", Icon = "🔧", ViewName = "Tuning" });
            NavigationItems.Add(new NavigationItem { Title = "Satellite Map", Icon = "🛰️", ViewName = "Satellite" });
            
            SelectedNavigationItem = NavigationItems[0];
            CurrentView = _dashboardView;
        }
        
        partial void OnSelectedNavigationItemChanged(NavigationItem? value)
        {
            if (value == null) return;
            Navigate(value.ViewName);
        }
        
        private void Navigate(string viewName)
        {
            switch (viewName)
            {
                case "Dashboard": CurrentView = _dashboardView; break;
                case "Tuning": CurrentView = _tuningView; break;
                case "Satellite": CurrentView = _satelliteView; break;
            }
        }
    }
}
