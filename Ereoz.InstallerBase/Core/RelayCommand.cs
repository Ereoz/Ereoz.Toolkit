using System;
using System.Windows.Input;

namespace Ereoz.InstallerBase
{
    /// <summary>
    /// Реализация ICommand с уведомлением возможности выполнения.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private Action<object> _execute;
        private Predicate<object> _canExecute;

        /// <summary>
        /// Уведомляет, когда возможность выполнения команды изменилась.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="execute">Действие, выполняющееся при вызове команды.</param>
        /// <param name="canExecute">Предикат, определяющий, может ли команда выполняться.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Определяет, может ли команда выполняться в текущем состоянии.
        /// </summary>
        /// <param name="parameter">Параметр для команды. Если не задан, будет <see langword="null"/></param>
        /// <returns>Если команда может быть выполнена - <see langword="true" />, в противном случае - <see langword="false" />.</returns>
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        /// <summary>
        /// Выполняет команду.
        /// </summary>
        /// <param name="parameter">Параметр для команды. Если не задан, будет <see langword="null"/></param>
        public void Execute(object parameter) => _execute(parameter);
    }
}
