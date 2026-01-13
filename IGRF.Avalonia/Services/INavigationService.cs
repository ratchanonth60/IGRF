using System;

namespace IGRF.Avalonia.Services
{
    /// <summary>
    /// Interface for navigation service
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Currently displayed view
        /// </summary>
        object? CurrentView { get; }

        /// <summary>
        /// Navigate to a view by name
        /// </summary>
        void NavigateTo(string viewName);

        /// <summary>
        /// Event fired when current view changes
        /// </summary>
        event Action<object?>? CurrentViewChanged;
    }
}
