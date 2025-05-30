using System;
using System.Globalization;
using System.Windows.Data;

namespace EasySave.Converters
{
    /// <summary>
    /// Converter that enables a control only when a single item is selected.
    /// </summary>
    public class SingleSelectionToEnabledConverter : IValueConverter
    {
        /// <summary>
        /// Converts the selection count to a boolean indicating if exactly one item is selected.
        /// </summary>
        /// <param name="value">The selection count (expected to be an int).</param>
        /// <param name="targetType">The target type of the binding (not used).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>True if exactly one item is selected; otherwise, false.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is an integer representing the selection count
            if (value is int count)
                // Return true only if exactly one item is selected
                return count == 1;
            // Return false for invalid input
            return false;
        }

        /// <summary>
        /// Not implemented. Converts back from boolean to selection count.
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Throws NotImplementedException.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 