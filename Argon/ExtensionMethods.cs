using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Argon
{
    public static class ExtensionMethods
    {
        public static long NextSecond(this long i)
        {
            return (long)Math.Ceiling((decimal)i / 10000000) * 10000000;
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IQueryable<T> enumeration)
        {
            return new ObservableCollection<T>(enumeration);
        }

    }
}
