using EasySave.ViewModels;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace EasySave.Converters
{
    /// <summary>
    /// MultiValueConverter that determines if a DataGrid row is selected based on its index.
    /// Used for binding selection state in a DataGrid to the ViewModel's selection logic.
    /// </summary>
    public class RowIndexToIsSelectedMultiConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts the row index and selected indices to a boolean indicating if the row is selected.
        /// </summary>
        /// <param name="values">
        /// values[0]: IEnumerable of selected job indices (from ViewModel)
        /// values[1]: DataGridRow representing the current row
        /// </param>
        /// <param name="targetType">The target binding type (should be bool).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture info for the conversion.</param>
        /// <returns>True if the row is selected, false otherwise.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = SelectedJobIndices
            // values[1] = DataGridRow
            if (values[0] is System.Collections.IEnumerable selectedIndices && values[1] is DataGridRow row)
            {
                // Retrieve the MainWindow and its DataContext (MainMenuViewModel)
                var mainWindow = System.Windows.Application.Current.MainWindow as EasySave.MainWindow;
                if (mainWindow?.DataContext is MainMenuViewModel viewModel)
                {
                    int index = row.GetIndex();
                    // Use ViewModel logic to determine if the job at this index is selected
                    return viewModel.IsJobSelected(index);
                }
            }
            return false;
        }

        /// <summary>
        /// Not implemented. Selection changes are handled via events in the code-behind.
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetTypes">The array of target types.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">Culture info.</param>
        /// <returns>Always returns null.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Selection changes are handled via Checked/Unchecked events in the code-behind
            return null;
        }
    }
}
