using EasySave.ViewModels;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace EasySave.Converters
{
    /// <summary>
    /// Converter that determines if a DataGridRow is selected based on the ViewModel's selection logic.
    /// </summary>
    public class RowToIsSelectedConverter : IValueConverter
    {
        /// <summary>
        /// Converts a DataGridRow to a boolean indicating if the row is selected.
        /// </summary>
        /// <param name="value">The DataGridRow to check.</param>
        /// <param name="targetType">The target type of the binding (should be bool).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if the row is selected, otherwise false.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a DataGridRow and has a valid DataContext
            if (value is DataGridRow row && row.DataContext != null)
            {
                // Retrieve the ViewModel from the MainWindow's DataContext
                var viewModel = ((MainWindow)App.Current.MainWindow).DataContext as MainMenuViewModel;
                if (viewModel != null)
                {
                    // Get the index of the row and check if it is selected
                    int index = row.GetIndex();
                    return viewModel.IsJobSelected(index);
                }
            }
            // Return false if the row is not selected or ViewModel is not available
            return false;
        }

        /// <summary>
        /// Not implemented: Converts back from boolean to DataGridRow (not used).
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The input value (no conversion).</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // No conversion back is needed
            return value;
        }
    }
}
