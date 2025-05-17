using System.Windows;
using System.Windows.Controls;
using EasySave; // Assure-toi que ce namespace est correct

namespace EasySave
{
    public partial class AddBackupJobDialog : Window
    {
        public string JobName => NameBox.Text;
        public string SourcePath => SourceBox.Text;
        public string TargetPath => TargetBox.Text;

        public BackupType BackupType
        {
            get
            {
                if (TypeBox.SelectedItem is ComboBoxItem item)
                {
                    // On récupère le Tag et on le convertit en BackupType
                    return (item.Tag?.ToString() == "1") ? BackupType.Differential : BackupType.Complete;
                }
                return BackupType.Complete;
            }
        }

        public AddBackupJobDialog()
        {
            InitializeComponent();
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
