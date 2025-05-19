using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EasySave.ViewModels;
using Application = System.Windows.Application;

namespace EasySave.Converters
{
    public class RowIndexToIsSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is DataGridRow row && row.DataContext != null)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.DataContext is MainMenuViewModel viewModel)
                {
                    int index = row.GetIndex();
                    return viewModel.IsJobSelected(index);
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is DataGridRow row && row.DataContext != null && value is bool isSelected)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.DataContext is MainMenuViewModel viewModel)
                {
                    int index = row.GetIndex();

                    // Si l'état de la case à cocher a changé, mettre à jour la sélection
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
