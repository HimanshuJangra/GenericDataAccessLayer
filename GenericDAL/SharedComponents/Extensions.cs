using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents
{
    public static class Extensions
    {
        public static bool In<T>(this T value, params T[] compares)
        {
            var Eqd = EqualityComparer<T>.Default;

            return compares.Any(a => Eqd.Equals(a, value));
        }
    }
}
