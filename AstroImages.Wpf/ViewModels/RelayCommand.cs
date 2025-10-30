using System;
using System.Windows.Input;

namespace AstroImages.Wpf.ViewModels
{
    /// <summary>
    /// RelayCommand is a reusable implementation of ICommand for MVVM applications.
    /// ICommand is WPF's interface for binding UI actions (button clicks, menu items) to ViewModel methods.
    /// This allows the ViewModel to handle UI actions without knowing about specific UI controls.
    /// </summary>
    public class RelayCommand : ICommand
    {
        // Private fields to store the action to execute and the condition for execution
        // Action<object?> = a method that takes one parameter and returns nothing
        private readonly Action<object?> _execute;
        
        // Func<object?, bool> = a method that takes one parameter and returns a boolean
        // This determines if the command can be executed (enables/disables buttons)
        private readonly Func<object?, bool>? _canExecute;

        /// <summary>
        /// Constructor for RelayCommand.
        /// </summary>
        /// <param name="execute">The method to call when the command is executed (required)</param>
        /// <param name="canExecute">Optional method to determine if command can execute (for enabling/disabling UI)</param>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            // Store the execute action, throw exception if it's null (required parameter)
            // ?? throw means "if left side is null, throw exception"
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            
            // Store the optional canExecute function (can be null)
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute with the given parameter.
        /// This is called by WPF to enable/disable bound UI elements (buttons, menu items).
        /// </summary>
        /// <param name="parameter">Data passed from the UI (often null)</param>
        /// <returns>True if command can execute, false if it should be disabled</returns>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        // This means: if _canExecute exists, call it with parameter; otherwise return true

        /// <summary>
        /// Executes the command with the given parameter.
        /// This is called when the user interacts with the bound UI element.
        /// </summary>
        /// <param name="parameter">Data passed from the UI</param>
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Event that WPF listens to for determining when to re-evaluate CanExecute.
        /// CommandManager.RequerySuggested is fired by WPF when it thinks command states might have changed.
        /// This is what makes buttons automatically enable/disable based on application state.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            // When someone subscribes to this event, also subscribe them to WPF's global command event
            add { CommandManager.RequerySuggested += value; }
            
            // When someone unsubscribes, also unsubscribe them from WPF's global command event
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
