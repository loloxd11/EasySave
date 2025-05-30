using System.Globalization;
using System.Windows.Data;

namespace EasySave.Converters
{
    /// <summary>
    /// Value converter that returns true if the input value is not null, false otherwise.
    /// Useful for data binding scenarios in WPF where null-check logic is required.
    /// </summary>
    public class NotNullConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value to a boolean indicating whether the value is not null.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if value is not null; otherwise, false.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return true if value is not null, otherwise false
            return value != null;
        }

        /// <summary>
        /// Not implemented. Throws NotImplementedException if called.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Nothing. Always throws.</returns>
        /// <exception cref="NotImplementedException">Always thrown.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not supported for this converter
            throw new NotImplementedException();
        }
    }
}
