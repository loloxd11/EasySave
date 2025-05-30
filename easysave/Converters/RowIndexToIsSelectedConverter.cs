using EasySave.ViewModels;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Application = System.Windows.Application;

namespace EasySave.Converters
{
    /// <summary>
    /// Converter that binds the selection state of a DataGrid row to the selection state in the MainMenuViewModel.
    /// Used to synchronize the UI checkbox state with the ViewModel's selected jobs.
    /// </summary>
    public class RowIndexToIsSelectedConverter : IValueConverter
    {
        /// <summary>
        /// Converts the DataGridRow parameter to a boolean indicating if the job at the given index is selected.
        /// </summary>
        /// <param name="value">The value from the binding target (not used).</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The DataGridRow whose selection state is being queried.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if the job at the row's index is selected, false otherwise.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure the parameter is a DataGridRow and has a valid DataContext
            if (parameter is DataGridRow row && row.DataContext != null)
            {
                // Get the MainWindow and its DataContext (MainMenuViewModel)
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.DataContext is MainMenuViewModel viewModel)
                {
                    int index = row.GetIndex();
                    // Check if the job at this index is selected
                    return viewModel.IsJobSelected(index);
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the ViewModel's selection state when the checkbox is toggled in the UI.
        /// </summary>
        /// <param name="value">The new value of the checkbox (bool).</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The DataGridRow whose selection state is being set.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The value passed in, to update the UI.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure the parameter is a DataGridRow, has a valid DataContext, and value is a bool
            if (parameter is DataGridRow row && row.DataContext != null && value is bool isSelected)
            {
                // Get the MainWindow and its DataContext (MainMenuViewModel)
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.DataContext is MainMenuViewModel viewModel)
                {
                    int index = row.GetIndex();

                    // If the selection state has changed, update the ViewModel
                    if (isSelected != viewModel.IsJobSelected(index))
                    {
                        viewModel.ToggleJobSelection(index);
                    }
                }
            }
            return value;
        }
    }
}
