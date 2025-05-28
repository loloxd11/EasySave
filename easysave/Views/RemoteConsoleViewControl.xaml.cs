using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace EasySave.Views
{
    public partial class RemoteConsoleViewControl : UserControl
    {
        public RemoteConsoleViewModel ViewModel { get; private set; }
        public RemoteConsoleViewControl()
        {
            InitializeComponent();
            var vm = new ViewModels.RemoteConsoleViewModel();
            DataContext = vm;
            vm.RequestBackToMainView += () =>
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ResetToMainMenu();
                }
            };
        }
    }
}