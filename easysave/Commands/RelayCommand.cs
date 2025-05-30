using System.Windows.Input;

namespace EasySave.Commands
{
    /// <summary>
    /// A generic command implementation that allows binding actions to UI elements.
    /// This class is used to encapsulate an Action delegate and an optional condition (Func&lt;bool&gt;)
    /// to determine whether the command can execute.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional condition to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if the execute parameter is null.</exception>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            // If no canExecute predicate is provided, always return true.
            return _canExecute == null || _canExecute();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
        public void Execute(object parameter)
        {
            // Invoke the action associated with this command.
            _execute();
        }
    }

    /// <summary>
    /// A generic command implementation that supports parameterized actions.
    /// This class is used to encapsulate an Action&lt;T&gt; delegate and an optional condition (Predicate&lt;T&gt;)
    /// to determine whether the command can execute.
    /// </summary>
    /// <typeparam name="T">The type of the parameter passed to the command.</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional condition to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if the execute parameter is null.</exception>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">The parameter passed to the command.</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            // If parameter is null or not of type T, command cannot execute.
            if (parameter == null || !(parameter is T)) return false;
            // If no canExecute predicate is provided, always return true.
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">The parameter passed to the command.</param>
        public void Execute(object parameter)
        {
            // Invoke the action associated with this command, passing the parameter.
            _execute((T)parameter);
        }
    }

    /// <summary>
    /// Asynchronous command implementation to support async methods in WPF binding without crashing.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous action to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional condition to determine if the command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if the execute parameter is null.</exception>
        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            // Command cannot execute if already running.
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
        public async void Execute(object parameter)
        {
            _isExecuting = true;
            try
            {
                // Notify WPF that command state has changed.
                CommandManager.InvalidateRequerySuggested();
                await _execute();
            }
            catch (Exception ex)
            {
                // Log and display any exception that occurs during execution.
                System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand exception: {ex.Message}\n{ex.StackTrace}");
                System.Windows.MessageBox.Show($"Error while executing command: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _isExecuting = false;
                // Notify WPF that command state has changed.
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
