using EasySave.ViewModels;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace EasySave.Converters
{
    public class RowIndexToIsSelectedMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = SelectedJobIndices
            // values[1] = DataGridRow
            if (values[0] is System.Collections.IEnumerable selectedIndices && values[1] is DataGridRow row)
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as EasySave.MainWindow;
                if (mainWindow?.DataContext is MainMenuViewModel viewModel)
                {
                    int index = row.GetIndex();
                    return viewModel.IsJobSelected(index);
                }
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // La gestion du retour se fait via les events Checked/Unchecked dans le code-behind
            return null;
        }
    }
}
