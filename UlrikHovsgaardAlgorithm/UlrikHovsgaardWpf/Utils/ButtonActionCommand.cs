using System;
using System.Windows.Input;

namespace UlrikHovsgaardWpf.Utils
{
    public class ButtonActionCommand : ICommand
    {
        private readonly Action _execute;

        public ButtonActionCommand(Action execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public event EventHandler CanExecuteChanged;
    }
}
