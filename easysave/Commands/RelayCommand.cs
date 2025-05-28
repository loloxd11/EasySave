using System.Windows.Input;

namespace EasySave.Commands
{
    /// <summary>  
    /// A generic command implementation that allows binding actions to UI elements.  
    /// This class is used to encapsulate an Action delegate and an optional condition (Func<bool>)  
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
            return _canExecute == null || _canExecute();
        }

        /// <summary>  
        /// Executes the command.  
        /// </summary>  
        /// <param name="parameter">Data used by the command. Not used in this implementation.</param>  
        public void Execute(object parameter)
        {
            _execute();
        }
    }

    /// <summary>  
    /// A generic command implementation that supports parameterized actions.  
    /// This class is used to encapsulate an Action<T> delegate and an optional condition (Predicate<T>)  
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
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>  
        /// Executes the command.  
        /// </summary>  
        /// <param name="parameter">The parameter passed to the command.</param>  
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}
