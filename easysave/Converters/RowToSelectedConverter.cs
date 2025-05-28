using EasySave.ViewModels;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace EasySave.Converters
{
    public class RowToIsSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridRow row && row.DataContext != null)
            {
                var viewModel = ((MainWindow)App.Current.MainWindow).DataContext as MainMenuViewModel;
                if (viewModel != null)
                {
                    int index = row.GetIndex();
                    return viewModel.IsJobSelected(index);
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
