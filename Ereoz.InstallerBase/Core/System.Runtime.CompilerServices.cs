#if NET40
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Позволяет получить имя свойства или метода вызывающего метод объекта.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class CallerMemberNameAttribute : Attribute
    {
    }

    /// <summary>
    /// Позволяет получить полный путь исходного файла, содержащего вызывающий объект.
    /// Это путь к файлу во время компиляции.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class CallerFilePathAttribute : Attribute
    {
    }

    /// <summary>
    /// Позволяет получить номер строки в исходном файле, в которой вызывается метод.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class CallerLineNumberAttribute : Attribute
    {
    }
}
#endif