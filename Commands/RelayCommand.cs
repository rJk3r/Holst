using System;
using System.Windows.Input;

namespace Holst.Commands
{
    public class RelayCommand : CommandBase
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public override bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public override void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : CommandBase
    {
        private readonly Action<T> _execute;

        public RelayCommand(Action<T> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public override void Execute(object? parameter) => _execute((T)parameter!);
    }
}
