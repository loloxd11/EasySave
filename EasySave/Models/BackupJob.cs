using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class BackupJob : INotifyPropertyChanged
    {
        // Name of the backup job
        private string name;
        // Source directory path
        private string src;
        // Destination directory path
        private string dst;
        // Type of backup (Complete or Differential)
        private BackupType type;
        // Strategy used to execute the backup
        private AbstractBackupStrategy backupStrategy;
        private JobState _state = JobState.inactive;
        private int _progress = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupJob"/> class.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="source">The source directory path.</param>
        /// <param name="target">The destination directory path.</param>
        /// <param name="type">The type of backup (Complete or Differential).</param>
        /// <param name="strategy">The backup strategy to use.</param>
        public BackupJob(string name, string source, string target, BackupType type, AbstractBackupStrategy strategy)
        {
            this.name = name;
            this.src = source;
            this.dst = target;
            this.type = type;
            this.backupStrategy = strategy;
        }

        /// <summary>
        /// Executes the backup job using the specified strategy.
        /// </summary>
        /// <returns>True if the backup was successful; otherwise, false.</returns>
        public bool Execute()
        {
            return backupStrategy.Execute(name, src, dst, "default");
        }

        // Properties to access private fields

        /// <summary>
        /// Gets the name of the backup job.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Gets the source directory path.
        /// </summary>
        public string Source => src;

        /// <summary>
        /// Gets the destination directory path.
        /// </summary>
        public string Destination => dst;

        /// <summary>
        /// Gets the type of backup.
        /// </summary>
        public BackupType Type => type;

        public JobState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress of the backup job as a percentage.
        /// Notifies property change listeners when the value changes.
        /// </summary>
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }
    }
}
