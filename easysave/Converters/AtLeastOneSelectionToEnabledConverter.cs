using System;
using System.Globalization;
using System.Windows.Data;

namespace EasySave.Converters
{
    /// <summary>
    /// Converter that enables or disables a control based on whether at least one item is selected.
    /// </summary>
    public class AtLeastOneSelectionToEnabledConverter : IValueConverter
    {
        /// <summary>
        /// Converts the selection count to a boolean indicating if the control should be enabled.
        /// </summary>
        /// <param name="value">The number of selected items (expected to be an int).</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if at least one item is selected; otherwise, false.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is an integer representing the count of selected items
            if (value is int count)
                // Enable if at least one item is selected
                return count >= 1;
            // Disable if value is not an integer
            return false;
        }

        /// <summary>
        /// Not implemented. Converts a value back to its source type.
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Throws NotImplementedException.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 