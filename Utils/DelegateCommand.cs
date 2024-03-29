﻿using System.Windows.Input;

namespace Swapster.Utils
{
    /// <summary>
    /// An ICommand implementation, that delegates the called command to a given function
    /// </summary>
    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object?>? _canExecute;
        private readonly Action<object?> _execute;

        public event EventHandler? CanExecuteChanged;
        public DelegateCommand(Action<object?> execute) : this(execute, null) { }
        public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public virtual bool CanExecute(object? parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        public virtual void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}
