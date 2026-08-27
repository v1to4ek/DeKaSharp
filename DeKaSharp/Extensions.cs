using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp
{
    internal static class Extensions
    {
        public static bool HasNull<T>(this Span<T> dataSpan)
        {
            foreach (var item in dataSpan)
            {
                if (item is null) return true;
            }
            return false;
        }
    }
}
