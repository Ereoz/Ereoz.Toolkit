using System.Collections.Generic;
using System.Linq;

namespace Ereoz.InstallerBase.Core
{
    public class IteratorCollection<T>
    {
        private List<T> list = new List<T>();

        public T Current { get; private set; }

        public void Add(T item)
        {
            list.Add(item);
            Current = list.First();
        }

        public T Next()
        {
            var currentIndex = list.IndexOf(Current);

            if (currentIndex < list.Count - 1)
                Current = list[currentIndex + 1];

            return Current;
        }

        public T Last()
        {
            var currentIndex = list.IndexOf(Current);

            if (currentIndex > 0)
                Current = list[currentIndex - 1];

            return Current;
        }
    }
}
