using System.Windows;
using EasySave.ViewModels;

namespace EasySave.Views
{
    public partial class RemoteConsoleView : Window
    {
        public RemoteConsoleView()
        {
            InitializeComponent();
            DataContext = new RemoteConsoleViewModel();
        }
    }
}
