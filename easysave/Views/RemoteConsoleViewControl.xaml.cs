using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace EasySave.Views
{
    /// <summary>
    /// Interaction logic for RemoteConsoleViewControl.
    /// This control hosts the remote console view, allowing users to monitor and control backup jobs on a remote server.
    /// </summary>
    public partial class RemoteConsoleViewControl : UserControl
    {
        /// <summary>
        /// Gets the ViewModel for the remote console.
        /// </summary>
        public RemoteConsoleViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteConsoleViewControl"/> class.
        /// Sets up the DataContext and handles the back navigation event.
        /// </summary>
        public RemoteConsoleViewControl()
        {
            InitializeComponent();
            // Create the ViewModel and set it as the DataContext for data binding.
            var vm = new ViewModels.RemoteConsoleViewModel();
            DataContext = vm;
            // Subscribe to the back navigation event to return to the main menu.
            vm.RequestBackToMainView += () =>
            {
                // If the main window is available, reset the view to the main menu.
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ResetToMainMenu();
                }
            };
        }
    }
}