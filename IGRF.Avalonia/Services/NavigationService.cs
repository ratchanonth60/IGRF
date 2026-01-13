using System;
using System.Collections.Generic;
using Avalonia.Controls;
using IGRF.Avalonia.Views;

namespace IGRF.Avalonia.Services
{
    /// <summary>
    /// Navigation service implementation for managing view navigation
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly Dictionary<string, Func<UserControl>> _viewFactories;
        private readonly Dictionary<string, UserControl> _viewCache;
        private object? _currentView;

        public object? CurrentView
        {
            get => _currentView;
            private set
            {
                if (_currentView != value)
                {
                    _currentView = value;
                    CurrentViewChanged?.Invoke(value);
                }
            }
        }

        public event Action<object?>? CurrentViewChanged;

        public NavigationService()
        {
            _viewFactories = new Dictionary<string, Func<UserControl>>();
            _viewCache = new Dictionary<string, UserControl>();
        }

        /// <summary>
        /// Register a view factory for a view name
        /// </summary>
        public void RegisterView(string viewName, Func<UserControl> factory)
        {
            _viewFactories[viewName] = factory;
        }

        /// <summary>
        /// Navigate to a view by name (caches views for performance)
        /// </summary>
        public void NavigateTo(string viewName)
        {
            if (!_viewFactories.ContainsKey(viewName))
            {
                throw new ArgumentException(
                    $"View '{viewName}' is not registered",
                    nameof(viewName)
                );
            }

            if (!_viewCache.TryGetValue(viewName, out var view))
            {
                view = _viewFactories[viewName]();
                _viewCache[viewName] = view;
            }

            CurrentView = view;
        }

        /// <summary>
        /// Navigate to first registered view (default)
        /// </summary>
        public void NavigateToDefault()
        {
            foreach (var key in _viewFactories.Keys)
            {
                NavigateTo(key);
                break;
            }
        }
    }
}
