using EasySave.Models;
using EasySave.Views;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;

namespace EasySave
{
    /// <summary>
    /// Main application class for EasySave WPF application.
    /// Handles application-level events and resource initialization.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Constructor for the App class.
        /// Initializes application-wide resources such as value converters.
        /// </summary>
        public App()
        {
            // Add a Boolean to Visibility converter to application resources
            Resources.Add("BoolToVis", new BooleanToVisibilityConverter());
            // Add a NotNull converter to application resources
            Resources.Add("NotNullConverter", new NotNullConverter());
        }

        /// <summary>
        /// Called on application startup.
        /// Initializes configuration and encryption services.
        /// </summary>
        /// <param name="e">Startup event arguments</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize configuration manager and load configuration
            ConfigManager.GetInstance().LoadConfiguration();
            // Initialize encryption service (loads encryption settings)
            EncryptionService.GetInstance();
        }
    }
}
