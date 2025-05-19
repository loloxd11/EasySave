using EasySave.Views;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;

namespace EasySave
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Resources.Add("BoolToVis", new BooleanToVisibilityConverter());
            Resources.Add("NotNullConverter", new NotNullConverter());
        }
    }
}
