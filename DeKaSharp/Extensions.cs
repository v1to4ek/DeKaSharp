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

                if (item is IntPtr ptr)
                {
                    if (ptr == IntPtr.Zero) return true;
                }
            }
            return false;
        }
    }
}
