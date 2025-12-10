using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ereoz.InstallerBase
{
    /// <summary>
    /// Реализация INotifyPropertyChanged.
    /// </summary>
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        /// <summary>
        /// Уведомляет об изменении значения свойства.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Вызывает событие, уведомляющее об изменении значении свойства.
        /// При вызове метода без параметра имя вызывающего свойства подставляется автоматически.
        /// Для вызова события, уведомляющего об изменении сразу всех свойств, нужно явно передать параметр <see langword="null"/>.
        /// </summary>
        /// <param name="propertyName">Имя изменённого свойства.</param>
        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
