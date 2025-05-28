using EasySave.ViewModels;
using System.Windows;

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
