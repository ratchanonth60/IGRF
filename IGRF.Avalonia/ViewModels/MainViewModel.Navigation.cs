using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

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

            // Navigate to default view
            _navigationService.NavigateTo("Dashboard");
        }

        partial void OnSelectedNavigationItemChanged(NavigationItem? value)
        {
            if (value == null) return;
            Navigate(value.ViewName);
        }

        private void Navigate(string viewName)
        {
            _navigationService.NavigateTo(viewName);
        }
    }
}
